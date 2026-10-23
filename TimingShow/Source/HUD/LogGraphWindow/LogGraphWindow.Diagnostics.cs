using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace TimingShow.HUD.LogGraphWindow
{
    public partial class LogGraphWindow
    {
        private static bool DiagnoseGraphBounds
        {
            get
            {
                Settings settings = ModContext.Settings;
                return settings != null && settings.Diag_GraphBounds;
            }
        }

        private static bool DiagnoseTrackMeshBounds
        {
            get
            {
                Settings settings = ModContext.Settings;
                return settings != null && settings.Diag_TrackMeshBounds;
            }
        }

        private static float BoundsTolerance
        {
            get
            {
                Settings settings = ModContext.Settings;
                return Mathf.Clamp(settings != null ? settings.Diag_BoundsTolerancePx : 0.5f, 0f, 8f);
            }
        }

        private static float BoundsCheckInterval
        {
            get
            {
                Settings settings = ModContext.Settings;
                return Mathf.Clamp(settings != null ? settings.Diag_BoundsIntervalSec : 1f, 0.05f, 10f);
            }
        }

        private static int BoundsLogLimit
        {
            get
            {
                Settings settings = ModContext.Settings;
                return Mathf.Clamp(settings != null ? settings.Diag_BoundsLogLimit : 40, 1, 500);
            }
        }

        private static float BoundsLogRepeatSeconds
        {
            get
            {
                Settings settings = ModContext.Settings;
                return Mathf.Clamp(settings != null ? settings.Diag_BoundsLogRepeatSec : 5f, 0f, 60f);
            }
        }

        private static readonly List<KeyValuePair<string, RectTransform>> BoundsTargets = new List<KeyValuePair<string, RectTransform>>(24);
        private static readonly Dictionary<string, float> BoundsLoggedAt = new Dictionary<string, float>();
        private static readonly Vector3[] BoundsCorners = new Vector3[4];
        private static int _boundsLogCount;
        private static bool _boundsActivated;
        private float _boundsNextCheckAt;

        private void UpdateBoundsDiagnostics()
        {
            bool wantMeshBounds = DiagnoseGraphBounds || DiagnoseTrackMeshBounds;
            if (GraphDrawerBase.TrackMeshBounds != wantMeshBounds) GraphDrawerBase.TrackMeshBounds = wantMeshBounds;

            if (!DiagnoseGraphBounds || _windowRect == null || _panel == null) return;
            if (Time.unscaledTime < _boundsNextCheckAt) return;
            _boundsNextCheckAt = Time.unscaledTime + BoundsCheckInterval;

            if (!_boundsActivated)
                _boundsActivated = true;

            ValidateGraphBounds(Time.unscaledTime);
        }

        private void ValidateGraphBounds(float now)
        {
            Rect window = ScreenRectOf(_windowRect);
            Rect container = ScreenRectOf(_graphContainerRect);
            if (_graphLayoutResolved)
            {
                ReportLayoutMismatch("GraphContainer", container, ExpectedScreenRect(_resolvedContainerRect), now);
                ReportLayoutMismatch("PlotArea", ScreenRectOf(_plotAreaRect), ExpectedScreenRect(_resolvedPlotRect), now);
            }

            BoundsTargets.Clear();
            AddBoundsTarget("GraphContainer", _graphContainerRect);
            AddBoundsTarget("PlotArea", _plotAreaRect);
            if (_drawer != null) _drawer.CollectBoundsRectangles(BoundsTargets);

            for (int i = 0; i < BoundsTargets.Count; i++)
            {
                KeyValuePair<string, RectTransform> target = BoundsTargets[i];
                Rect bounds = ScreenRectOf(target.Value);

                ReportBoundsOverflow(target.Key, bounds, window, now, "window");
                if (!ReferenceEquals(target.Value, _graphContainerRect))
                    ReportBoundsOverflow(target.Key, bounds, container, now, "container");
            }

            Vector2 meshMin;
            Vector2 meshMax;
            if (_drawer != null && _graphRect != null && _drawer.TryGetMeshBounds(out meshMin, out meshMax))
            {
                Vector3 min = _graphRect.TransformPoint(new Vector3(meshMin.x, meshMin.y, 0f));
                Vector3 max = _graphRect.TransformPoint(new Vector3(meshMax.x, meshMax.y, 0f));
                Rect meshBounds = ScreenRectOfWorld(min, max);

                ReportBoundsOverflow("GraphMesh", meshBounds, window, now, "window");
                ReportBoundsOverflow("GraphMesh", meshBounds, container, now, "container");
            }
        }

        private static void AddBoundsTarget(string name, RectTransform rect)
        {
            if (rect == null || !rect.gameObject.activeInHierarchy) return;
            BoundsTargets.Add(new KeyValuePair<string, RectTransform>(name, rect));
        }

        private static void ReportBoundsOverflow(string name, Rect bounds, Rect reference, float now, string scope)
        {
            if (bounds.width <= 0f && bounds.height <= 0f) return;

            float left = Mathf.Max(0f, reference.xMin - bounds.xMin);
            float right = Mathf.Max(0f, bounds.xMax - reference.xMax);
            float bottom = Mathf.Max(0f, reference.yMin - bounds.yMin);
            float top = Mathf.Max(0f, bounds.yMax - reference.yMax);

            if (left <= BoundsTolerance && right <= BoundsTolerance
                && bottom <= BoundsTolerance && top <= BoundsTolerance) return;
            if (_boundsLogCount >= BoundsLogLimit) return;

            string key = name + "@" + scope;
            float lastAt;
            if (BoundsLoggedAt.TryGetValue(key, out lastAt) && now - lastAt < BoundsLogRepeatSeconds) return;
            BoundsLoggedAt[key] = now;
            _boundsLogCount++;

            ModContext.Logger?.Log(
                $"[LogGraphWindow] overflow {name} (scope: {scope}) bounds [{bounds.xMin:F1},{bounds.yMin:F1}]-[{bounds.xMax:F1},{bounds.yMax:F1}]" +
                $" ref [{reference.xMin:F1},{reference.yMin:F1}]-[{reference.xMax:F1},{reference.yMax:F1}]" +
                $" spill L{left:F1} R{right:F1} B{bottom:F1} T{top:F1}");
        }

        private Rect ExpectedScreenRect(Rect local)
        {
            if (_windowRect == null) return local;

            Vector3 min = _windowRect.TransformPoint(new Vector3(local.xMin, local.yMin, 0f));
            Vector3 max = _windowRect.TransformPoint(new Vector3(local.xMax, local.yMax, 0f));
            return ScreenRectOfWorld(min, max);
        }

        private static void ReportLayoutMismatch(string name, Rect actual, Rect expected, float now)
        {
            if (actual.width <= 0f && actual.height <= 0f) return;
            if (Mathf.Abs(actual.xMin - expected.xMin) <= BoundsTolerance && Mathf.Abs(actual.yMin - expected.yMin) <= BoundsTolerance
                && Mathf.Abs(actual.xMax - expected.xMax) <= BoundsTolerance && Mathf.Abs(actual.yMax - expected.yMax) <= BoundsTolerance) return;
            if (_boundsLogCount >= BoundsLogLimit) return;

            string key = name + "@Layout";
            float lastAt;
            if (BoundsLoggedAt.TryGetValue(key, out lastAt) && now - lastAt < BoundsLogRepeatSeconds) return;
            BoundsLoggedAt[key] = now;
            _boundsLogCount++;

            ModContext.Logger?.Log(
                $"[LogGraphWindow] layout mismatch {name} actual [{actual.xMin:F1},{actual.yMin:F1}]-[{actual.xMax:F1},{actual.yMax:F1}]" +
                $" expected [{expected.xMin:F1},{expected.yMin:F1}]-[{expected.xMax:F1},{expected.yMax:F1}]");
        }

        private static Rect ScreenRectOf(RectTransform rect)
        {
            if (rect == null) return new Rect(0f, 0f, 0f, 0f);

            TMP_Text text = rect.GetComponent<TMP_Text>();
            if (text != null && text.gameObject.activeInHierarchy && !string.IsNullOrEmpty(text.text))
            {
                Bounds ink = text.textBounds;
                Vector3 center = text.transform.TransformPoint(ink.center);
                Vector3 extents = text.transform.TransformVector(ink.extents);
                extents = new Vector3(Mathf.Abs(extents.x), Mathf.Abs(extents.y), Mathf.Abs(extents.z));
                return ScreenRectOfWorld(center - extents, center + extents);
            }

            rect.GetWorldCorners(BoundsCorners);
            return ScreenRectOfWorld(BoundsCorners[0], BoundsCorners[2]);
        }

        private static Rect ScreenRectOfWorld(Vector3 a, Vector3 b)
        {
            Vector2 sa = WorldToScreen(a);
            Vector2 sb = WorldToScreen(b);
            return Rect.MinMaxRect(Mathf.Min(sa.x, sb.x), Mathf.Min(sa.y, sb.y), Mathf.Max(sa.x, sb.x), Mathf.Max(sa.y, sb.y));
        }

    }
}
