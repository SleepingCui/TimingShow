using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TimingShow.HUD.ScatterDrawerBase
{
    public abstract partial class ScatterDrawerBase : GraphDrawerBase, IPointerDownHandler, IPointerUpHandler, IScrollHandler
    {
        protected virtual bool LevelFinished => ModContext.IsLevelFinished;
        protected virtual bool GamePaused => false;
        protected virtual bool FullRangeMode => LevelFinished || GamePaused;
        protected virtual string YUnitSuffix => "ms";
        protected virtual bool ExternalPointerInput => false;
        protected virtual bool ResetViewOnSizeChange => true;
        protected override bool IsEnabled => ModContext.IsEnabled && ModContext.IsPlaying;
        protected override bool ShowGraph => ModContext.Settings != null && ModContext.Settings.ShowTimingScatter;
        protected override float Scale => ModContext.Settings != null ? ModContext.Settings.TimingScatter_Scale : 1f;
        protected override float Width => ModContext.Settings != null ? ModContext.Settings.TimingScatter_Width : 320f;
        protected override float Height => ModContext.Settings != null ? ModContext.Settings.TimingScatter_Height : 140f;
        protected override float PosX => ModContext.Settings != null ? ModContext.Settings.TimingScatter_X : 0.05f;
        protected override float PosY => ModContext.Settings != null ? ModContext.Settings.TimingScatter_Y : 0.20f;
        protected override Color BgColor => ModContext.Settings != null ? ModContext.Settings.TimingScatter_BgColor : new Color(0f, 0f, 0f, 0.6f);
        protected override Color GridColor => ModContext.Settings != null ? ModContext.Settings.TimingScatter_GridColor : new Color(1f, 1f, 1f, 0.35f);
        protected override Color LineColor => ModContext.Settings != null ? ModContext.Settings.TimingScatter_PointColor : DefaultPointColor;
        protected override int MaxPoints => EffectivePointLimit;
        public override string GraphName => "Timing";

        protected override int DataVersion => ScatterVersion;
        protected override bool UseCustomFont => ModContext.Settings != null && ModContext.Settings.TimingScatter_UseCustomFont;
        protected override string FontPath => ModContext.Settings != null ? ModContext.Settings.TimingScatter_FontPath : "";

        protected override bool ShowPerfInfo => ModContext.Settings != null && ModContext.Settings.TimingScatter_ShowPerfInfo;
        protected override int PerfDataCount => _visibleIndices.Count;

        protected virtual bool ScatterIgnoreOutliers => ModContext.Settings != null && ModContext.Settings.TimingScatter_IgnoreOutliers;
        protected virtual bool ScatterUseJudgeColor => ModContext.Settings == null || ModContext.Settings.TimingScatter_UseJudgeColor;
        protected virtual bool ScatterUseHitAxis => ModContext.Settings != null && ModContext.Settings.TimingScatter_UseHitAxis;
        protected virtual bool ScatterShowZeroLine => ModContext.Settings != null && ModContext.Settings.TimingScatter_ShowZeroLine;
        protected virtual bool ScatterShowAvgLine => ModContext.Settings != null && ModContext.Settings.TimingScatter_ShowAvgLine;
        protected virtual float ScatterPointSize => ModContext.Settings != null ? ModContext.Settings.TimingScatter_PointSize : DefaultPointSize;
        protected virtual Color ScatterPointColor => ModContext.Settings != null ? ModContext.Settings.TimingScatter_PointColor : DefaultPointColor;
        protected virtual Color ScatterZeroLineColor => ModContext.Settings != null ? ModContext.Settings.TimingScatter_ZeroLineColor : DefaultZeroLineColor;
        protected virtual Color ScatterAvgLineColor => ModContext.Settings != null ? ModContext.Settings.TimingScatter_AvgLineColor : DefaultAvgLineColor;
        protected virtual Color ScatterAxisTextColor => ModContext.Settings != null ? ModContext.Settings.TimingScatter_AxisTextColor : DefaultAxisTextColor;

        protected virtual bool RobustYViewport => false;

        protected virtual int RobustViewportMinSamples => 24;

        protected virtual float RobustLowerPercentile => 0.02f;
        protected virtual float RobustUpperPercentile => 0.98f;

        protected virtual bool ShowOutOfRangeIndicators => false;

        protected virtual int MaxOutOfRangeIndicators => 120;

        protected virtual bool DrawPlotFrame => false;

        protected virtual bool ShowGrid => false;

        protected virtual int GridDivisions => 4;

        protected virtual bool AvgLabelStub => false;

        protected virtual bool HintInFooter => false;

        protected virtual float AvgLabelMaxWidthRatio => 1f;

        protected override float PerfTextOffsetY(float scale, int fontSize)
        {
            return -3f * scale - 16f * (fontSize / 12f) - 1f * scale;
        }

        protected override string PerfExtraInfo
        {
            get
            {
                if (!ScatterIgnoreOutliers) return null;
                if (!LevelFinished) return null;
                return "Ignored " + _ignoredOutlierCount;
            }
        }

        protected virtual int ScatterMaxRenderPoints
        {
            get
            {
                int configured = DefaultRenderPoints;
                if (ModContext.Settings != null)
                    configured = Mathf.Clamp(ModContext.Settings.TimingScatter_MaxRenderPoints, MinRenderPoints, MaxRenderPoints);

                return configured;
            }
        }

        protected virtual int EffectivePointLimit => Mathf.Min(ScatterMaxRenderPoints, MaxSafePointCount);

        protected override int ComputeSettingsHash()
        {
            unchecked
            {
                int hash = base.ComputeSettingsHash();
                Settings settings = ModContext.Settings;
                if (settings == null) return hash;

                hash = hash * 31 + settings.TimingScatter_SampleCount;
                hash = hash * 31 + (ScatterUseHitAxis ? 1 : 0);
                hash = hash * 31 + (settings.TimingScatter_AutoScroll ? 1 : 0);
                hash = hash * 31 + (ScatterUseJudgeColor ? 1 : 0);
                hash = hash * 31 + (ScatterShowZeroLine ? 1 : 0);
                hash = hash * 31 + (ScatterShowAvgLine ? 1 : 0);
                hash = hash * 31 + (ShowGrid ? 1 : 0);
                hash = hash * 31 + GridDivisions;
                hash = hash * 31 + (ScatterIgnoreOutliers ? 1 : 0);
                hash = hash * 31 + ScatterPointSize.GetHashCode();
                hash = hash * 31 + ScatterMaxRenderPoints;
                hash = hash * 31 + ScatterPointColor.GetHashCode();
                hash = hash * 31 + ScatterZeroLineColor.GetHashCode();
                hash = hash * 31 + ScatterAvgLineColor.GetHashCode();
                hash = hash * 31 + ScatterAxisTextColor.GetHashCode();
                hash = hash * 31 + (settings.TimingScatter_ShowJudgeBands ? 1 : 0);
                hash = hash * 31 + (settings.TimingScatter_ShowXpBand ? 1 : 0);
                hash = hash * 31 + (settings.TimingScatter_BandAutoWindow ? 1 : 0);
                hash = hash * 31 + settings.TimingScatter_BandThresholdBpm.GetHashCode();
                hash = hash * 31 + settings.TimingScatter_BandPerfectColor.GetHashCode();
                hash = hash * 31 + settings.TimingScatter_BandElPerfectColor.GetHashCode();
                hash = hash * 31 + settings.TimingScatter_BandEarlyLateColor.GetHashCode();
                hash = hash * 31 + settings.TimingScatter_BandXpColor.GetHashCode();
                hash = hash * 31 + (FullRangeMode ? 1 : 0);
                return hash;
            }
        }

        protected override string FormatYLabel(float value) => $"{value:+0.0;-0.0;0.0}{YUnitSuffix}";

        private bool LiveScrollActive
        {
            get
            {
                if (!_hasData || !_useTimeAxis) return false;
                if (LevelFinished) return false;
                if (_manualViewActive) return false;

                Settings settings = ModContext.Settings;
                if (settings == null || !settings.TimingScatter_AutoScroll) return false;
                if (GamePaused) return false;

                return Patches.PlayStatePatches.GetSessionTimeMs() > 0.0;
            }
        }

        protected override bool NeedsContinuousRedraw() => LiveScrollActive || _gesturePanning;

        protected override bool UpdateFrameAnimation()
        {
            if (!_avgAvailable)
            {
                _avgSmoothValid = false;
                return false;
            }

            if (!_avgSmoothValid)
            {
                _avgSmoothValue = _avgCurrent;
                _avgSmoothValid = true;
                return false;
            }

            float target = _avgCurrent;
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f) return false;

            float k = 1f - Mathf.Exp(-dt / AvgSmoothTauSeconds);
            float next = Mathf.Lerp(_avgSmoothValue, target, k);
            if (Mathf.Abs(next - target) < 0.005f) next = target;
            if (next == _avgSmoothValue) return false;

            _avgSmoothValue = next;
            return true;
        }

        public override void CollectBoundsRectangles(List<KeyValuePair<string, RectTransform>> targets)
        {
            base.CollectBoundsRectangles(targets);
            AddBoundsTarget(targets, "XAxisStartLabel", _xStartText);
            AddBoundsTarget(targets, "XAxisEndLabel", _xEndText);
            AddBoundsTarget(targets, "AvgValueLabel", _avgValueText);
            AddBoundsTarget(targets, "DetailLabel", _detailText);
            AddBoundsTarget(targets, "GestureHintLabel", _hintText);
        }

        protected override void Update()
        {
            base.Update();
            UpdateInteraction();
            RefreshBandWindow();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ClearDetailInteraction();
            _gestureActive = false;
            _gesturePanning = false;
        }

    }
}
