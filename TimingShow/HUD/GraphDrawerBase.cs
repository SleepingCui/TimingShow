using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TimingShow.HUD
{
    [RequireComponent(typeof(CanvasRenderer))]
    public abstract class GraphDrawerBase : MaskableGraphic
    {
        protected abstract bool IsEnabled { get; }
        protected abstract bool ShowGraph { get; }
        protected abstract float Scale { get; }
        protected abstract float Width { get; }
        protected abstract float Height { get; }
        protected abstract float PosX { get; }
        protected abstract float PosY { get; }
        protected abstract Color BgColor { get; }
        protected abstract Color GridColor { get; }
        protected abstract Color LineColor { get; }
        protected abstract int MaxPoints { get; }
        public abstract string GraphName { get; }
        protected virtual int DataVersion => ModContext.XAccVersion;
        
        protected virtual bool UseCustomFont => false;
        protected virtual string FontPath => "";
        protected virtual bool AllowGameFontFallback => true;

        protected virtual bool ShowPerfInfo => false;
        protected virtual int PerfDataCount => GetDataCount();
        protected virtual string PerfExtraInfo => null;
        protected virtual Color PerfTextColor => new Color32(0, 255, 0, 255);   

        protected abstract void UpdateData();
        protected abstract int GetDataCount();
        protected abstract float GetDataValue(int index);
        protected abstract float GetMinY();
        protected abstract float GetMaxY();

        protected TMP_Text _titleText;
        protected TMP_Text _topLabelText;
        protected TMP_Text _tidLabelText;
        protected TMP_Text _botLabelText;
        protected TMP_Text _perfText;
        protected TMP_FontAsset CurrentFontAsset { get; private set; }

        private readonly System.Diagnostics.Stopwatch _perfStopwatch = new System.Diagnostics.Stopwatch();
        private float _perfUpdateMs;
        private int _perfVertexCount;
        private float _perfFps;
        private int _perfRedrawCount;
        private float _perfRedrawWindow;
        private float _perfRedrawsPerSec;

        private static readonly Dictionary<string, TMP_FontAsset> FontCache = new Dictionary<string, TMP_FontAsset>();
        private string _appliedFontKey;

        private int _lastDataVersion = -1;
        private int _lastSettingsHash = int.MinValue;
        private float _lastRectWidth = -1f;
        private float _lastRectHeight = -1f;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
            material = defaultMaterial;
            CreateTextComponents();
        }

        protected virtual void CreateTextComponents()
        {
            CurrentFontAsset = ResolveGraphFont();

            _titleText = CreateText("TitleText", CurrentFontAsset, TextAnchor.UpperLeft);
            _topLabelText = CreateText("TopLabel", CurrentFontAsset, TextAnchor.MiddleRight);
            _tidLabelText = CreateText("MidLabel", CurrentFontAsset, TextAnchor.MiddleRight);
            _botLabelText = CreateText("BotLabel", CurrentFontAsset, TextAnchor.MiddleRight);
            _perfText = CreateText("PerfInfoLabel", CurrentFontAsset, TextAnchor.UpperLeft);
        }
        

        protected virtual TMP_FontAsset ResolveGraphFont()
        {
            string path = UseCustomFont ? (FontPath ?? "").Trim() : "";
            string key = FontCacheKey(path);

            if (FontCache.TryGetValue(key, out TMP_FontAsset cached)) return cached;

            TMP_FontAsset asset = path.Length > 0 ? LoadFontFromPath(path) : null;
            if (asset == null && AllowGameFontFallback) asset = FindGameFont();

            FontCache[key] = asset;
            return asset;
        }

        private string FontCacheKey(string path)
        {
            if (!string.IsNullOrEmpty(path)) return "custom:" + path;
            return AllowGameFontFallback ? "game" : "system";
        }

        protected void ApplyGraphFont()
        {
            TMP_FontAsset asset = ResolveGraphFont();
            if (asset != null) CurrentFontAsset = asset;

            string path = UseCustomFont ? (FontPath ?? "").Trim() : "";
            string key = FontCacheKey(path);
            if (_appliedFontKey == key) return;

            _appliedFontKey = key;
            ApplyFontToTexts(asset);
        }
        
        protected virtual void ApplyFontToTexts(TMP_FontAsset font)
        {
            ApplyFontToText(_titleText, font);
            ApplyFontToText(_topLabelText, font);
            ApplyFontToText(_tidLabelText, font);
            ApplyFontToText(_botLabelText, font);
            ApplyFontToText(_perfText, font);
        }
        
        protected static void ApplyFontToText(TMP_Text text, TMP_FontAsset font)
        {
            if (text == null || font == null) return;

            text.font = font;
            text.SetVerticesDirty();
            text.SetLayoutDirty();
        }
        
        private static TMP_FontAsset LoadFontFromPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                ModContext.Logger?.Log("Graph font file does not exist: " + path);
                return null;
            }

            string extension = Path.GetExtension(path);
            if (!string.Equals(extension, ".ttf", System.StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".otf", System.StringComparison.OrdinalIgnoreCase))
            {
                ModContext.Logger?.Log("Unsupported graph font file: " + path);
                return null;
            }

            try
            {
                Font sourceFont = new Font();
                MethodInfo loadFromPath = typeof(Font).GetMethod("Internal_CreateFontFromPath", BindingFlags.Static | BindingFlags.NonPublic);
                if (loadFromPath == null)
                {
                    ModContext.Logger?.Log("Unity does not expose font loading from path");
                    return null;
                }

                loadFromPath.Invoke(null, new object[] { sourceFont, path });

                TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont);
                if (fontAsset == null)
                {
                    ModContext.Logger?.Log("Failed to build graph font asset: " + path);
                    return null;
                }

                fontAsset.name = "TimingShow_GraphFont_" + Path.GetFileNameWithoutExtension(path);
                Object.DontDestroyOnLoad(fontAsset);
                return fontAsset;
            }
            catch (System.Exception e)
            {
                ModContext.Logger?.Log("Failed to load graph font: " + e.Message);
                return null;
            }
        }
        
        private static TMP_FontAsset FindGameFont()
        {
            try
            {
                scrHitTextMesh[] hitTexts = Resources.FindObjectsOfTypeAll<scrHitTextMesh>();
                for (int i = 0; i < hitTexts.Length; i++)
                {
                    if (hitTexts[i] != null && hitTexts[i].text != null && hitTexts[i].text.font != null)
                        return hitTexts[i].text.font;
                }
            }
            catch (System.Exception e)
            {
                ModContext.Logger?.Log("Failed to find game graph font: " + e.Message);
            }

            return TMP_Settings.defaultFontAsset;
        }
        
        private static TextAlignmentOptions ToTextAlignment(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.MidlineLeft;
                case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.MidlineRight;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.MidlineRight;
            }
        }

        protected TMP_Text CreateText(string name, TMP_FontAsset font, TextAnchor alignment)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(1f, 0.5f);

            TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
            t.font = font != null ? font : TMP_Settings.defaultFontAsset;
            t.alignment = ToTextAlignment(alignment);
            t.color = Color.white;
            t.raycastTarget = false;
            t.richText = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;

            return t;
        }

        protected virtual void Update()
        {
            if (!IsEnabled || !ShowGraph)
            {
                if (!canvasRenderer.cull) canvasRenderer.cull = true;
                ToggleTexts(false);
                return;
            }

            canvasRenderer.cull = false;
            ToggleTexts(true);
            UpdateTransform();

            Rect rect = rectTransform.rect;
            bool rectChanged = rect.width != _lastRectWidth || rect.height != _lastRectHeight;
            int settingsHash = ComputeSettingsHash();
            bool continuousRedraw = NeedsContinuousRedraw();
            if (_lastDataVersion != DataVersion || rectChanged || settingsHash != _lastSettingsHash || continuousRedraw)
            {
                _lastDataVersion = DataVersion;
                _lastSettingsHash = settingsHash;
                _lastRectWidth = rect.width;
                _lastRectHeight = rect.height;
                _perfStopwatch.Restart();
                UpdateData();
                _perfStopwatch.Stop();
                _perfUpdateMs = (float)_perfStopwatch.Elapsed.TotalMilliseconds;
                UpdateTextLayoutAndValues();
                SetVerticesDirty();
            }
            else if (ShowPerfInfo)
            {
                UpdatePerfText();
            }

            if (UpdateFrameAnimation())
            {
                UpdateTextLayoutAndValues();
                SetVerticesDirty();
            }
        }
        
        protected virtual bool UpdateFrameAnimation() => false;

        protected virtual bool NeedsContinuousRedraw() => false;

        protected virtual int ComputeSettingsHash()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Scale.GetHashCode();
                hash = hash * 31 + Width.GetHashCode();
                hash = hash * 31 + Height.GetHashCode();
                hash = hash * 31 + PosX.GetHashCode();
                hash = hash * 31 + PosY.GetHashCode();
                hash = hash * 31 + MaxPoints;
                hash = hash * 31 + BgColor.GetHashCode();
                hash = hash * 31 + GridColor.GetHashCode();
                hash = hash * 31 + LineColor.GetHashCode();
                hash = hash * 31 + (UseCustomFont ? 1 : 0);
                hash = hash * 31 + (FontPath ?? "").GetHashCode();
                hash = hash * 31 + (ShowPerfInfo ? 1 : 0);
                return hash;
            }
        }

        protected virtual void ToggleTexts(bool active)
        {
            if (_titleText != null && _titleText.gameObject.activeSelf != active) _titleText.gameObject.SetActive(active);
            if (_topLabelText != null && _topLabelText.gameObject.activeSelf != active) _topLabelText.gameObject.SetActive(active);
            if (_tidLabelText != null && _tidLabelText.gameObject.activeSelf != active) _tidLabelText.gameObject.SetActive(active);
            if (_botLabelText != null && _botLabelText.gameObject.activeSelf != active) _botLabelText.gameObject.SetActive(active);

            bool perfActive = active && ShowPerfInfo;
            if (_perfText != null && _perfText.gameObject.activeSelf != perfActive) _perfText.gameObject.SetActive(perfActive);
        }

        protected virtual void UpdateTransform()
        {
            RectTransform rect = rectTransform;
            float scale = Mathf.Max(0.01f, Scale);
            float w = Width * scale;
            float h = Height * scale;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.sizeDelta = new Vector2(w, h);

            float posX = Screen.width * PosX;
            float posY = Screen.height * (1.0f - PosY);
            rect.anchoredPosition = new Vector2(posX, posY);
        }

        protected virtual string FormatYLabel(float value) => $"{value:F1}%";

        private float _axisLabelWidth = -1f;
        private float _axisFooterHeight = -1f;

        public float AxisLabelWidth => Mathf.Max(0f, _axisLabelWidth);

        public float AxisFooterHeight => Mathf.Max(0f, _axisFooterHeight);

        private float _axisBandLeft;
        private float _axisBandBottom;
        private float _axisBandRight;
        private float _axisBandTop;

        protected float AxisBandLeft => _axisBandLeft;

        protected float AxisBandBottom => _axisBandBottom;

        protected float AxisBandRight => _axisBandRight;

        protected float AxisBandTop => _axisBandTop;

        public void SetAxisBands(float left, float bottom, float right, float top)
        {
            float l = Mathf.Max(0f, left);
            float b = Mathf.Max(0f, bottom);
            float r = Mathf.Max(0f, right);
            float t = Mathf.Max(0f, top);
            if (Mathf.Approximately(l, _axisBandLeft) && Mathf.Approximately(b, _axisBandBottom)
                && Mathf.Approximately(r, _axisBandRight) && Mathf.Approximately(t, _axisBandTop)) return;

            _axisBandLeft = l;
            _axisBandBottom = b;
            _axisBandRight = r;
            _axisBandTop = t;

            UpdateTextLayoutAndValues();
            SetVerticesDirty();
        }

        protected float AxisLabelGap => 6f * Mathf.Max(0.01f, Scale);

        protected virtual bool AxisLabelsInside => false;

        protected void ReportAxisLabelWidth(float width)
        {
            float quantized = QuantizeAxisMetric(width);
            if (Mathf.Approximately(quantized, _axisLabelWidth)) return;

            _axisLabelWidth = quantized;
            OnAxisMetricsChanged();
        }

        protected void ReportAxisFooterHeight(float height)
        {
            float quantized = QuantizeAxisMetric(height);
            if (Mathf.Approximately(quantized, _axisFooterHeight)) return;

            _axisFooterHeight = quantized;
            OnAxisMetricsChanged();
        }

        private static float QuantizeAxisMetric(float value) => value <= 1f ? 0f : Mathf.Ceil(value / 4f) * 4f;

        protected virtual void OnAxisMetricsChanged()
        {
        }

        protected static float MeasureTextWidth(TMP_Text text, string content, float fontSize)
        {
            if (text != null && text.font != null && !string.IsNullOrEmpty(content))
            {
                try
                {
                    Vector2 preferred = text.GetPreferredValues(content);
                    if (preferred.x > 0.5f && preferred.x < 16384f && !float.IsNaN(preferred.x)) return preferred.x;
                }
                catch (System.Exception)
                {
                }
            }

            int length = content != null ? content.Length : 0;
            return Mathf.Max(1f, length * Mathf.Max(1f, fontSize) * 0.62f);
        }

        protected virtual void UpdateTextLayoutAndValues()
        {
            ApplyGraphFont();

            float scale = Mathf.Max(0.01f, Scale);
            float w = rectTransform.rect.width;
            float h = rectTransform.rect.height;
            float minY = GetMinY();
            float maxY = GetMaxY();
            float midY = (minY + maxY) * 0.5f;

            if (_titleText != null)
            {
                int titleFontSize = Mathf.Clamp(Mathf.RoundToInt(h * 0.1f), 10, 100);
                _titleText.fontSize = titleFontSize;
                _titleText.text = GraphName;
                _titleText.color = new Color(1f, 1f, 1f, 102f / 255f);

                RectTransform rt = _titleText.rectTransform;
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(6f * scale, h - 2f * scale);
                rt.sizeDelta = new Vector2(w * 0.8f, h * 0.4f);
            }

            int scaleFontSize = Mathf.Clamp(Mathf.RoundToInt(12 * scale), 8, 32);
            Color scaleColor = new Color(1f, 1f, 1f, 0.8f);
            string topLabel = FormatYLabel(maxY);
            string midLabel = FormatYLabel(midY);
            string botLabel = FormatYLabel(minY);

            PrepareLeftScaleText(_topLabelText, topLabel, scaleFontSize, scaleColor);
            PrepareLeftScaleText(_tidLabelText, midLabel, scaleFontSize, scaleColor);
            PrepareLeftScaleText(_botLabelText, botLabel, scaleFontSize, scaleColor);

            float measureWidth = Mathf.Max(
                MeasureTextWidth(_topLabelText, topLabel, scaleFontSize),
                Mathf.Max(
                    MeasureTextWidth(_tidLabelText, midLabel, scaleFontSize),
                    MeasureTextWidth(_botLabelText, botLabel, scaleFontSize)));

            float boxWidth = measureWidth;
            if (_axisBandLeft > 1f) boxWidth = Mathf.Min(boxWidth, Mathf.Max(1f, _axisBandLeft - AxisLabelGap));

            float boxHeight = 24f * (scaleFontSize / 12f);

            if (AxisLabelsInside)
            {
                float maxHeight = Mathf.Max(1f, h);
                float topBoxHeight = Mathf.Min(boxHeight, maxHeight);
                float bottomBoxHeight = Mathf.Min(boxHeight, maxHeight);
                float midBoxHeight = Mathf.Min(boxHeight, maxHeight);
                float topCenter = Mathf.Max(h - topBoxHeight * 0.5f, h * 0.5f);
                float bottomCenter = Mathf.Min(bottomBoxHeight * 0.5f, h * 0.5f);

                PlaceLeftScaleText(_topLabelText, new Vector2(AxisLabelGap, topCenter), boxWidth, topBoxHeight);
                PlaceLeftScaleText(_tidLabelText, new Vector2(AxisLabelGap, h * 0.5f), boxWidth, midBoxHeight);
                PlaceLeftScaleText(_botLabelText, new Vector2(AxisLabelGap, bottomCenter), boxWidth, bottomBoxHeight);
            }
            else
            {
                float topBoxHeight = boxHeight;
                float bottomBoxHeight = boxHeight;
                if (_axisBandTop > 1f) topBoxHeight = Mathf.Min(boxHeight, Mathf.Max(1f, 2f * _axisBandTop));
                if (_axisBandBottom > 1f) bottomBoxHeight = Mathf.Min(boxHeight, Mathf.Max(1f, 2f * _axisBandBottom));
                float midBoxHeight = boxHeight;
                if (_axisBandTop > 1f && _axisBandBottom > 1f)
                {
                    float allowedHeight = 2f * Mathf.Min(_axisBandTop, _axisBandBottom) + h;
                    midBoxHeight = Mathf.Min(midBoxHeight, Mathf.Max(1f, allowedHeight));
                }

                PlaceLeftScaleText(_topLabelText, new Vector2(-AxisLabelGap, h), boxWidth, topBoxHeight);
                PlaceLeftScaleText(_tidLabelText, new Vector2(-AxisLabelGap, h * 0.5f), boxWidth, midBoxHeight);
                PlaceLeftScaleText(_botLabelText, new Vector2(-AxisLabelGap, 0f), boxWidth, bottomBoxHeight);
            }

            ReportAxisLabelWidth(measureWidth);

            UpdatePerfText();
        }

        protected virtual float PerfTextOffsetY(float scale, int fontSize) => -3f * scale;

        protected virtual string BuildPerfText()
        {
            string text = string.Format(
                "Pts {0}/{1} | Vtx {2} | Upd {3:F2}ms | Rebuild {4:F0}/s | {5:F0} FPS",
                PerfDataCount, GetDataCount(), _perfVertexCount, _perfUpdateMs, _perfRedrawsPerSec, _perfFps);

            string extra = PerfExtraInfo;
            if (!string.IsNullOrEmpty(extra)) text += " | " + extra;
            return text;
        }

        private void UpdatePerfText()
        {
            if (_perfText == null) return;

            if (!ShowPerfInfo)
            {
                if (_perfText.gameObject.activeSelf) _perfText.gameObject.SetActive(false);
                return;
            }

            float dt = Time.unscaledDeltaTime;
            if (dt > 0.0001f)
            {
                float instant = 1f / dt;
                _perfFps = _perfFps <= 0f ? instant : Mathf.Lerp(_perfFps, instant, 0.05f);
            }

            _perfRedrawWindow += dt;
            if (_perfRedrawWindow >= 0.5f)
            {
                _perfRedrawsPerSec = _perfRedrawCount / _perfRedrawWindow;
                _perfRedrawCount = 0;
                _perfRedrawWindow = 0f;
            }

            float scale = Mathf.Max(0.01f, Scale);
            int fontSize = Mathf.Clamp(Mathf.RoundToInt(11 * scale), 8, 32);
            float lineHeight = 16f * (fontSize / 12f);

            _perfText.fontSize = fontSize;
            _perfText.color = PerfTextColor;
            _perfText.alignment = TextAlignmentOptions.TopLeft;

            string text = BuildPerfText();
            if (_perfText.text != text) _perfText.text = text;

            RectTransform rt = _perfText.rectTransform;
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(2f * scale, PerfTextOffsetY(scale, fontSize));
            rt.sizeDelta = new Vector2(560f * (fontSize / 12f), lineHeight);

            if (!_perfText.gameObject.activeSelf) _perfText.gameObject.SetActive(true);
        }

        protected void PrepareLeftScaleText(TMP_Text t, string content, int fontSize, Color color)
        {
            if (t == null) return;
            t.fontSize = fontSize;
            t.text = content;
            t.color = color;
            t.alignment = AxisLabelsInside ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.MidlineRight;
        }

        protected void PlaceLeftScaleText(TMP_Text t, Vector2 localPos, float boxWidth, float boxHeight = 0f)
        {
            if (t == null) return;

            float height = boxHeight > 0f ? boxHeight : 24f * (t.fontSize / 12f);

            RectTransform rt = t.rectTransform;
            rt.pivot = AxisLabelsInside ? new Vector2(0f, 0.5f) : new Vector2(1f, 0.5f);
            rt.anchoredPosition = localPos;
            rt.sizeDelta = new Vector2(Mathf.Max(1f, boxWidth), Mathf.Max(1f, height));
        }

        protected void SetupLeftScaleText(TMP_Text t, string content, Vector2 localPos, int fontSize, Color color)
        {
            PrepareLeftScaleText(t, content, fontSize, color);

            float boxWidth = MeasureTextWidth(t, content, fontSize);
            if (_axisBandLeft > 1f) boxWidth = Mathf.Min(boxWidth, Mathf.Max(1f, _axisBandLeft - AxisLabelGap));
            PlaceLeftScaleText(t, localPos, boxWidth);
        }

        public static bool TrackMeshBounds;

        public bool TryGetMeshBounds(out Vector2 min, out Vector2 max)
        {
            min = _meshBoundsMin;
            max = _meshBoundsMax;
            return _meshBoundsValid;
        }

        private Vector2 _meshBoundsMin;
        private Vector2 _meshBoundsMax;
        private bool _meshBoundsValid;
        private static readonly List<UIVertex> MeshBoundsScratch = new List<UIVertex>(4096);

        private void MeasureMeshBounds(VertexHelper vh)
        {
            MeshBoundsScratch.Clear();
            vh.GetUIVertexStream(MeshBoundsScratch);

            if (MeshBoundsScratch.Count == 0)
            {
                _meshBoundsValid = false;
                return;
            }

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            for (int i = 0; i < MeshBoundsScratch.Count; i++)
            {
                Vector3 p = MeshBoundsScratch[i].position;
                if (p.x < min.x) min.x = p.x;
                if (p.y < min.y) min.y = p.y;
                if (p.x > max.x) max.x = p.x;
                if (p.y > max.y) max.y = p.y;
            }

            _meshBoundsMin = min;
            _meshBoundsMax = max;
            _meshBoundsValid = true;
        }

        public virtual void CollectBoundsRectangles(List<KeyValuePair<string, RectTransform>> targets)
        {
            AddBoundsTarget(targets, "TitleText", _titleText);
            AddBoundsTarget(targets, "YAxisTopLabel", _topLabelText);
            AddBoundsTarget(targets, "YAxisMidLabel", _tidLabelText);
            AddBoundsTarget(targets, "YAxisBottomLabel", _botLabelText);
            AddBoundsTarget(targets, "PerfInfoLabel", _perfText);
        }

        protected static void AddBoundsTarget(List<KeyValuePair<string, RectTransform>> targets, string name, TMP_Text text)
        {
            if (targets == null || text == null) return;
            if (!text.gameObject.activeInHierarchy) return;
            targets.Add(new KeyValuePair<string, RectTransform>(name, text.rectTransform));
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            float w = rectTransform.rect.width;
            float h = rectTransform.rect.height;

            if (w <= 0 || h <= 0)
            {
                _meshBoundsValid = false;
                return;
            }

            DrawQuad(vh, Vector2.zero, new Vector2(w, h), BgColor);
            DrawGridLines(vh, w, h);

            DrawReferenceLines(vh, w, h);
            DrawSeries(vh, w, h);

            _perfVertexCount = vh.currentVertCount;
            _perfRedrawCount++;

            if (TrackMeshBounds) MeasureMeshBounds(vh);
        }

        protected virtual void DrawReferenceLines(VertexHelper vh, float w, float h)
        {
        }

        protected virtual void DrawSeries(VertexHelper vh, float w, float h)
        {
            int count = GetDataCount();
            if (count < 2) return;

            float minY = GetMinY();
            float maxY = GetMaxY();
            float rangeY = Mathf.Max(0.01f, maxY - minY);
            int maxCapacity = MaxPoints > 0 ? MaxPoints : 250;
            float stepX = w / Mathf.Max(1, maxCapacity - 1);
            float lineWidth = 2.0f * Scale;

            Vector2 prevPoint = Vector2.zero;

            for (int i = 0; i < count; i++)
            {
                float normY = Mathf.Clamp01((GetDataValue(i) - minY) / rangeY);
                Vector2 curPoint = new Vector2(i * stepX, normY * h);
                if (i > 0)
                    DrawSegment(vh, prevPoint, curPoint, lineWidth * 0.5f, LineColor);
                prevPoint = curPoint;
            }
        }

        protected void DrawQuad(VertexHelper vh, Vector2 min, Vector2 max, Color color)
        {
            int baseIdx = vh.currentVertCount;
            vh.AddVert(new Vector3(min.x, min.y), color, Vector2.zero);
            vh.AddVert(new Vector3(min.x, max.y), color, Vector2.zero);
            vh.AddVert(new Vector3(max.x, max.y), color, Vector2.zero);
            vh.AddVert(new Vector3(max.x, min.y), color, Vector2.zero);
            vh.AddTriangle(baseIdx, baseIdx + 1, baseIdx + 2);
            vh.AddTriangle(baseIdx, baseIdx + 2, baseIdx + 3);
        }

        protected void DrawSegment(VertexHelper vh, Vector2 p1, Vector2 p2, float halfWidth, Color color)
        {
            Vector2 dir = (p2 - p1).normalized;
            if (dir == Vector2.zero) return;
            Vector2 normal = new Vector2(-dir.y, dir.x) * halfWidth;

            int baseIdx = vh.currentVertCount;
            vh.AddVert(p1 - normal, color, Vector2.zero);
            vh.AddVert(p1 + normal, color, Vector2.zero);
            vh.AddVert(p2 + normal, color, Vector2.zero);
            vh.AddVert(p2 - normal, color, Vector2.zero);
            vh.AddTriangle(baseIdx, baseIdx + 1, baseIdx + 2);
            vh.AddTriangle(baseIdx, baseIdx + 2, baseIdx + 3);
        }


        protected virtual float GridAlphaScale => 1f;
        
        protected virtual void DrawGridLines(VertexHelper vh, float w, float h)
        {
            float halfGridWidth = 1.0f * Scale * 0.5f;
            Color color = GridColor;
            color.a *= Mathf.Clamp01(GridAlphaScale);
            DrawSegment(vh, new Vector2(0, h * 0.5f), new Vector2(w, h * 0.5f), halfGridWidth, color);
        }
    }}