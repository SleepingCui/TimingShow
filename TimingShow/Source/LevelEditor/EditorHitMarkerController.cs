using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using TimingShow.HUD;

namespace TimingShow.LevelEditor
{
    // Shows the hits of the just-finished run on the editor track. Lives on a
    // persistent GameObject created when the session snapshot is captured.
    public sealed class EditorHitMarkerController : MonoBehaviour
    {
        private const float MarkerRadiusPixels = 4f;
        private const int MarkerCircleSegments = 10;
        private const float MarkerPlayerSpacingPixels = 3.5f;
        private const float HoverRadiusPixels = 11f;
        private const float TooltipPadding = 7f;
        private const float TooltipOffset = 14f;
        private const float RebuildIntervalSeconds = 0.1f;
        private const float LevelCheckIntervalSeconds = 0.5f;
        private const int PrepareChunkSize = 8000;

        private static EditorHitMarkerController _host;

        internal static void EnsureHost()
        {
            if (_host != null) return;

            GameObject go = new GameObject("TimingShow_EditorHitMarkers");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _host = go.AddComponent<EditorHitMarkerController>();
        }

        internal static void DestroyHost()
        {
            EditorHitMarkerController host = _host;
            _host = null;

            if (host != null && host.gameObject != null)
                UnityEngine.Object.Destroy(host.gameObject);
        }

        private enum Phase
        {
            Idle,
            Preparing,
            Plotting,
            Ready
        }

        private struct VisibleHit
        {
            internal int SampleIndex;
            internal Vector2 Screen;
        }

        private EditorHitSnapshotData _snapshot;
        private Phase _phase;
        private int _prepareIndex;
        private bool[] _valid;
        private float _lastRebuildTime;
        private float _lastLevelCheckTime;
        private float _progressHideAt;

        private GameObject _markerObject;
        private Mesh _mesh;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Material _material;

        private readonly List<Vector3> _vertices = new List<Vector3>(4096);
        private readonly List<Color> _colors = new List<Color>(4096);
        private readonly List<int> _triangles = new List<int>(8192);
        private readonly List<VisibleHit> _visible = new List<VisibleHit>(1024);

        private GameObject _canvasObject;
        private RectTransform _tooltipRect;
        private GameObject _tooltipRoot;
        private TMP_Text _tooltipText;
        private Image _tooltipAccent;
        private int _hoverSampleIndex = -1;

        private GameObject _progressRoot;
        private RectTransform _progressFill;
        private TMP_Text _progressLabel;

        private readonly StringBuilder _builder = new StringBuilder(192);

        private void Update()
        {
            try
            {
                UpdateCore();
            }
            catch (Exception e)
            {
                ModContext.Logger?.Error("Editor hit markers: " + e.Message);
                EditorHitSnapshot.Clear();
            }
        }

        private void UpdateCore()
        {
            Settings settings = ModContext.Settings;
            if (settings == null || !settings.EditorHits_Enabled)
            {
                Teardown();
                return;
            }

            EditorHitSnapshotData snapshot = EditorHitSnapshot.Pending;
            if (snapshot == null)
            {
                Teardown();
                return;
            }

            scnEditor editor = scnEditor.instance;
            if (!IsEditorReady(editor))
            {
                SetUiVisible(false);
                return;
            }

            if (!ReferenceEquals(_snapshot, snapshot))
                Begin(snapshot);
            else if (_canvasObject != null && !_canvasObject.activeSelf)
                _canvasObject.SetActive(true);

            if (_phase == Phase.Preparing)
            {
                StepPrepare(editor);
                return;
            }

            if (Time.unscaledTime - _lastLevelCheckTime >= LevelCheckIntervalSeconds)
            {
                _lastLevelCheckTime = Time.unscaledTime;
                if (!_snapshot.MatchesEditor(editor))
                {
                    EditorHitSnapshot.Clear();
                    return;
                }
            }

            if (_phase == Phase.Plotting)
            {
                RebuildMarkers(editor);
                _phase = Phase.Ready;
                _progressHideAt = Time.unscaledTime + 0.2f;
            }
            else if (Time.unscaledTime - _lastRebuildTime >= RebuildIntervalSeconds)
            {
                RebuildMarkers(editor);
            }

            if (_progressHideAt > 0f && Time.unscaledTime >= _progressHideAt)
            {
                _progressHideAt = 0f;
                if (_progressRoot != null) _progressRoot.SetActive(false);
            }

            UpdateHover(editor);
        }

