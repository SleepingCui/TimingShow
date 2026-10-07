using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TimingShow
{
    public class LogScatterDrawer : ScatterDrawerBase
    {
        private List<TimingScatterSample> _samples = new List<TimingScatterSample>();
        private int _dataVersion;
        private bool _isAngle;
        public TMP_FontAsset GraphFontAsset => CurrentFontAsset;
        
        public void LoadLog(TimingLogData data)
        {
            _samples = data != null && data.Samples != null ? data.Samples : new List<TimingScatterSample>();
            _isAngle = data != null && data.IsAngle;
            _dataVersion++;

            ResetManualView();
            SetVerticesDirty();
        }



        protected override List<TimingScatterSample> ScatterSamples => _samples;
        protected override int ScatterVersion => _dataVersion;

        protected override bool LevelFinished => true;
        protected override string YUnitSuffix => _isAngle ? "°" : "ms";
        public override string GraphName => "";
        
        protected override bool IsEnabled => true;
        protected override bool ShowGraph => true;

        protected override float Scale => LogSettings != null ? Mathf.Clamp(LogSettings.LogGraph_UIScale, 0.75f, 3f) : 1.25f;
        protected override bool ShowPerfInfo => false;
        
        protected override bool ExternalPointerInput => true;
        protected override bool ResetViewOnSizeChange => false;
        
        protected override bool RobustYViewport => true;
        protected override bool ShowOutOfRangeIndicators => true;
        protected override bool DrawPlotFrame => true;
        protected override bool AvgLabelStub => true;
        protected override float AvgLabelMaxWidthRatio => 0.28f;
        protected override bool HintInFooter => true;
        protected override float GridAlphaScale => 0.6f;
        
        public void ReloadAppearance()
        {
            ApplyGraphFont();
            UpdateTextLayoutAndValues();
            SetVerticesDirty();
        }

  
        // 日志窗口不允许回退到游戏字体：没配置自定义字体时只使用系统字体。
        protected override bool AllowGameFontFallback => false;

        protected override TMP_FontAsset ResolveGraphFont()
        {
            bool custom = UseCustomFont && !string.IsNullOrEmpty((FontPath ?? "").Trim());
            if (custom)
            {
                TMP_FontAsset asset = base.ResolveGraphFont();
                if (asset != null && asset.atlasPopulationMode == AtlasPopulationMode.Dynamic) return asset;
            }

            return LogGraphFont.Resolve();
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
