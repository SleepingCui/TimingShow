using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TimingShow.Logging;
using ScatterBase = TimingShow.HUD.ScatterDrawerBase.ScatterDrawerBase;

namespace TimingShow.HUD
{
    public class LogScatterDrawer : ScatterBase
    {
        private const float GridSpacingPx = 120f;
        private List<TimingScatterSample> _samples = new List<TimingScatterSample>();
        private int _dataVersion;
        private bool _isAngle;
        private float _windowBpm;
        private int _playerCount = 1;
        private bool _hasMinOffset;
        private bool _hasMaxOffset;
        private float _minOffset;
        private float _maxOffset;
        public TMP_FontAsset GraphFontAsset => CurrentFontAsset;

        public System.Action AxisMetricsChanged;

        protected override int ScatterPlayerCount
        {
            get { return _playerCount; }
        }

        protected override bool ScatterDistinguishPlayers
        {
            get { return LogSettings == null || LogSettings.LogGraph_DistinguishPlayers; }
        }

        protected override bool AllowPlayerFilter
        {
            get { return LogSettings == null || LogSettings.LogGraph_DistinguishPlayers; }
        }

        protected override void OnAxisMetricsChanged()
        {
            System.Action handler = AxisMetricsChanged;
            if (handler != null) handler();
        }
        
        protected override void ResetPlayerFilter()
        {
            _playerFilter = _playerCount > 1 ? 0 : -1;
        }

        public bool HasOffsetRange => _hasMinOffset || _hasMaxOffset;

        public int RangeKeptCount => OffsetRangeKept;

        public int RangeHiddenCount => OffsetRangeHidden;

        public void SetOffsetRange(bool hasMin, float min, bool hasMax, float max)
        {
            if (_hasMinOffset == hasMin && _hasMaxOffset == hasMax &&
                (!hasMin || Mathf.Approximately(_minOffset, min)) &&
                (!hasMax || Mathf.Approximately(_maxOffset, max)))
            {
                return;
            }

            _hasMinOffset = hasMin;
            _hasMaxOffset = hasMax;
            _minOffset = min;
            _maxOffset = max;
            _dataVersion++;
        }

        protected override bool HasOffsetRangeFilter => HasOffsetRange;

        protected override bool IsOffsetInRange(float value)
        {
            if (_hasMinOffset && value < _minOffset) return false;
            if (_hasMaxOffset && value > _maxOffset) return false;
            return true;
        }

        public void LoadLog(TimingLogData data)
        {
            _samples = data != null && data.Samples != null ? data.Samples : new List<TimingScatterSample>();
            _isAngle = data != null && data.IsAngle;
            _windowBpm = ComputeWindowBpm(data);
            _playerCount = data != null && data.PlayerCount > 0 ? data.PlayerCount : 1;
            _dataVersion++;

            ResetPlayerFilter();
            ResetManualView();
            SetVerticesDirty();
        }



        protected override List<TimingScatterSample> ScatterSamples => _samples;
        protected override int ScatterVersion => _dataVersion;

        protected override bool LevelFinished => true;
        protected override string YUnitSuffix => _isAngle ? "°" : "ms";
        public override string GraphName => "";
        protected override string PerfName => "log-graph";
        
        protected override bool IsEnabled => true;
        protected override bool ShowGraph => true;

        protected override float Scale => LogSettings != null ? Mathf.Clamp(LogSettings.LogGraph_UIScale, 0.75f, 3f) : 1.25f;
        protected override bool ShowPerfInfo => false;
        
        protected override bool ExternalPointerInput => true;
        protected override bool ResetViewOnSizeChange => false;
        
        protected override bool RobustYViewport => true;
        protected override bool ShowOutOfRangeIndicators => true;
        protected override bool DrawPlotFrame => true;
        protected override bool AxisLabelsInside => true;
        protected override bool AvgLabelStub => true;
        protected override float AvgLabelMaxWidthRatio => 0.28f;
        protected override bool HintInFooter => true;
        protected override float GridAlphaScale => 0.6f;
        protected override bool ShowGrid => LogSettings == null || LogSettings.LogGraph_ShowGrid;
        protected override bool UsesDynamicAxisTicks => true;