        private static bool IsEditorReady(scnEditor editor)
        {
            if (editor == null) return false;
            if (ModContext.IsPlaying) return false;

            try
            {
                return editor.camera != null && editor.floors != null && editor.floors.Count > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void Begin(EditorHitSnapshotData snapshot)
        {
            Teardown();

            _snapshot = snapshot;
            _phase = Phase.Preparing;
            _prepareIndex = 0;
            _lastRebuildTime = 0f;
            _lastLevelCheckTime = Time.unscaledTime;
            _valid = new bool[snapshot.Samples.Length];
            _hoverSampleIndex = -1;

            EnsureCanvas();
            if (_canvasObject != null) _canvasObject.SetActive(true);
            EnsureMarkers();
            ShowProgress(0, snapshot.Samples.Length);
        }

        private void StepPrepare(scnEditor editor)
        {
            int total = _snapshot.Samples.Length;
            List<scrFloor> floors;
            try { floors = editor.floors; }
            catch (Exception) { floors = null; }

            int end = Mathf.Min(total, _prepareIndex + PrepareChunkSize);
            for (; _prepareIndex < end; _prepareIndex++)
            {
                TimingScatterSample sample = _snapshot.Samples[_prepareIndex];
                bool valid = false;
                if (sample.HasJudge && floors != null && sample.FloorId >= 0 && sample.FloorId < floors.Count)
                {
                    scrFloor floor = floors[sample.FloorId];
                    valid = floor != null && floor.seqID == sample.FloorId;
                }

                _valid[_prepareIndex] = valid;
            }

            ShowProgress(_prepareIndex, total);

            if (_prepareIndex >= total)
                _phase = Phase.Plotting;
        }

        private void RebuildMarkers(scnEditor editor)
        {
            _lastRebuildTime = Time.unscaledTime;

            _vertices.Clear();
            _colors.Clear();
            _triangles.Clear();
            _visible.Clear();

            if (_snapshot == null || _mesh == null) return;

            Camera camera = editor.camera;
            List<scrFloor> floors;
            try { floors = editor.floors; }
            catch (Exception) { floors = null; }

            if (camera == null || floors == null) return;

            Settings settings = ModContext.Settings;
            float markerPixels = settings != null ? settings.EditorHits_MarkerSize : MarkerRadiusPixels;
            float angleScale = settings != null ? settings.EditorHits_AngleScale : 1f;

            Rect viewport = camera.pixelRect;
            float worldPerPixel = WorldPerPixel(camera);
            float radius = Mathf.Clamp(markerPixels, 1f, 12f) * worldPerPixel;
            float playerSpacing = MarkerPlayerSpacingPixels * worldPerPixel;
            Vector3 right = camera.transform.right;
            Vector3 up = camera.transform.up;
            Vector3 towardCamera = -camera.transform.forward;
            int playerCount = Mathf.Max(1, _snapshot.PlayerCount);

            for (int i = 0; i < _snapshot.Samples.Length; i++)
            {
                if (_valid == null || i >= _valid.Length || !_valid[i]) continue;

                TimingScatterSample sample = _snapshot.Samples[i];
                if (sample.FloorId < 0 || sample.FloorId >= floors.Count) continue;

                scrFloor floor = floors[sample.FloorId];
                if (floor == null || floor.seqID != sample.FloorId) continue;

                Vector3 world = AngleOffsetPosition(floor, sample, angleScale) + towardCamera * 0.15f;
                if (playerCount > 1)
                    world += up * ((sample.PlayerId - (playerCount - 1) * 0.5f) * playerSpacing);

                Vector3 screen = camera.WorldToScreenPoint(world);
                if (screen.z <= 0f) continue;
                if (screen.x < viewport.xMin - 12f || screen.x > viewport.xMax + 12f) continue;
                if (screen.y < viewport.yMin - 12f || screen.y > viewport.yMax + 12f) continue;

                AppendCircle(world, right, up, radius, MarkerColor(sample));

                VisibleHit hit = new VisibleHit();
                hit.SampleIndex = i;
                hit.Screen = new Vector2(screen.x, screen.y);
                _visible.Add(hit);
            }

            ApplyMesh();
        }

        // Places the marker on the circle the planet was orbiting when the hit was
        // judged. The floor heading (previous floor -> hit floor) is 0 degrees; the
        // signed hit angle rotates around it, so the point reproduces the input.
        private static Vector3 AngleOffsetPosition(scrFloor floor, TimingScatterSample sample, float scale)
        {
            Vector3 center = floor.transform.position;
            if (!sample.HasAngle || !IsFinite(sample.AngleDeg)) return center;

            scrFloor previous;
            try { previous = floor.prevfloor; }
            catch (Exception) { previous = null; }

            if (previous == null) return center;

            Vector3 previousPosition = previous.transform.position;
            float dx = center.x - previousPosition.x;
            float dy = center.y - previousPosition.y;
            float spacing = Mathf.Sqrt(dx * dx + dy * dy);
            if (spacing < 1e-5f) return center;

            float baseX = dx / spacing;
            float baseY = dy / spacing;

            bool clockwise = sample.HasOrbitDirection ? sample.OrbitClockwise : !previous.isCCW;
            float delta = (clockwise ? 1f : -1f) * sample.AngleDeg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(delta);
            float sin = Mathf.Sin(delta);

            float actualX = baseX * cos + baseY * sin;
            float actualY = baseY * cos - baseX * sin;

            float radiusScale = previous.radiusScale;
            if (radiusScale < 0.001f) radiusScale = 1f;

            float radius = spacing * radiusScale * Mathf.Clamp(scale, 0f, 3f);
            center.x += (actualX - baseX) * radius;
            center.y += (actualY - baseY) * radius;
            return center;
        }

        private static Color MarkerColor(TimingScatterSample sample)
        {
            return JColors.GetColor(sample.Judge, sample.IsXPerfect, true);
        }

        private void AppendCircle(Vector3 center, Vector3 right, Vector3 up, float radius, Color color)
        {
            int centerIndex = _vertices.Count;

            _vertices.Add(center);
            _colors.Add(color);

            for (int i = 0; i <= MarkerCircleSegments; i++)
            {
                float angle = i / (float)MarkerCircleSegments * Mathf.PI * 2f;
                Vector3 offset = right * (Mathf.Cos(angle) * radius) + up * (Mathf.Sin(angle) * radius);
                _vertices.Add(center + offset);
                _colors.Add(color);
            }

            for (int i = 0; i < MarkerCircleSegments; i++)
            {
                _triangles.Add(centerIndex);
                _triangles.Add(centerIndex + 1 + i);
                _triangles.Add(centerIndex + 2 + i);
            }
        }

        private void ApplyMesh()
        {
            if (_mesh == null) return;

            _mesh.Clear();
            if (_vertices.Count > 0)
            {
                _mesh.SetVertices(_vertices);
                _mesh.SetColors(_colors);
                _mesh.SetTriangles(_triangles, 0, true);
            }

            _mesh.RecalculateBounds();
        }

        private static float WorldPerPixel(Camera camera)
        {
            float pixels = Mathf.Max(1f, camera.pixelHeight);

            if (camera.orthographic)
                return camera.orthographicSize * 2f / pixels;

            float depth = Mathf.Max(0.01f, Mathf.Abs(camera.transform.position.z));
            return 2f * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * depth / pixels;
        }

        private void UpdateHover(scnEditor editor)
        {
            Settings settings = ModContext.Settings;
            if (settings == null || !settings.EditorHits_ShowTooltip)
            {
                HideTooltip();
                return;
            }

            Camera camera = editor.camera;
            if (camera == null)
            {
                HideTooltip();
                return;
            }

            Vector2 mouse = Input.mousePosition;
            Rect viewport = camera.pixelRect;
            if (mouse.x < viewport.xMin || mouse.x > viewport.xMax ||
                mouse.y < viewport.yMin || mouse.y > viewport.yMax)
            {
                HideTooltip();
                return;
            }

            int best = -1;
            float bestDistance = HoverRadiusPixels * HoverRadiusPixels;
            for (int i = 0; i < _visible.Count; i++)
            {
                float distance = (_visible[i].Screen - mouse).sqrMagnitude;
                if (distance > bestDistance) continue;
                bestDistance = distance;
                best = _visible[i].SampleIndex;
            }

            if (best < 0)
            {
                HideTooltip();
                return;
            }

            EnsureTooltip();

            if (best != _hoverSampleIndex)
            {
                _hoverSampleIndex = best;
                TimingScatterSample sample = _snapshot.Samples[best];
                _tooltipText.text = BuildTooltip(sample);
                if (_tooltipAccent != null)
                {
                    Color color = JColors.GetColor(sample.Judge, sample.IsXPerfect, true);
                    color.a = 1f;
                    _tooltipAccent.color = color;
                }
            }

            PositionTooltip(mouse, viewport);
            if (!_tooltipRoot.activeSelf) _tooltipRoot.SetActive(true);
        }

        private string BuildTooltip(TimingScatterSample sample)
        {
            _builder.Length = 0;

            _builder.Append(i18n.T("Detail_Player")).Append(": ").Append(Coop.Label(sample.PlayerId)).Append('\n');

            string floor = sample.HasFloorId
                ? (sample.FloorIdEstimated ? "~#" : "#") + sample.FloorId.ToString(CultureInfo.InvariantCulture)
                : i18n.T("Detail_Unknown");
            _builder.Append(i18n.T("Detail_Floor")).Append(": ").Append(floor).Append('\n');

            string timing = IsFinite(sample.OffsetMs)
                ? sample.OffsetMs.ToString("F2", CultureInfo.InvariantCulture) + " ms"
                : i18n.T("Detail_Unknown");
            string angle = sample.HasAngle && IsFinite(sample.AngleDeg)
                ? sample.AngleDeg.ToString("F2", CultureInfo.InvariantCulture) + "°"
                : i18n.T("Detail_Unknown");
            _builder.Append(i18n.T("Detail_Timing")).Append(": ").Append(timing)
                .Append("  ").Append(i18n.T("Detail_Angle")).Append(": ").Append(angle).Append('\n');

            if (sample.HasHitParams)
            {
                _builder.Append("BPM: ").Append(sample.Bpm.ToString("F1", CultureInfo.InvariantCulture))
                    .Append("  ×").Append(sample.Speed.ToString("F2", CultureInfo.InvariantCulture));
            }
            else
            {
                _builder.Append("BPM: ").Append(i18n.T("Detail_Unknown"));
            }
            _builder.Append('\n');

            Color judgeColor = JColors.GetColor(sample.Judge, sample.IsXPerfect, true);
            _builder.Append(i18n.T("Detail_Judge")).Append(": ")
                .Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(judgeColor)).Append('>')
                .Append(JudgeText(sample))
                .Append("</color>");

            return _builder.ToString();
        }

        private static string JudgeText(TimingScatterSample sample)
        {
            if (sample.IsXPerfect && !HitMarginCompat.IsGame34 && HitMarginCompat.IsPerfectFamily(sample.Judge))
                return i18n.T("Toggle_XPerfect");

            switch (sample.Judge)
            {
                case HitMan.TooEarly: return i18n.T("Toggle_TooEarly");
                case HitMan.VeryEarly: return i18n.T("Toggle_VeryEarly");
                case HitMan.EarlyPerfect: return i18n.T("Toggle_EarlyPerfect");
                case HitMan.PerfectMinus: return i18n.T(HitMarginCompat.IsNormalPerfect(sample.Judge) ? "Toggle_Perfect" : "Toggle_PerfectMinus");
                case HitMan.XPerfect: return i18n.T("Toggle_XPerfect");
                case HitMan.PerfectPlus: return i18n.T("Toggle_PerfectPlus");
                case HitMan.LatePerfect: return i18n.T("Toggle_LatePerfect");
                case HitMan.VeryLate: return i18n.T("Toggle_VeryLate");
                case HitMan.TooLate: return i18n.T("Toggle_TooLate");
                case HitMan.Multipress: return i18n.T("Toggle_Multipress");
                case HitMan.FailMiss: return i18n.T("Toggle_FailMiss");
                case HitMan.FailOverload: return i18n.T("Toggle_FailOverload");
                case HitMan.OverPress: return i18n.T("Toggle_OverPress");
                case HitMan.Auto: return i18n.T("Toggle_Auto");
                case HitMan.Midspin: return i18n.T("Judge_Midspin");
                case HitMan.FailedFloor: return i18n.T("Judge_FailedFloor");
                default: return i18n.T("Judge_Unknown");
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private void HideTooltip()
        {
            _hoverSampleIndex = -1;
            if (_tooltipRoot != null && _tooltipRoot.activeSelf) _tooltipRoot.SetActive(false);
        }

        private void SetUiVisible(bool visible)
        {
            if (_canvasObject != null && _canvasObject.activeSelf != visible) _canvasObject.SetActive(visible);
            if (!visible) HideTooltip();
        }

        private void EnsureCanvas()
        {
            if (_canvasObject != null) return;

            _canvasObject = new GameObject("TimingShow_EditorHitCanvas");
            _canvasObject.transform.SetParent(transform, false);

            Canvas canvas = _canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
        }

        private void EnsureMarkers()
        {
            if (_markerObject != null) return;

            _markerObject = new GameObject("TimingShow_EditorHitMarkers");
            _markerObject.transform.SetParent(transform, false);

            _mesh = new Mesh();
            _mesh.name = "TimingShow_EditorHitMarkers";
            _mesh.indexFormat = IndexFormat.UInt32;
            _mesh.MarkDynamic();

            _meshFilter = _markerObject.AddComponent<MeshFilter>();
            _meshFilter.sharedMesh = _mesh;

            _meshRenderer = _markerObject.AddComponent<MeshRenderer>();
            _meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _meshRenderer.receiveShadows = false;
            _meshRenderer.sortingOrder = 32000;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");
            _material = new Material(shader);
            _material.name = "TimingShow_EditorHitMarkers";
            _meshRenderer.sharedMaterial = _material;
        }

        private void EnsureTooltip()
        {
            if (_tooltipRoot != null) return;

            _tooltipRoot = new GameObject("HitTooltip", typeof(RectTransform), typeof(Image));
            _tooltipRoot.transform.SetParent(_canvasObject.transform, false);
            _tooltipRect = (RectTransform)_tooltipRoot.transform;
            _tooltipRect.anchorMin = new Vector2(1f, 1f);
            _tooltipRect.anchorMax = new Vector2(1f, 1f);
            _tooltipRect.pivot = new Vector2(0f, 1f);

            Image background = _tooltipRoot.GetComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.82f);
            background.raycastTarget = false;

            GameObject accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(_tooltipRoot.transform, false);
            RectTransform accentRect = (RectTransform)accent.transform;
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.pivot = new Vector2(0f, 0.5f);
            accentRect.offsetMin = new Vector2(0f, 0f);
            accentRect.offsetMax = new Vector2(3f, 0f);
            _tooltipAccent = accent.GetComponent<Image>();
            _tooltipAccent.raycastTarget = false;

            GameObject textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(_tooltipRoot.transform, false);
            RectTransform textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(TooltipPadding + 4f, TooltipPadding);
            textRect.offsetMax = new Vector2(-TooltipPadding, -TooltipPadding);

            _tooltipText = textObject.AddComponent<TextMeshProUGUI>();
            _tooltipText.raycastTarget = false;
            _tooltipText.richText = true;
            _tooltipText.fontSize = 14f;
            _tooltipText.color = Color.white;
            _tooltipText.alignment = TextAlignmentOptions.TopLeft;
            _tooltipText.textWrappingMode = TextWrappingModes.NoWrap;
            _tooltipText.overflowMode = TextOverflowModes.Overflow;
            TMP_FontAsset font = LogGraphFont.Resolve();
            _tooltipText.font = font != null ? font : TMP_Settings.defaultFontAsset;

            _tooltipRoot.SetActive(false);
        }

        private void PositionTooltip(Vector2 mouse, Rect viewport)
        {
            if (_tooltipText == null) return;

            Vector2 content = _tooltipText.GetPreferredValues(_tooltipText.text);
            float width = Mathf.Max(150f, content.x + TooltipPadding * 2f + 4f);
            float height = Mathf.Max(40f, content.y + TooltipPadding * 2f);
            _tooltipRect.sizeDelta = new Vector2(width, height);

            Vector2 position = mouse + new Vector2(TooltipOffset, -TooltipOffset);
            float minX = viewport.xMin + 2f;
            float maxX = Mathf.Max(minX, viewport.xMax - width - 2f);
            float minY = viewport.yMin + height + 2f;
            float maxY = viewport.yMax - 2f;

            position.x = Mathf.Clamp(position.x, minX, maxX);
            position.y = Mathf.Clamp(position.y, minY, maxY);
            _tooltipRect.position = new Vector3(position.x, position.y, 0f);
        }

        private void ShowProgress(int processed, int total)
        {
            EnsureProgress();
            if (_progressRoot == null) return;

            if (!_progressRoot.activeSelf) _progressRoot.SetActive(true);
            _progressHideAt = 0f;

            float ratio = total > 0 ? Mathf.Clamp01((float)processed / total) : 1f;
            if (_progressFill != null)
            {
                Vector2 max = _progressFill.anchorMax;
                max.x = ratio;
                _progressFill.anchorMax = max;
            }

            if (_progressLabel != null)
            {
                string text = string.Format(CultureInfo.InvariantCulture, i18n.T("EditorHits_Progress"), processed, total);
                if (_snapshot != null && _snapshot.Truncated)
                    text += i18n.T("EditorHits_Truncated");
                if (_progressLabel.text != text) _progressLabel.text = text;
            }

            PositionProgress();
        }

        private void EnsureProgress()
        {
            if (_progressRoot != null) return;

            _progressRoot = new GameObject("HitMarkersProgress", typeof(RectTransform), typeof(Image));
            _progressRoot.transform.SetParent(_canvasObject.transform, false);
            RectTransform rootRect = (RectTransform)_progressRoot.transform;
            rootRect.anchorMin = new Vector2(0.5f, 0f);
            rootRect.anchorMax = new Vector2(0.5f, 0f);
            rootRect.pivot = new Vector2(0.5f, 0f);
            rootRect.sizeDelta = new Vector2(360f, 26f);

            Image background = _progressRoot.GetComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.72f);
            background.raycastTarget = false;

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(_progressRoot.transform, false);
            _progressFill = (RectTransform)fill.transform;
            _progressFill.anchorMin = Vector2.zero;
            _progressFill.anchorMax = new Vector2(0f, 1f);
            _progressFill.pivot = new Vector2(0f, 0.5f);
            _progressFill.offsetMin = new Vector2(2f, 2f);
            _progressFill.offsetMax = new Vector2(-2f, -2f);
            Image fillImage = fill.GetComponent<Image>();
            fillImage.color = new Color(0.30f, 0.76f, 1f, 0.55f);
            fillImage.raycastTarget = false;

            GameObject label = new GameObject("Label", typeof(RectTransform));
            label.transform.SetParent(_progressRoot.transform, false);
            RectTransform labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(6f, 2f);
            labelRect.offsetMax = new Vector2(-6f, -2f);

            _progressLabel = label.AddComponent<TextMeshProUGUI>();
            _progressLabel.raycastTarget = false;
            _progressLabel.fontSize = 14f;
            _progressLabel.color = Color.white;
            _progressLabel.alignment = TextAlignmentOptions.Center;
            _progressLabel.textWrappingMode = TextWrappingModes.NoWrap;
            _progressLabel.overflowMode = TextOverflowModes.Ellipsis;
            TMP_FontAsset font = LogGraphFont.Resolve();
            _progressLabel.font = font != null ? font : TMP_Settings.defaultFontAsset;

            _progressRoot.SetActive(false);
        }

        private void PositionProgress()
        {
            if (_progressRoot == null) return;

            Camera camera = scnEditor.instance != null ? scnEditor.instance.camera : null;
            Rect viewport = camera != null
                ? camera.pixelRect
                : new Rect(0f, 0f, Screen.width, Screen.height);

            RectTransform rect = (RectTransform)_progressRoot.transform;
            rect.position = new Vector3(viewport.center.x, viewport.yMin + 20f, 0f);
        }

        private void Teardown()
        {
            if (_phase == Phase.Idle && _snapshot == null) return;

            _snapshot = null;
            _valid = null;
            _phase = Phase.Idle;
            _prepareIndex = 0;
            _hoverSampleIndex = -1;
            _progressHideAt = 0f;

            _visible.Clear();
            _vertices.Clear();
            _colors.Clear();
            _triangles.Clear();

            if (_canvasObject != null) UnityEngine.Object.Destroy(_canvasObject);
            if (_markerObject != null) UnityEngine.Object.Destroy(_markerObject);
            if (_mesh != null) UnityEngine.Object.Destroy(_mesh);
            if (_material != null) UnityEngine.Object.Destroy(_material);

            _canvasObject = null;
            _markerObject = null;
            _mesh = null;
            _meshFilter = null;
            _meshRenderer = null;
            _material = null;
            _tooltipRoot = null;
            _tooltipRect = null;
            _tooltipText = null;
            _tooltipAccent = null;
            _progressRoot = null;
            _progressFill = null;
            _progressLabel = null;
        }

        private void OnDestroy()
        {
            if (_mesh != null) UnityEngine.Object.Destroy(_mesh);
            if (_material != null) UnityEngine.Object.Destroy(_material);
            if (_host == this) _host = null;
        }
    }
}
