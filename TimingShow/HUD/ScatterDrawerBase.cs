using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace TimingShow
{

    public abstract class ScatterDrawerBase : GraphDrawerBase, IPointerDownHandler, IPointerUpHandler, IScrollHandler
    {

        protected virtual List<TimingScatterSample> ScatterSamples => ModContext.TimingScatterSamples;
        protected virtual int ScatterVersion => ModContext.TimingScatterVersion;
        protected virtual bool LevelFinished => ModContext.IsLevelFinished;
        protected virtual bool GamePaused => false;
        protected virtual bool FullRangeMode => LevelFinished || GamePaused;
        protected virtual string YUnitSuffix => "ms";
        protected virtual bool ExternalPointerInput => false;
        protected virtual bool ResetViewOnSizeChange => true;
        public bool InteractionSuspended { get; set; }
        public bool IsGestureActive => _gestureActive;
        public bool IsGesturePanning => _gesturePanning;

        public void BeginPan(Vector2 screenPosition)
        {
            if (!DetailInteractive) return;
            if (!IsPointerInsideRect(screenPosition)) return;

            BeginGesture(screenPosition);
        }

        public void MovePan(Vector2 screenPosition)
        {
            if (!_gestureActive) return;
            if (!DetailInteractive)
            {
                EndPan();
                return;
            }

            if (!_gesturePanning)
            {
                Vector2 delta = screenPosition - _pointerDownScreen;
                if (delta.sqrMagnitude < GestureDragThreshold * GestureDragThreshold) return;

                _gesturePanning = true;
                _hintHideTime = 0f;
                _hoverSourceIndex = -1;
                HideDetailPanel();
                ApplyPan(_pointerDownScreen, screenPosition);
                _lastPointerScreen = screenPosition;
                return;
            }

            ApplyPan(_lastPointerScreen, screenPosition);
            _lastPointerScreen = screenPosition;
        }

        public void EndPan()
        {
            EndGesture();
        }

        public bool ZoomAt(float scrollDelta, Vector2 screenPosition)
        {
            if (Mathf.Approximately(scrollDelta, 0f)) return false;
            if (!DetailInteractive) return false;
            if (!IsPointerInsideRect(screenPosition)) return false;

            ApplyZoom(scrollDelta, screenPosition);
            return true;
        }

        protected virtual void DrawBands(VertexHelper vh, float w, float h, Settings settings, float minY, float maxY)
        {
        }

        protected virtual void RefreshBandWindow()
        {
        }

        protected const int MinRenderPoints = 20;
        protected const int MaxRenderPoints = 8000;
        private const int DefaultRenderPoints = 800;
        private const int FixedVertexBudget = 128;
        private const int MaxMeshVertexCount = 65535;
        private const int MinCircleSegments = 6;
        private const int MaxCircleSegments = 16;
        protected static readonly int MaxSafePointCount = (MaxMeshVertexCount - FixedVertexBudget) / (MinCircleSegments + 4);

        private const float MinYRangeMs = 2f;
        private const float MinYPadMs = 1.5f;
        private const float YPadRatio = 0.08f;
        private const float MinTimeAxisRangeMs = 1f;
        private const float GestureDragThreshold = 5f;
        private const float WheelZoomSpeed = 0.08f;
        private const float ViewMarginRatio = 0.05f;
        private const float MinViewSpanRatio = 0.001f;
        private const float MinYViewSpanMs = 0.05f;
        private const float MaxYViewSpanRatio = 5f;
        private const float YPanMarginRatio = 2f;
        private const float HintShowSeconds = 8f;
        private const float ViewSmoothTauSeconds = 0.30f;
        private const float AvgSmoothTauSeconds = 0.30f;

        private const float DetailOffset = 14f;
        private const float DetailPanelPadding = 5f;
        private const float DetailAccentWidth = 2f;
        private const float DetailMinHitRadius = 6f;
        private const int DetailLineCount = 4;
        private static readonly Color DetailPanelBgColor = new Color(0f, 0f, 0f, 0.78f);

        private static readonly Color DefaultPointColor = new Color(0.30f, 0.76f, 1f, 1f);
        private const float DefaultPointSize = 3f;
        private static readonly Color DefaultZeroLineColor = new Color(1f, 1f, 1f, 0.65f);
        private static readonly Color DefaultAvgLineColor = new Color(1f, 0.85f, 0.20f, 0.95f);
        private static readonly Color DefaultAxisTextColor = new Color(0.80f, 0.80f, 0.80f, 1f);

        private struct RenderPoint
        {
            public int SourceIndex;
            public float TimeMs;
            public float OffsetMs;
            public HitMan Judge;
            public bool IsXPerfect;
        }

        private readonly List<int> _visibleIndices = new List<int>(1024);
        private readonly List<RenderPoint> _renderPoints = new List<RenderPoint>(1024);
        private readonly List<float> _outlierScratch = new List<float>(1024);

        private RenderPoint[] _bucketMinPoints = new RenderPoint[64];
        private RenderPoint[] _bucketMaxPoints = new RenderPoint[64];
        private bool[] _bucketFilled = new bool[64];

        private TMP_Text _xStartText;
        private TMP_Text _xEndText;
        private TMP_Text _avgValueText;
        private TMP_Text _detailText;
        private TMP_Text _hintText;

        private bool _detailVisible;
        private int _hoverSourceIndex = -1;
        private int _detailShownIndex = -1;
        private int _detailDataVersion = int.MinValue;
        private Vector2 _detailPanelPosition;
        private float _detailPanelWidth;
        private float _detailPanelHeight;
        private Color _detailAccentColor = Color.white;
        private readonly List<float> _avgValues = new List<float>(1024);
        private bool _avgAvailable;
        private float _avgCurrent;
        private float _avgSmoothValue;
        private bool _avgSmoothValid;
        private bool _avgLabelVisible;

        private bool _hasData;
        private bool _useTimeAxis;
        private float _minY;
        private float _maxY;
        private int _firstSourceIndex;
        private int _lastSourceIndex;
        private float _minTimeMs;
        private float _maxTimeMs;
        private float _liveLeftTimeMs;
        private float _liveRightTimeMs;
        private int _ignoredOutlierCount;

        private float _autoXMin;
        private float _autoXMax;
        private float _autoYMin;
        private float _autoYMax;
        private float _dataXMin;
        private float _dataXMax;
        private float _viewXMin;
        private float _viewXMax;
        private bool _manualViewActive;
        private int _viewAxisMode = -1;
        private bool _gestureActive;
        private bool _gesturePanning;
        private Vector2 _pointerDownScreen;
        private Vector2 _lastPointerScreen;
        private int _scrollEventFrame = -1;
        private float _lastViewRectWidth = -1f;
        private float _lastViewRectHeight = -1f;
        private int _lastViewScreenWidth = -1;
        private int _lastViewScreenHeight = -1;
        private bool _interactiveLastFrame;
        private float _hintHideTime;

        private float _smoothMinY;
        private float _smoothMaxY;
        private bool _smoothViewValid;
        private float _smoothSpanMs;
        private bool _smoothSpanValid;

        private float _dataYMin;
        private float _dataYMax;
        private readonly List<float> _robustScratch = new List<float>(256);

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

        protected override void CreateTextComponents()
        {
            base.CreateTextComponents();

            _xStartText = CreateText("XStartLabel", CurrentFontAsset, TextAnchor.UpperLeft);
            _xEndText = CreateText("XEndLabel", CurrentFontAsset, TextAnchor.UpperRight);
            _avgValueText = CreateText("AvgValueLabel", CurrentFontAsset, TextAnchor.MiddleLeft);
            _detailText = CreateText("DetailLabel", CurrentFontAsset, TextAnchor.LowerLeft);
            _detailText.richText = true;
            _detailText.overflowMode = TextOverflowModes.Overflow;
            _detailText.gameObject.SetActive(false);
            _hintText = CreateText("GestureHintLabel", CurrentFontAsset, TextAnchor.LowerCenter);
            _hintText.alignment = TextAlignmentOptions.Bottom;
            _hintText.gameObject.SetActive(false);

            TMP_Text[] children = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < children.Length; i++) children[i].raycastTarget = false;
        }

        protected override void ApplyFontToTexts(TMP_FontAsset font)
        {
            base.ApplyFontToTexts(font);
            ApplyFontToText(_xStartText, font);
            ApplyFontToText(_xEndText, font);
            ApplyFontToText(_avgValueText, font);
            ApplyFontToText(_detailText, font);
            ApplyFontToText(_hintText, font);
        }

        protected override void ToggleTexts(bool active)
        {
            base.ToggleTexts(active);
            if (_xStartText != null && _xStartText.gameObject.activeSelf != active) _xStartText.gameObject.SetActive(active);
            if (_xEndText != null && _xEndText.gameObject.activeSelf != active) _xEndText.gameObject.SetActive(active);

            if (_avgValueText != null)
            {
                bool wantAvg = active && _avgLabelVisible;
                if (_avgValueText.gameObject.activeSelf != wantAvg) _avgValueText.gameObject.SetActive(wantAvg);
            }

            if (_detailText != null)
            {
                bool wantDetail = active && _detailVisible;
                if (_detailText.gameObject.activeSelf != wantDetail) _detailText.gameObject.SetActive(wantDetail);
            }
        }

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

        protected override void UpdateData()
        {
            ResetViewOnLayoutChange();

            _visibleIndices.Clear();
            _renderPoints.Clear();
            _hasData = false;
            _useTimeAxis = false;
            if (!_manualViewActive)
            {
                _minY = 0f;
                _maxY = 0f;
            }
            _firstSourceIndex = 0;
            _lastSourceIndex = 0;
            _minTimeMs = 0f;
            _maxTimeMs = 0f;
            _liveLeftTimeMs = 0f;
            _liveRightTimeMs = 0f;
            _avgValues.Clear();
            _avgAvailable = false;
            _avgCurrent = 0f;
            _ignoredOutlierCount = 0;

            List<TimingScatterSample> samples = ScatterSamples;
            int total = samples.Count;
            if (total == 0)
            {
                _smoothViewValid = false;
                _smoothSpanValid = false;
                _avgSmoothValid = false;
                ResetManualView();
                _autoXMin = 0f;
                _autoXMax = 0f;
                _dataXMin = 0f;
                _dataXMax = 0f;
                _autoYMin = 0f;
                _autoYMax = 0f;
                return;
            }

            Settings settings = ModContext.Settings;
            bool finished = LevelFinished;

            int take = int.MaxValue;
            if (!FullRangeMode)
            {
                int window = settings != null ? Mathf.Clamp(settings.TimingScatter_SampleCount, 1, 100000) : 200;
                take = window;
            }

            int collected = 0;
            for (int i = total - 1; i >= 0 && collected < take; i--)
            {
                if (!samples[i].HasJudge) continue;
                _visibleIndices.Add(i);
                collected++;
            }
            if (collected == 0)
            {
                _smoothViewValid = false;
                _smoothSpanValid = false;
                _avgSmoothValid = false;
                ResetManualView();
                _autoXMin = 0f;
                _autoXMax = 0f;
                _dataXMin = 0f;
                _dataXMax = 0f;
                _autoYMin = 0f;
                _autoYMax = 0f;
                return;
            }
            _visibleIndices.Reverse();
            if (finished && ScatterIgnoreOutliers)
                collected = ApplyOutlierFilter(samples, collected);

            float minOffset = float.MaxValue;
            float maxOffset = float.MinValue;
            float minTime = float.MaxValue;
            float maxTime = float.MinValue;
            bool timeValid = true;

            for (int i = 0; i < collected; i++)
            {
                TimingScatterSample s = samples[_visibleIndices[i]];
                if (s.OffsetMs < minOffset) minOffset = s.OffsetMs;
                if (s.OffsetMs > maxOffset) maxOffset = s.OffsetMs;

                if (s.TimeMs < 0f) timeValid = false;
                else
                {
                    if (s.TimeMs < minTime) minTime = s.TimeMs;
                    if (s.TimeMs > maxTime) maxTime = s.TimeMs;
                }
            }

            _dataYMin = minOffset;
            _dataYMax = maxOffset;

            _hasData = true;
            _firstSourceIndex = _visibleIndices[0];
            _lastSourceIndex = _visibleIndices[collected - 1];

            if (timeValid)
            {
                _minTimeMs = minTime;
                _maxTimeMs = maxTime;
            }
            bool wantHitAxis = ScatterUseHitAxis;
            _useTimeAxis = !wantHitAxis && timeValid && (maxTime - minTime) >= MinTimeAxisRangeMs;

            UpdateLiveTimeWindow();
            if (_useTimeAxis)
            {
                _dataXMin = minTime;
                _dataXMax = maxTime;
                _autoXMin = Mathf.Min(_liveLeftTimeMs, _liveRightTimeMs);
                _autoXMax = Mathf.Max(_liveLeftTimeMs, _liveRightTimeMs);
            }
            else
            {
                _dataXMin = _firstSourceIndex;
                _dataXMax = _lastSourceIndex;
                _autoXMin = _firstSourceIndex;
                _autoXMax = _lastSourceIndex;
            }

            int axisMode = _useTimeAxis ? 1 : 0;
            if (axisMode != _viewAxisMode)
            {
                _viewAxisMode = axisMode;
                ResetManualView();
            }

            float viewLow = minOffset;
            float viewHigh = maxOffset;
            if (RobustYViewport && collected >= RobustViewportMinSamples)
                ComputeRobustYBounds(samples, collected, ref viewLow, ref viewHigh);

            float range = viewHigh - viewLow;
            float targetMinY;
            float targetMaxY;
            if (range < MinYRangeMs)
            {
                float mid = (viewLow + viewHigh) * 0.5f;
                targetMinY = mid - MinYRangeMs * 0.5f;
                targetMaxY = mid + MinYRangeMs * 0.5f;
            }
            else
            {
                float pad = Mathf.Max(MinYPadMs, range * YPadRatio);
                targetMinY = viewLow - pad;
                targetMaxY = viewHigh + pad;
            }

            ApplyViewScaling(targetMinY, targetMaxY, minOffset, maxOffset);

            if (!_manualViewActive)
            {
                _viewXMin = _autoXMin;
                _viewXMax = _autoXMax;
            }

            BuildAverageSeries(samples, collected);
            BuildRenderPoints(samples, collected);
        }

        private int ApplyOutlierFilter(List<TimingScatterSample> samples, int count)
        {
            _ignoredOutlierCount = 0;
            if (count < 4) return count;

            _outlierScratch.Clear();
            for (int i = 0; i < count; i++)
            {
                float value = samples[_visibleIndices[i]].OffsetMs;
                if (!float.IsNaN(value)) _outlierScratch.Add(value);
            }
            if (_outlierScratch.Count < 4) return count;

            _outlierScratch.Sort();
            float q1 = _outlierScratch[Mathf.FloorToInt(_outlierScratch.Count * 0.25f)];
            float q3 = _outlierScratch[Mathf.FloorToInt(_outlierScratch.Count * 0.75f)];
            float iqr = q3 - q1;
            if (float.IsNaN(iqr) || float.IsInfinity(iqr)) return count;

            float lowerBound = q1 - 1.5f * iqr;
            float upperBound = q3 + 1.5f * iqr;

            int kept = 0;
            for (int i = 0; i < count; i++)
            {
                float value = samples[_visibleIndices[i]].OffsetMs;
                if (value >= lowerBound && value <= upperBound)
                {
                    _visibleIndices[kept++] = _visibleIndices[i];
                }
            }
            if (kept <= 0 || kept == count) return count;

            _ignoredOutlierCount = count - kept;
            _visibleIndices.RemoveRange(kept, _visibleIndices.Count - kept);
            return kept;
        }

        private void ComputeRobustYBounds(List<TimingScatterSample> samples, int collected, ref float low, ref float high)
        {
            _robustScratch.Clear();
            for (int i = 0; i < collected; i++)
            {
                float v = samples[_visibleIndices[i]].OffsetMs;
                if (float.IsNaN(v) || float.IsInfinity(v)) continue;
                _robustScratch.Add(v);
            }

            int n = _robustScratch.Count;
            if (n < RobustViewportMinSamples) return;

            _robustScratch.Sort();
            int loIndex = Mathf.Clamp(Mathf.FloorToInt((n - 1) * Mathf.Clamp01(RobustLowerPercentile)), 0, n - 1);
            int hiIndex = Mathf.Clamp(Mathf.CeilToInt((n - 1) * Mathf.Clamp01(RobustUpperPercentile)), 0, n - 1);
            if (hiIndex < loIndex) hiIndex = loIndex;

            low = _robustScratch[loIndex];
            high = _robustScratch[hiIndex];
        }

        private void UpdateLiveTimeWindow()
        {
            _liveLeftTimeMs = _minTimeMs;
            _liveRightTimeMs = _maxTimeMs;

            if (!LiveScrollActive)
            {
                _smoothSpanValid = false;
                return;
            }

            float now = (float)Patches.PlayStatePatches.GetSessionTimeMs();
            if (now <= _maxTimeMs) return;

            float spanTarget = Mathf.Max(MinTimeAxisRangeMs, _maxTimeMs - _minTimeMs);
            if (!_smoothSpanValid)
            {
                _smoothSpanMs = spanTarget;
                _smoothSpanValid = true;
            }
            else
            {
                float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime / ViewSmoothTauSeconds);
                _smoothSpanMs = Mathf.Lerp(_smoothSpanMs, spanTarget, k);
            }

            _liveRightTimeMs = now;
            _liveLeftTimeMs = now - _smoothSpanMs;
        }

        private void ApplyViewScaling(float targetMinY, float targetMaxY, float dataMinY, float dataMaxY)
        {
            if (!LiveScrollActive)
            {
                _smoothViewValid = false;
                _autoYMin = targetMinY;
                _autoYMax = targetMaxY;
            }
            else
            {
                if (!_smoothViewValid)
                {
                    _smoothMinY = targetMinY;
                    _smoothMaxY = targetMaxY;
                    _smoothViewValid = true;
                }
                else
                {
                    float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime / ViewSmoothTauSeconds);
                    _smoothMinY = Mathf.Lerp(_smoothMinY, targetMinY, k);
                    _smoothMaxY = Mathf.Lerp(_smoothMaxY, targetMaxY, k);
                }

                _smoothMinY = Mathf.Min(_smoothMinY, dataMinY);
                _smoothMaxY = Mathf.Max(_smoothMaxY, dataMaxY);

                _autoYMin = _smoothMinY;
                _autoYMax = _smoothMaxY;
            }

            if (!_manualViewActive)
            {
                _minY = _autoYMin;
                _maxY = _autoYMax;
                return;
            }
            if (_maxY < dataMinY || _minY > dataMaxY)
            {
                _minY = Mathf.Min(_minY, dataMinY);
                _maxY = Mathf.Max(_maxY, dataMaxY);
            }
        }

        private void BuildAverageSeries(List<TimingScatterSample> samples, int count)
        {
            Settings settings = ModContext.Settings;
            if (!ScatterShowAvgLine) return;
            bool dropOutliers = LevelFinished && ScatterIgnoreOutliers;

            int visible = 0;
            double sum = 0.0;
            int judged = 0;

            for (int i = 0; i < samples.Count && visible < count; i++)
            {
                TimingScatterSample s = samples[i];
                if (!s.HasJudge) continue;

                if (dropOutliers && !(visible < count && _visibleIndices[visible] == i))
                    continue;

                sum += s.OffsetMs;
                judged++;

                if (_visibleIndices[visible] == i)
                {
                    _avgValues.Add((float)(sum / judged));
                    visible++;
                }
            }

            if (_avgValues.Count > 0)
            {
                _avgAvailable = true;
                _avgCurrent = _avgValues[_avgValues.Count - 1];
            }
        }

        private void BuildRenderPoints(List<TimingScatterSample> samples, int count)
        {
            _renderPoints.Clear();

            int limit = EffectivePointLimit;
            float viewSpan = _viewXMax - _viewXMin;
            bool viewValid = viewSpan > 0f;

            if (count <= limit)
            {
                for (int i = 0; i < count; i++)
                {
                    int sourceIndex = _visibleIndices[i];
                    if (!IsInsideViewX(samples[sourceIndex], sourceIndex, viewValid, viewSpan)) continue;
                    _renderPoints.Add(ToRenderPoint(samples, sourceIndex));
                }
                return;
            }

            int bucketCount = Mathf.Max(1, Mathf.CeilToInt(rectTransform.rect.width));
            int maxPairs = Mathf.Max(1, limit / 2);
            if (bucketCount > maxPairs) bucketCount = maxPairs;

            EnsureBucketCapacity(bucketCount);
            for (int i = 0; i < bucketCount; i++) _bucketFilled[i] = false;

            for (int i = 0; i < count; i++)
            {
                int sourceIndex = _visibleIndices[i];
                TimingScatterSample s = samples[sourceIndex];

                if (!IsInsideViewX(s, sourceIndex, viewValid, viewSpan)) continue;

                float nx = (DataX(s, sourceIndex) - _viewXMin) / viewSpan;

                int bucket = (int)(nx * bucketCount);
                if (bucket < 0) bucket = 0;
                if (bucket >= bucketCount) bucket = bucketCount - 1;

                RenderPoint point = ToRenderPoint(samples, sourceIndex);

                if (!_bucketFilled[bucket])
                {
                    _bucketFilled[bucket] = true;
                    _bucketMinPoints[bucket] = point;
                    _bucketMaxPoints[bucket] = point;
                    continue;
                }

                if (point.OffsetMs < _bucketMinPoints[bucket].OffsetMs) _bucketMinPoints[bucket] = point;
                if (point.OffsetMs > _bucketMaxPoints[bucket].OffsetMs) _bucketMaxPoints[bucket] = point;
            }

            for (int b = 0; b < bucketCount; b++)
            {
                if (!_bucketFilled[b]) continue;

                RenderPoint lo = _bucketMinPoints[b];
                RenderPoint hi = _bucketMaxPoints[b];

                if (lo.SourceIndex == hi.SourceIndex)
                {
                    _renderPoints.Add(lo);
                }
                else if (lo.SourceIndex < hi.SourceIndex)
                {
                    _renderPoints.Add(lo);
                    _renderPoints.Add(hi);
                }
                else
                {
                    _renderPoints.Add(hi);
                    _renderPoints.Add(lo);
                }
            }
        }

        private static RenderPoint ToRenderPoint(List<TimingScatterSample> samples, int sourceIndex)
        {
            TimingScatterSample s = samples[sourceIndex];
            return new RenderPoint
            {
                SourceIndex = sourceIndex,
                TimeMs = s.TimeMs,
                OffsetMs = s.OffsetMs,
                Judge = s.Judge,
                IsXPerfect = s.IsXPerfect
            };
        }

        private void EnsureBucketCapacity(int count)
        {
            if (_bucketFilled.Length >= count) return;

            int size = _bucketFilled.Length;
            while (size < count) size *= 2;

            _bucketMinPoints = new RenderPoint[size];
            _bucketMaxPoints = new RenderPoint[size];
            _bucketFilled = new bool[size];
        }

        protected override int GetDataCount() => _renderPoints.Count;

        protected override float GetDataValue(int index) => _renderPoints[index].OffsetMs;

        protected override float GetMinY() => _minY;

        protected override float GetMaxY() => _maxY;

        private float DataX(TimingScatterSample sample, int sourceIndex)
        {
            return _useTimeAxis ? sample.TimeMs : sourceIndex;
        }

        private bool IsInsideViewX(TimingScatterSample sample, int sourceIndex, bool viewValid, float viewSpan)
        {
            if (!viewValid) return true;
            float x = DataX(sample, sourceIndex);
            return x >= _viewXMin && x <= _viewXMax;
        }

        private float NormalizedX(RenderPoint point)
        {
            float span = _viewXMax - _viewXMin;
            if (!(span > 0f)) return 0.5f;

            float x = _useTimeAxis ? point.TimeMs : point.SourceIndex;
            return (x - _viewXMin) / span;
        }

        protected override void DrawReferenceLines(VertexHelper vh, float w, float h)
        {
            Settings settings = ModContext.Settings;
            if (settings == null) return;

            DrawBands(vh, w, h, settings, _minY, _maxY);

            if (!ScatterShowZeroLine) return;
            if (_minY > 0f || _maxY < 0f) return;

            float rangeY = Mathf.Max(0.01f, _maxY - _minY);
            float y = Mathf.Clamp01((0f - _minY) / rangeY) * h;
            float halfWidth = Mathf.Max(0.5f, 1.5f * Mathf.Max(0.01f, Scale) * 0.5f);
            DrawSegment(vh, new Vector2(0f, y), new Vector2(w, y), halfWidth, ScatterZeroLineColor);
        }

        protected override void DrawSeries(VertexHelper vh, float w, float h)
        {
            int count = _renderPoints.Count;

            Settings settings = ModContext.Settings;
            bool judgeColor = ScatterUseJudgeColor;
            Color uniformColor = ScatterPointColor;
            float scale = Mathf.Max(0.01f, Scale);
            float radius = Mathf.Max(0.5f, (ScatterPointSize) * scale * 0.5f);
            float rangeY = Mathf.Max(0.01f, _maxY - _minY);

            if (DrawPlotFrame) DrawPlotFrameLines(vh, w, h);

            DrawAverageLine(vh, w, h, settings, scale, rangeY);

            int circleSegments = CircleSegmentsFor(count);

            for (int i = 0; i < count; i++)
            {
                RenderPoint point = _renderPoints[i];
                if (!TryGetPointLocalPosition(point, w, h, out Vector2 center)) continue;

                Color color = judgeColor ? JColors.GetColor(point.Judge, point.IsXPerfect, true) : uniformColor;
                DrawCircle(vh, center, radius, circleSegments, color);
            }

            if (ShowOutOfRangeIndicators) DrawOutOfRangeMarkers(vh, w, h);

            DrawDetailPanel(vh, scale);
        }

        private void DrawPlotFrameLines(VertexHelper vh, float w, float h)
        {
            Color color = ScatterAxisTextColor;
            float thickness = Mathf.Max(0.5f, 1f * Mathf.Max(0.01f, Scale) * 0.5f);

            Color bottom = color;
            bottom.a *= 0.30f;
            DrawSegment(vh, new Vector2(0f, 0f), new Vector2(w, 0f), thickness, bottom);

            Color left = color;
            left.a *= 0.18f;
            DrawSegment(vh, new Vector2(0f, 0f), new Vector2(0f, h), thickness, left);
        }

        private void DrawOutOfRangeMarkers(VertexHelper vh, float w, float h)
        {
            int count = _renderPoints.Count;
            if (count <= 0) return;

            float scale = Mathf.Max(0.01f, Scale);
            float rangeY = Mathf.Max(0.01f, _maxY - _minY);
            bool judgeColor = ScatterUseJudgeColor;
            Color uniformColor = ScatterPointColor;
            float halfWidth = Mathf.Max(1.5f, 3.2f * scale);
            float height = Mathf.Max(2.5f, 5f * scale);
            float inset = Mathf.Max(1f, 1.5f * scale);
            int drawn = 0;

            for (int i = 0; i < count; i++)
            {
                RenderPoint point = _renderPoints[i];
                float ny = (point.OffsetMs - _minY) / rangeY;
                if (ny >= 0f && ny <= 1f) continue;

                float nx = Mathf.Clamp01(NormalizedX(point));
                float x = Mathf.Clamp(nx * w, halfWidth, Mathf.Max(halfWidth, w - halfWidth));
                bool above = ny > 1f;

                Color color = judgeColor ? JColors.GetColor(point.Judge, point.IsXPerfect, true) : uniformColor;
                color.a *= 0.85f;

                float baseY = above ? h - inset - height : inset + height;
                float tipY = above ? h - inset : inset;
                DrawTriangle(vh, new Vector2(x, baseY), new Vector2(x, tipY), halfWidth, color);

                drawn++;
                if (drawn >= MaxOutOfRangeIndicators) break;
            }
        }

        private void DrawTriangle(VertexHelper vh, Vector2 baseCenter, Vector2 tip, float halfWidth, Color color)
        {
            int baseIndex = vh.currentVertCount;
            vh.AddVert(new Vector3(baseCenter.x - halfWidth, baseCenter.y, 0f), color, Vector2.zero);
            vh.AddVert(new Vector3(baseCenter.x + halfWidth, baseCenter.y, 0f), color, Vector2.zero);
            vh.AddVert(new Vector3(tip.x, tip.y, 0f), color, Vector2.zero);
            vh.AddTriangle(baseIndex, baseIndex + 1, baseIndex + 2);
        }

        private static int CircleSegmentsFor(int pointCount)
        {
            int budgetPerPoint = (MaxMeshVertexCount - FixedVertexBudget) / Mathf.Max(1, pointCount);
            int segments = budgetPerPoint - 4;
            return Mathf.Clamp(segments, MinCircleSegments, MaxCircleSegments);
        }

        private void DrawCircle(VertexHelper vh, Vector2 center, float radius, int segments, Color color)
        {
            if (segments < 3) segments = 3;

            int baseIndex = vh.currentVertCount;
            for (int i = 0; i < segments; i++)
            {
                float angle = (Mathf.PI * 2f * i) / segments;
                vh.AddVert(new Vector3(center.x + Mathf.Cos(angle) * radius, center.y + Mathf.Sin(angle) * radius, 0f), color, Vector2.zero);
            }

            for (int i = 1; i < segments - 1; i++)
            {
                vh.AddTriangle(baseIndex, baseIndex + i, baseIndex + i + 1);
            }
        }

        private void DrawAverageLine(VertexHelper vh, float w, float h, Settings settings, float scale, float rangeY)
        {
            if (!_avgAvailable || !ScatterShowAvgLine) return;
            if (_avgValues.Count != _visibleIndices.Count || _avgValues.Count < 2) return;

            int count = _renderPoints.Count;
            float halfWidth = Mathf.Max(0.5f, 1.6f * scale * 0.5f);
            Color color = ScatterAvgLineColor;

            int cursor = 0;
            bool hasPrev = false;
            Vector2 prev = Vector2.zero;

            for (int i = 0; i < count; i++)
            {
                RenderPoint point = _renderPoints[i];
                while (cursor < _avgValues.Count - 1 && _visibleIndices[cursor] < point.SourceIndex) cursor++;
                float avg = i == count - 1 && _avgSmoothValid ? _avgSmoothValue : _avgValues[cursor];
                float nx = NormalizedX(point);
                float ny = Mathf.Clamp01((avg - _minY) / rangeY);
                Vector2 cur = new Vector2(nx * w, ny * h);

                if (hasPrev) DrawClippedSegment(vh, prev, cur, w, halfWidth, color);
                prev = cur;
                hasPrev = true;
            }

            float currentAvg = _avgSmoothValid ? _avgSmoothValue : _avgCurrent;
            float tailNy = Mathf.Clamp01((currentAvg - _minY) / rangeY);
            if (hasPrev)
            {
                Vector2 tail = new Vector2(w, tailNy * h);
                DrawClippedSegment(vh, prev, tail, w, halfWidth, color);
            }
            else
            {
                DrawSegment(vh, new Vector2(0f, tailNy * h), new Vector2(w, tailNy * h), halfWidth, color);
            }

            if (AvgLabelStub)
            {
                Color stub = color;
                stub.a *= 0.55f;
                float stubHalf = Mathf.Max(0.5f, halfWidth * 0.8f);
                DrawSegment(vh, new Vector2(w - 1f * scale, tailNy * h), new Vector2(w + 4f * scale, tailNy * h), stubHalf, stub);
            }
        }

        private void DrawClippedSegment(VertexHelper vh, Vector2 a, Vector2 b, float w, float halfWidth, Color color)
        {
            if (a.x > b.x) (a, b) = (b, a);
            if (b.x <= 0f || a.x >= w) return;

            float dx = b.x - a.x;
            if (a.x < 0f && dx > 0.0001f) a = new Vector2(0f, Mathf.Lerp(a.y, b.y, (0f - a.x) / dx));
            if (b.x > w && dx > 0.0001f) b = new Vector2(w, Mathf.Lerp(a.y, b.y, (w - a.x) / dx));

            if (b.x - a.x < 0.01f) return;

            DrawSegment(vh, a, b, halfWidth, color);
        }

        protected override void UpdateTextLayoutAndValues()
        {
            base.UpdateTextLayoutAndValues();

            if (_xStartText == null || _xEndText == null) return;

            Settings settings = ModContext.Settings;
            float scale = Mathf.Max(0.01f, Scale);
            float w = rectTransform.rect.width;
            float h = rectTransform.rect.height;
            int fontSize = Mathf.Clamp(Mathf.RoundToInt(11 * scale), 8, 32);
            float labelHeight = 16f * (fontSize / 12f);
            Color color = ScatterAxisTextColor;

            string startText = string.Empty;
            string endText = string.Empty;

            if (_hasData)
            {
                if (_useTimeAxis)
                {
                    startText = _manualViewActive ? FormatDetailTime(_viewXMin) : $"{_viewXMin / 1000f:F1}s";
                    endText = _manualViewActive ? FormatDetailTime(_viewXMax) : $"{_viewXMax / 1000f:F1}s";
                }
                else
                {
                    startText = $"#{Mathf.Max(0, Mathf.RoundToInt(_viewXMin)) + 1}";
                    endText = $"#{Mathf.Max(0, Mathf.RoundToInt(_viewXMax)) + 1}";
                }
            }

            float axisY = -3f * scale;

            SetupXAxisText(_xStartText, startText, new Vector2(2f * scale, axisY),
                new Vector2(0f, 1f), TextAlignmentOptions.TopLeft, fontSize, labelHeight, color);
            SetupXAxisText(_xEndText, endText, new Vector2(w - 2f * scale, axisY),
                new Vector2(1f, 1f), TextAlignmentOptions.TopRight, fontSize, labelHeight, color);

            bool showAvgLabel = _avgAvailable && ScatterShowAvgLine;
            UpdateAvgValueLabel(showAvgLabel, settings, scale, w, h);
        }

        private void UpdateAvgValueLabel(bool visible, Settings settings, float scale, float w, float h)
        {
            if (_avgValueText == null) return;

            _avgLabelVisible = visible;
            if (_avgValueText.gameObject.activeSelf != visible) _avgValueText.gameObject.SetActive(visible);
            if (!visible) return;

            float rangeY = Mathf.Max(0.01f, _maxY - _minY);
            float currentAvg = _avgSmoothValid ? _avgSmoothValue : _avgCurrent;
            float ny = Mathf.Clamp01((currentAvg - _minY) / rangeY);

            int fontSize = Mathf.Clamp(Mathf.RoundToInt(12 * scale), 8, 32);
            _avgValueText.fontSize = fontSize;
            _avgValueText.text = "Avg: " + FormatYLabel(currentAvg);
            _avgValueText.color = ScatterAvgLineColor;
            _avgValueText.alignment = TextAlignmentOptions.MidlineLeft;

            RectTransform rt = _avgValueText.rectTransform;
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(w + 6f * scale, ny * h);

            float labelWidth = 120f * (fontSize / 12f);
            if (AvgLabelMaxWidthRatio < 1f)
            {
                float maxWidth = Mathf.Max(24f, w * Mathf.Clamp01(AvgLabelMaxWidthRatio));
                if (labelWidth > maxWidth)
                {
                    labelWidth = maxWidth;
                    _avgValueText.overflowMode = TextOverflowModes.Ellipsis;
                }
            }

            rt.sizeDelta = new Vector2(labelWidth, 24f * (fontSize / 12f));
        }

        private void SetupXAxisText(TMP_Text t, string content, Vector2 anchoredPosition, Vector2 pivot, TextAlignmentOptions alignment, int fontSize, float height, Color color)
        {
            if (t == null) return;

            t.fontSize = fontSize;
            t.text = content;
            t.color = color;
            t.alignment = alignment;

            RectTransform rt = t.rectTransform;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = new Vector2(220f * (fontSize / 12f), height);
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

        private bool DetailInteractive
        {
            get
            {
                if (!IsEnabled || !ShowGraph) return false;
                if (InteractionSuspended) return false;
                if (LevelFinished) return true;
                return GamePaused;
            }
        }

        private bool TryGetPointLocalPosition(RenderPoint point, float w, float h, out Vector2 local)
        {
            float rangeY = _maxY - _minY;
            if (!(rangeY > 0f))
            {
                local = Vector2.zero;
                return false;
            }

            float nx = NormalizedX(point);
            float ny = (point.OffsetMs - _minY) / rangeY;
            if (nx < 0f || nx > 1f || ny < 0f || ny > 1f)
            {
                local = Vector2.zero;
                return false;
            }

            local = new Vector2(nx * w, ny * h);
            return true;
        }

        private void UpdateInteraction()
        {
            if (_detailDataVersion != ScatterVersion)
            {
                _detailDataVersion = ScatterVersion;
                ClearDetailInteraction();
                if (ScatterSamples.Count == 0) ResetManualView();
            }

            bool interactive = DetailInteractive;
            raycastTarget = interactive;

            if (interactive && !_interactiveLastFrame)
            {
                _hintHideTime = Time.unscaledTime + HintShowSeconds;
            }
            _interactiveLastFrame = interactive;

            UpdateGesture();

            if (!interactive)
            {
                ClearDetailInteraction();
                ResetManualView();
                UpdateHintText();
                return;
            }

            float w = rectTransform.rect.width;
            float h = rectTransform.rect.height;
            if (w <= 0f || h <= 0f || _renderPoints.Count == 0)
            {
                _hoverSourceIndex = -1;
                RefreshDetailPanel();
                UpdateHintText();
                UpdateScrollFallback(false);
                return;
            }

            bool insideRect = false;
            Vector2 localMouse = Vector2.zero;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, Input.mousePosition, null, out Vector2 converted))
            {
                localMouse = converted;
                insideRect = localMouse.x >= 0f && localMouse.x <= w && localMouse.y >= 0f && localMouse.y <= h;
            }

            if (_gesturePanning)
            {
                _hoverSourceIndex = -1;
                HideDetailPanel();
            }
            else
            {
                _hoverSourceIndex = insideRect ? FindNearestRenderPoint(localMouse, w, h) : -1;
                RefreshDetailPanel();
            }

            if (!ExternalPointerInput) UpdateScrollFallback(insideRect);
            UpdateHintText();
        }

        private void UpdateGesture()
        {
            if (ExternalPointerInput)
            {
                return;
            }

            if (!DetailInteractive)
            {
                _gestureActive = false;
                _gesturePanning = false;
                return;
            }

            if (!_gestureActive)
            {
                if (EventSystem.current == null && Input.GetMouseButtonDown(0)
                    && IsPointerInsideRect(Input.mousePosition))
                {
                    BeginGesture(Input.mousePosition);
                }
                return;
            }
            if (!Input.GetMouseButton(0))
            {
                EndGesture();
                return;
            }

            Vector2 current = Input.mousePosition;
            if (!_gesturePanning)
            {
                if ((current - _pointerDownScreen).sqrMagnitude < GestureDragThreshold * GestureDragThreshold)
                    return;

                _gesturePanning = true;
                _hintHideTime = 0f;
                _hoverSourceIndex = -1;
                HideDetailPanel();
                ApplyPan(_pointerDownScreen, current);
                _lastPointerScreen = current;
                return;
            }

            ApplyPan(_lastPointerScreen, current);
            _lastPointerScreen = current;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (ExternalPointerInput) return;
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
            if (!DetailInteractive) return;

            BeginGesture(eventData != null ? eventData.position : (Vector2)Input.mousePosition);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (ExternalPointerInput) return;
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
            EndGesture();
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (ExternalPointerInput) return;
            if (eventData == null || !DetailInteractive) return;

            _scrollEventFrame = Time.frameCount;
            Vector2 position = eventData.position;
            if (position == Vector2.zero) position = Input.mousePosition;
            ApplyZoom(eventData.scrollDelta.y, position);
        }

        private void BeginGesture(Vector2 screenPosition)
        {
            if (!IsPointerInsideRect(screenPosition)) return;

            _gestureActive = true;
            _gesturePanning = false;
            _pointerDownScreen = screenPosition;
            _lastPointerScreen = screenPosition;
        }

        private void EndGesture()
        {
            if (!_gestureActive && !_gesturePanning) return;

            _gestureActive = false;
            _gesturePanning = false;
            SetVerticesDirty();
        }

        private bool IsPointerInsideRect(Vector2 screenPosition)
        {
            Rect rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f) return false;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPosition, null, out Vector2 local)) return false;

            return local.x >= 0f && local.x <= rect.width && local.y >= 0f && local.y <= rect.height;
        }

        private void ApplyPan(Vector2 previousScreen, Vector2 currentScreen)
        {
            Rect rect = rectTransform.rect;
            float w = rect.width;
            float h = rect.height;
            if (w <= 0f || h <= 0f) return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, previousScreen, null, out Vector2 previousLocal)) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, currentScreen, null, out Vector2 currentLocal)) return;

            float dx = currentLocal.x - previousLocal.x;
            float dy = currentLocal.y - previousLocal.y;
            if (Mathf.Approximately(dx, 0f) && Mathf.Approximately(dy, 0f)) return;

            float spanX = _viewXMax - _viewXMin;
            float spanY = _maxY - _minY;
            if (!(spanX > 0f) && !(spanY > 0f)) return;

            _manualViewActive = true;
            _hintHideTime = 0f;

            if (spanX > 0f)
            {
                _viewXMin -= dx / w * spanX;
                _viewXMax = _viewXMin + spanX;
            }
            if (spanY > 0f)
            {
                _minY -= dy / h * spanY;
                _maxY = _minY + spanY;
            }

            ClampViewX();
            ClampViewY();

            RefreshViewGeometry();
        }

        private void ApplyZoom(float scrollDelta, Vector2 screenPosition)
        {
            if (Mathf.Approximately(scrollDelta, 0f)) return;
            if (!_hasData || !DetailInteractive) return;

            Rect rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPosition, null, out Vector2 local)) return;

            float factor = Mathf.Exp(-scrollDelta * WheelZoomSpeed);
            if (float.IsNaN(factor) || float.IsInfinity(factor) || factor <= 0f) return;

            float tx = Mathf.Clamp01(local.x / rect.width);
            float ty = Mathf.Clamp01(local.y / rect.height);

            _manualViewActive = true;
            _hintHideTime = 0f;

            float hardXMin, hardXMax, minXSpan, maxXSpan;
            float hardYMin, hardYMax, minYSpan, maxYSpan;
            GetXViewLimits(out hardXMin, out hardXMax, out minXSpan, out maxXSpan);
            GetYViewLimits(out hardYMin, out hardYMax, out minYSpan, out maxYSpan);

            ZoomAxis(ref _viewXMin, ref _viewXMax, tx, factor, hardXMin, hardXMax, minXSpan, maxXSpan);
            ZoomAxis(ref _minY, ref _maxY, ty, factor, hardYMin, hardYMax, minYSpan, maxYSpan);

            RefreshViewGeometry();
        }

        private void UpdateScrollFallback(bool insideRect)
        {
            if (!insideRect || _scrollEventFrame == Time.frameCount) return;

            float delta = Input.mouseScrollDelta.y;
            if (Mathf.Approximately(delta, 0f)) return;

            ApplyZoom(delta, Input.mousePosition);
        }

        private static void ZoomAxis(ref float min, ref float max, float anchorRatio, float factor,
            float hardMin, float hardMax, float minSpan, float maxSpan)
        {
            float span = max - min;
            if (float.IsNaN(span) || float.IsInfinity(span) || span <= 0f) return;

            float anchor = min + Mathf.Clamp01(anchorRatio) * span;
            float newSpan = Mathf.Clamp(span * factor, minSpan, Mathf.Max(minSpan, maxSpan));

            min = anchor - Mathf.Clamp01(anchorRatio) * newSpan;
            max = min + newSpan;

            ClampViewRange(ref min, ref max, hardMin, hardMax, minSpan, maxSpan);
        }

        private void GetXViewLimits(out float hardMin, out float hardMax, out float minSpan, out float maxSpan)
        {
            float dataMin = Mathf.Min(_dataXMin, _dataXMax);
            float dataMax = Mathf.Max(_dataXMin, _dataXMax);
            float dataSpan = Mathf.Max(1e-4f, dataMax - dataMin);
            float margin = dataSpan * ViewMarginRatio;

            hardMin = dataMin - margin;
            hardMax = dataMax + margin;
            maxSpan = hardMax - hardMin;
            minSpan = _useTimeAxis
                ? Mathf.Max(MinTimeAxisRangeMs, dataSpan * MinViewSpanRatio)
                : Mathf.Max(1f, dataSpan * MinViewSpanRatio);
        }
        
        private void GetYViewLimits(out float hardMin, out float hardMax, out float minSpan, out float maxSpan)
        {
            float autoSpan = Mathf.Max(1e-4f, _autoYMax - _autoYMin);

            float reachMin = _hasData ? Mathf.Min(_autoYMin, _dataYMin) : _autoYMin;
            float reachMax = _hasData ? Mathf.Max(_autoYMax, _dataYMax) : _autoYMax;
            float reachSpan = Mathf.Max(autoSpan, reachMax - reachMin);
            float margin = reachSpan * YPanMarginRatio;

            hardMin = reachMin - margin;
            hardMax = reachMax + margin;
            maxSpan = reachSpan * MaxYViewSpanRatio;
            minSpan = Mathf.Max(MinYViewSpanMs, autoSpan * MinViewSpanRatio);
        }

        private void ClampViewX()
        {
            float hardMin, hardMax, minSpan, maxSpan;
            GetXViewLimits(out hardMin, out hardMax, out minSpan, out maxSpan);
            ClampViewRange(ref _viewXMin, ref _viewXMax, hardMin, hardMax, minSpan, maxSpan);
        }

        private void ClampViewY()
        {
            float hardMin, hardMax, minSpan, maxSpan;
            GetYViewLimits(out hardMin, out hardMax, out minSpan, out maxSpan);
            ClampViewRange(ref _minY, ref _maxY, hardMin, hardMax, minSpan, maxSpan);
        }
        
        private static void ClampViewRange(ref float min, ref float max, float hardMin, float hardMax, float minSpan, float maxSpan)
        {
            if (float.IsNaN(min) || float.IsNaN(max) || float.IsInfinity(min) || float.IsInfinity(max))
            {
                min = hardMin;
                max = hardMin + Mathf.Max(minSpan, Mathf.Min(maxSpan, hardMax - hardMin));
                return;
            }

            if (min > max) (min, max) = (max, min);
            if (!(hardMax > hardMin)) return;

            float span = max - min;
            if (!(span > 0f))
            {
                float center = (min + max) * 0.5f;
                span = Mathf.Max(minSpan, Mathf.Min(maxSpan, hardMax - hardMin));
                min = center - span * 0.5f;
                max = min + span;
            }

            float clampedSpan = Mathf.Clamp(max - min, minSpan, Mathf.Max(minSpan, maxSpan));
            if (clampedSpan != max - min)
            {
                float center = (min + max) * 0.5f;
                min = center - clampedSpan * 0.5f;
                max = min + clampedSpan;
            }

            float finalSpan = max - min;
            if (finalSpan >= hardMax - hardMin)
            {
                min = hardMin;
                max = hardMax;
                return;
            }

            if (min < hardMin)
            {
                min = hardMin;
                max = min + finalSpan;
            }
            else if (max > hardMax)
            {
                max = hardMax;
                min = max - finalSpan;
            }
        }

        private void RefreshViewGeometry()
        {
            if (!_hasData)
            {
                SetVerticesDirty();
                return;
            }

            BuildRenderPoints(ScatterSamples, _visibleIndices.Count);
            UpdateTextLayoutAndValues();
            SetVerticesDirty();
        }

        protected void ResetManualView()
        {
            bool wasManual = _manualViewActive;
            _manualViewActive = false;
            _gestureActive = false;
            _gesturePanning = false;
            _viewXMin = _autoXMin;
            _viewXMax = _autoXMax;
            _minY = _autoYMin;
            _maxY = _autoYMax;
            if (wasManual) SetVerticesDirty();
        }
        private void ResetViewOnLayoutChange()
        {
            Rect rect = rectTransform.rect;
            bool rectChanged = rect.width != _lastViewRectWidth || rect.height != _lastViewRectHeight;
            bool screenChanged = Screen.width != _lastViewScreenWidth || Screen.height != _lastViewScreenHeight;
            if (!rectChanged && !screenChanged) return;

            _lastViewRectWidth = rect.width;
            _lastViewRectHeight = rect.height;
            _lastViewScreenWidth = Screen.width;
            _lastViewScreenHeight = Screen.height;

            if (!screenChanged && !ResetViewOnSizeChange) return;

            ResetManualView();
        }
        private void UpdateHintText()
        {
            if (_hintText == null) return;

            bool visible = DetailInteractive && Time.unscaledTime < _hintHideTime;
            if (_hintText.gameObject.activeSelf != visible) _hintText.gameObject.SetActive(visible);
            if (!visible) return;

            Settings settings = ModContext.Settings;
            float scale = Mathf.Max(0.01f, Scale);
            float w = rectTransform.rect.width;
            int fontSize = Mathf.Clamp(Mathf.RoundToInt(10 * scale), 8, 32);
            Color color = ScatterAxisTextColor;
            color.a *= 0.6f;

            string content = i18n.T("Hint_GraphGesture");
            if (_hintText.text != content) _hintText.text = content;
            if (_hintText.fontSize != fontSize) _hintText.fontSize = fontSize;
            if (_hintText.color != color) _hintText.color = color;

            RectTransform rt = _hintText.rectTransform;
            Vector2 position;
            Vector2 pivot;
            Vector2 size = new Vector2(280f * (fontSize / 12f), 16f * (fontSize / 12f));
            if (HintInFooter)
            {
                _hintText.alignment = TextAlignmentOptions.Top;
                position = new Vector2(w * 0.5f, -3f * scale);
                pivot = new Vector2(0.5f, 1f);
                size.x = Mathf.Min(size.x, Mathf.Max(40f, w * 0.8f));
            }
            else
            {
                _hintText.alignment = TextAlignmentOptions.Bottom;
                position = new Vector2(w * 0.5f, 3f * scale);
                pivot = new Vector2(0.5f, 0f);
            }

            if (rt.pivot != pivot) rt.pivot = pivot;
            if (rt.anchoredPosition != position) rt.anchoredPosition = position;
            if (rt.sizeDelta != size) rt.sizeDelta = size;
        }

        private int FindNearestRenderPoint(Vector2 localMouse, float w, float h)
        {
            Settings settings = ModContext.Settings;
            float scale = Mathf.Max(0.01f, Scale);
            float pointRadius = Mathf.Max(0.5f, (ScatterPointSize) * scale * 0.5f);
            float hitRadius = Mathf.Max(pointRadius, DetailMinHitRadius);
            float bestDistanceSqr = hitRadius * hitRadius;
            int best = -1;
            for (int i = 0; i < _renderPoints.Count; i++)
            {
                RenderPoint point = _renderPoints[i];
                if (!TryGetPointLocalPosition(point, w, h, out Vector2 center)) continue;

                float distanceSqr = (center - localMouse).sqrMagnitude;
                if (distanceSqr > bestDistanceSqr) continue;

                bestDistanceSqr = distanceSqr;
                best = point.SourceIndex;
            }

            return best;
        }

        private void RefreshDetailPanel()
        {
            int index = _hoverSourceIndex;
            if (index >= 0 && !IsRenderableSourceIndex(index)) index = -1;

            if (index < 0)
            {
                HideDetailPanel();
                return;
            }

            Settings settings = ModContext.Settings;
            float scale = Mathf.Max(0.01f, Scale);
            int fontSize = Mathf.Clamp(Mathf.RoundToInt(12 * scale), 8, 32);
            float lineHeight = 16f * (fontSize / 12f);
            float pad = DetailPanelPadding * (fontSize / 12f);
            float panelWidth = 132f * (fontSize / 12f) + pad * 2f;
            float panelHeight = lineHeight * DetailLineCount + pad * 2f;

            _detailText.fontSize = fontSize;
            _detailText.color = ScatterAxisTextColor;
            _detailText.alignment = TextAlignmentOptions.TopLeft;

            if (index != _detailShownIndex)
            {
                _detailShownIndex = index;
                _detailText.text = BuildDetailText(index);
            }

            Vector2 panelPosition = ComputeDetailPanelPosition(index, panelWidth, panelHeight);
            bool moved = (panelPosition - _detailPanelPosition).sqrMagnitude > 0.25f;
            if (_detailVisible && !moved && _detailPanelWidth == panelWidth && _detailPanelHeight == panelHeight) return;

            _detailPanelPosition = panelPosition;
            _detailPanelWidth = panelWidth;
            _detailPanelHeight = panelHeight;

            RectTransform rt = _detailText.rectTransform;
            rt.pivot = Vector2.zero;
            rt.anchoredPosition = new Vector2(panelPosition.x + pad, panelPosition.y + pad);
            rt.sizeDelta = new Vector2(panelWidth - pad * 2f, panelHeight - pad * 2f);

            _detailVisible = true;
            if (!_detailText.gameObject.activeSelf) _detailText.gameObject.SetActive(true);
            SetVerticesDirty();
        }

        private void HideDetailPanel()
        {
            _detailShownIndex = -1;
            if (!_detailVisible) return;

            _detailVisible = false;
            if (_detailText != null && _detailText.gameObject.activeSelf) _detailText.gameObject.SetActive(false);
            SetVerticesDirty();
        }

        private void ClearDetailInteraction()
        {
            _hoverSourceIndex = -1;
            HideDetailPanel();
        }

        private bool IsRenderableSourceIndex(int index)
        {
            List<TimingScatterSample> samples = ScatterSamples;
            if (index < 0 || index >= samples.Count || !samples[index].HasJudge) return false;

            for (int i = 0; i < _renderPoints.Count; i++)
                if (_renderPoints[i].SourceIndex == index) return true;

            return false;
        }

        private Vector2 ComputeDetailPanelPosition(int index, float panelWidth, float panelHeight)
        {
            float w = rectTransform.rect.width;
            float h = rectTransform.rect.height;
            Vector2 panelPosition = new Vector2(DetailOffset, DetailOffset);

            for (int i = 0; i < _renderPoints.Count; i++)
            {
                RenderPoint point = _renderPoints[i];
                if (point.SourceIndex != index) continue;

                if (TryGetPointLocalPosition(point, w, h, out Vector2 pointPosition))
                    panelPosition = new Vector2(pointPosition.x + DetailOffset, pointPosition.y + DetailOffset);
                break;
            }

            float canvasScale = canvas != null ? canvas.scaleFactor : 1f;
            float screenWidth = panelWidth * canvasScale;
            float screenHeight = panelHeight * canvasScale;

            Vector3 corner = RectTransformUtility.WorldToScreenPoint(null, rectTransform.TransformPoint(panelPosition));
            float x = corner.x;
            float y = corner.y;

            if (x + screenWidth > Screen.width) x -= screenWidth + DetailOffset * 2f * canvasScale;
            if (y + screenHeight > Screen.height) y -= screenHeight + DetailOffset * 2f * canvasScale;

            x = Mathf.Clamp(x, 0f, Mathf.Max(0f, Screen.width - screenWidth));
            y = Mathf.Clamp(y, 0f, Mathf.Max(0f, Screen.height - screenHeight));

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, new Vector2(x, y), null, out Vector2 local))
                panelPosition = local;

            return panelPosition;
        }

        private void DrawDetailPanel(VertexHelper vh, float scale)
        {
            if (!_detailVisible) return;

            float w = _detailPanelWidth;
            float h = _detailPanelHeight;
            if (w <= 0f || h <= 0f) return;

            float x = _detailPanelPosition.x;
            float y = _detailPanelPosition.y;

            DrawQuad(vh, new Vector2(x, y), new Vector2(x + w, y + h), DetailPanelBgColor);
            DrawQuad(vh, new Vector2(x, y), new Vector2(x + Mathf.Max(1f, DetailAccentWidth * scale), y + h), _detailAccentColor);
        }

        private string BuildDetailText(int index)
        {
            TimingScatterSample sample = ScatterSamples[index];
            _detailAccentColor = JColors.GetColor(sample.Judge, sample.IsXPerfect, true);

            string timing = string.Format("{0:+0.00;-0.00;0.00} ms", sample.OffsetMs);
            string time = sample.TimeMs < 0f ? i18n.T("Detail_TimeUnavailable") : FormatDetailTime(sample.TimeMs);

            return string.Format(
                "{0} #{1}\n{2}: {3}\n{4}: {5}\n{6}: <color=#{7}>{8}</color>",
                i18n.T("Detail_Hit"), index + 1,
                i18n.T("Detail_Timing"), timing,
                i18n.T("Detail_Time"), time,
                i18n.T("Detail_Judge"), ColorUtility.ToHtmlStringRGB(_detailAccentColor), FormatDetailJudge(sample));
        }

        private static string FormatDetailTime(float timeMs)
        {
            int totalMs = Mathf.Max(0, Mathf.RoundToInt(timeMs));
            int minutes = totalMs / 60000;
            int seconds = (totalMs / 1000) % 60;
            int millis = totalMs % 1000;
            return string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, millis);
        }

        private static string FormatDetailJudge(TimingScatterSample sample)
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
    }
}