        protected override void DrawGridLines(VertexHelper vh, float w, float h)
        {
            if (UsesDynamicAxisTicks)
            {
                base.DrawGridLines(vh, w, h);
                return;
            }

            if (!ShowGrid)
            {
                base.DrawGridLines(vh, w, h);
                return;
            }

            float scale = Mathf.Max(0.01f, Scale);
            float halfWidth = Mathf.Max(0.25f, 1f * scale * 0.5f);

            Color color = GridColor;
            color.a = Mathf.Clamp(color.a * Mathf.Clamp01(GridAlphaScale), 0.10f, 0.65f);

            int xDivisions = Mathf.Clamp(Mathf.RoundToInt(w / GridSpacingPx), 4, 24);
            int yDivisions = Mathf.Clamp(Mathf.RoundToInt(h / GridSpacingPx), 4, 24);

            for (int i = 1; i < xDivisions; i++)
            {
                float x = w * i / xDivisions;
                DrawSegment(vh, new Vector2(x, 0f), new Vector2(x, h), halfWidth, color);
            }

            for (int i = 1; i < yDivisions; i++)
            {
                float y = h * i / yDivisions;
                DrawSegment(vh, new Vector2(0f, y), new Vector2(w, y), halfWidth, color);
            }
        }
        
        public void ReloadAppearance()
        {
            ApplyGraphFont();
            UpdateTextLayoutAndValues();
            SetVerticesDirty();
        }

        protected override bool AllowGameFontFallback => false;

        protected override TMP_FontAsset ResolveGraphFont()
        {
            bool custom = UseCustomFont && !string.IsNullOrEmpty((FontPath ?? "").Trim());
            if (custom)
            {
                TMP_FontAsset asset = base.ResolveGraphFont();
                if (asset != null && asset.atlasPopulationMode == AtlasPopulationMode.Dynamic)
                {
                    AddBundledFallback(asset);
                    return asset;
                }
            }

            return LogGraphFont.Resolve();
        }

        private static void AddBundledFallback(TMP_FontAsset asset)
        {
            TMP_FontAsset fallback = LogGraphFont.Resolve();
            if (fallback == null || ReferenceEquals(fallback, asset)) return;

            if (asset.fallbackFontAssetTable == null)
                asset.fallbackFontAssetTable = new List<TMP_FontAsset>();
            if (!asset.fallbackFontAssetTable.Contains(fallback))
                asset.fallbackFontAssetTable.Add(fallback);
        }
        
        private static Settings LogSettings => ModContext.Settings;

        protected override Color BgColor => LogSettings != null ? LogSettings.LogGraph_BgColor : new Color(0f, 0f, 0f, 0.60f);
        protected override Color GridColor => LogSettings != null ? LogSettings.LogGraph_GridColor : new Color(1f, 1f, 1f, 0.16f);
        protected override Color LineColor => LogSettings != null ? LogSettings.LogGraph_PointColor : new Color(0.30f, 0.76f, 1f, 1f);
        protected override bool UseCustomFont => LogSettings != null && LogSettings.LogGraph_UseCustomFont;
        protected override string FontPath => LogSettings != null ? LogSettings.LogGraph_FontPath : "";

        protected override bool ScatterIgnoreOutliers => LogSettings != null && LogSettings.LogGraph_IgnoreOutliers;
        protected override bool ScatterUseJudgeColor => LogSettings == null || LogSettings.LogGraph_UseJudgeColor;
        protected override bool ScatterUseHitAxis => LogSettings != null && LogSettings.LogGraph_UseHitAxis;
        protected override bool ScatterShowZeroLine => LogSettings != null && LogSettings.LogGraph_ShowZeroLine;
        protected override bool ScatterShowAvgLine => LogSettings != null && LogSettings.LogGraph_ShowAvgLine;
        protected override float ScatterPointSize => LogSettings != null ? LogSettings.LogGraph_PointSize : 3f;
        protected override Color ScatterPointColor => LogSettings != null ? LogSettings.LogGraph_PointColor : new Color(0.30f, 0.76f, 1f, 1f);
        protected override Color ScatterZeroLineColor => LogSettings != null ? LogSettings.LogGraph_ZeroLineColor : new Color(1f, 1f, 1f, 0.65f);
        protected override Color ScatterAvgLineColor => LogSettings != null ? LogSettings.LogGraph_AvgLineColor : new Color(1f, 0.85f, 0.20f, 0.95f);
        protected override Color ScatterAxisTextColor => LogSettings != null ? LogSettings.LogGraph_AxisTextColor : new Color(0.80f, 0.80f, 0.80f, 1f);

        private static float ComputeWindowBpm(TimingLogData data)
        {
            if (data == null || data.Bpm <= 0.0) return 0f;

            double pitch = data.Pitch > 0.0 ? data.Pitch : 1.0;
            double speed = data.Speed > 0.0 ? data.Speed : 1.0;
            double windowBpm = data.Bpm * pitch * speed;
            return windowBpm > 0.0 && !double.IsNaN(windowBpm) ? (float)windowBpm : 0f;
        }

        protected override void DrawBands(VertexHelper vh, float w, float h, Settings settings, float minY, float maxY)
        {
            if (settings == null || _isAngle) return;

            float rangeY = Mathf.Max(0.01f, maxY - minY);

            if (settings.LogGraph_ShowJudgeBands)
            {
                float perfectMs, elPerfectMs, passMs;
                TimingScatterDrawer.GetLogWindowMs(_windowBpm, settings.LogGraph_BandThresholdBpm,
                    out perfectMs, out elPerfectMs, out passMs);

                if (passMs > 0f)
                {
                    DrawBand(vh, w, h, rangeY, minY, -passMs, -elPerfectMs, settings.LogGraph_BandEarlyLateColor);
                    DrawBand(vh, w, h, rangeY, minY, elPerfectMs, passMs, settings.LogGraph_BandEarlyLateColor);
                    DrawBand(vh, w, h, rangeY, minY, -elPerfectMs, -perfectMs, settings.LogGraph_BandElPerfectColor);
                    DrawBand(vh, w, h, rangeY, minY, perfectMs, elPerfectMs, settings.LogGraph_BandElPerfectColor);
                    DrawBand(vh, w, h, rangeY, minY, -perfectMs, perfectMs, settings.LogGraph_BandPerfectColor);
                }
            }

            if (settings.LogGraph_ShowXpBand)
            {
                float xpMs = TimingScatterDrawer.GetLogXpWindowMs(_windowBpm);
                if (xpMs > 0f) DrawBand(vh, w, h, rangeY, minY, -xpMs, xpMs, settings.LogGraph_BandXpColor);
            }
        }

        protected override int ComputeSettingsHash()
        {
            unchecked
            {
                int hash = base.ComputeSettingsHash();
                Settings s = LogSettings;
                if (s == null) return hash;

                hash = hash * 31 + (s.LogGraph_ShowJudgeBands ? 1 : 0);
                hash = hash * 31 + (s.LogGraph_ShowXpBand ? 1 : 0);
                hash = hash * 31 + s.LogGraph_BandThresholdBpm.GetHashCode();
                hash = hash * 31 + s.LogGraph_BandPerfectColor.GetHashCode();
                hash = hash * 31 + s.LogGraph_BandElPerfectColor.GetHashCode();
                hash = hash * 31 + s.LogGraph_BandEarlyLateColor.GetHashCode();
                hash = hash * 31 + s.LogGraph_BandXpColor.GetHashCode();
                return hash;
            }
        }

        protected override int ScatterMaxRenderPoints
        {
            get
            {
                int configured = 800;
                if (LogSettings != null)
                    configured = Mathf.Clamp(LogSettings.LogGraph_MaxRenderPoints, MinRenderPoints, MaxRenderPoints);

                return configured;
            }
        }

        protected override void UpdateTransform()
        {
            RectTransform rect = rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
