using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TimingShow
{
    public class TimingScatterDrawer : GraphDrawerBase
    {
        private const int MinRenderPoints = 20;
        private const int MaxRenderPoints = 8000;
        private const int DefaultRenderPoints = 800;
        private const int FixedVertexBudget = 128;
        private const int MaxMeshVertexCount = 65535;
        private const int MinCircleSegments = 6;
        private const int MaxCircleSegments = 16;
        
        private static readonly int MaxSafePointCount = (MaxMeshVertexCount - FixedVertexBudget) / (MinCircleSegments + 4);

        private const float MinYRangeMs = 2f;
        private const float MinYPadMs = 1.5f;
        private const float YPadRatio = 0.08f;
        private const float MinTimeAxisRangeMs = 1f;
        
        private const float ViewSmoothTauSeconds = 0.30f;
        private const float AvgSmoothTauSeconds = 0.30f;   

        private const float DetailOffset = 14f;
        private const float DetailPanelPadding = 5f;
        private const float DetailAccentWidth = 2f;
        private const float DetailMinHitRadius = 6f;
        private const int DetailLineCount = 4;
        private static readonly Color DetailPanelBgColor = new Color(0f, 0f, 0f, 0.78f);

        private static readonly Color DefaultPointColor = new Color(0.30f, 0.76f, 1f, 1f);

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

        private bool _detailVisible;
        private int _hoverSourceIndex = -1;
        private int _selectedSourceIndex = -1;
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

        private float _smoothMinY;
        private float _smoothMaxY;
        private bool _smoothViewValid;
        private float _smoothSpanMs;
        private bool _smoothSpanValid;

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

        protected override int DataVersion => ModContext.TimingScatterVersion;
        protected override bool UseCustomFont => ModContext.Settings != null && ModContext.Settings.TimingScatter_UseCustomFont;
        protected override string FontPath => ModContext.Settings != null ? ModContext.Settings.TimingScatter_FontPath : "";

        protected override bool ShowPerfInfo => ModContext.Settings != null && ModContext.Settings.TimingScatter_ShowPerfInfo;
        protected override int PerfDataCount => _visibleIndices.Count;

        protected override float PerfTextOffsetY(float scale, int fontSize)
        {
            return -3f * scale - 16f * (fontSize / 12f) - 1f * scale;
        }

        protected override string PerfExtraInfo
        {
            get
            {
                Settings settings = ModContext.Settings;
                if (settings == null || !settings.TimingScatter_IgnoreOutliers) return null;
                if (!ModContext.IsLevelFinished) return null;  
                return "Ignored " + _ignoredOutlierCount;
            }
        }

        private int EffectivePointLimit
        {
            get
            {
                int configured = DefaultRenderPoints;
                if (ModContext.Settings != null)
                    configured = Mathf.Clamp(ModContext.Settings.TimingScatter_MaxRenderPoints, MinRenderPoints, MaxRenderPoints);

                return Mathf.Min(configured, MaxSafePointCount);
            }
        }

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
        }

        protected override void ApplyFontToTexts(TMP_FontAsset font)
        {
            base.ApplyFontToTexts(font);
            ApplyFontToText(_xStartText, font);
            ApplyFontToText(_xEndText, font);
            ApplyFontToText(_avgValueText, font);
            ApplyFontToText(_detailText, font);
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
                hash = hash * 31 + (settings.TimingScatter_UseHitAxis ? 1 : 0);
                hash = hash * 31 + (settings.TimingScatter_AutoScroll ? 1 : 0);
                hash = hash * 31 + (settings.TimingScatter_UseJudgeColor ? 1 : 0);
                hash = hash * 31 + (settings.TimingScatter_ShowZeroLine ? 1 : 0);
                hash = hash * 31 + (settings.TimingScatter_ShowAvgLine ? 1 : 0);
                hash = hash * 31 + (settings.TimingScatter_IgnoreOutliers ? 1 : 0);
                hash = hash * 31 + settings.TimingScatter_PointSize.GetHashCode();
                hash = hash * 31 + settings.TimingScatter_PointColor.GetHashCode();
                hash = hash * 31 + settings.TimingScatter_ZeroLineColor.GetHashCode();
                hash = hash * 31 + settings.TimingScatter_AvgLineColor.GetHashCode();
                hash = hash * 31 + settings.TimingScatter_AxisTextColor.GetHashCode();
                return hash;
            }
        }

        protected override string FormatYLabel(float value) => $"{value:+0.0;-0.0;0.0}ms";
        
        private bool LiveScrollActive
        {
            get
            {
                if (!_hasData || !_useTimeAxis) return false;
                if (ModContext.IsLevelFinished) return false;

                Settings settings = ModContext.Settings;
                if (settings == null || !settings.TimingScatter_AutoScroll) return false;

                return Patches.PlayStatePatches.GetSessionTimeMs() > 0.0;
            }
        }

        protected override bool NeedsContinuousRedraw() => LiveScrollActive;

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
            _visibleIndices.Clear();
            _renderPoints.Clear();
            _hasData = false;
            _useTimeAxis = false;
            _minY = 0f;
            _maxY = 0f;
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

            List<TimingScatterSample> samples = ModContext.TimingScatterSamples;
            int total = samples.Count;
            if (total == 0)
            {
                _smoothViewValid = false;
                _smoothSpanValid = false;
                _avgSmoothValid = false;
                return;
            }

            Settings settings = ModContext.Settings;
            bool finished = ModContext.IsLevelFinished;
            
            int take = int.MaxValue;
            if (!finished)
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
                return;
            }
            
            _visibleIndices.Reverse();
            
            if (finished && settings != null && settings.TimingScatter_IgnoreOutliers)
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

            _hasData = true;
            _firstSourceIndex = _visibleIndices[0];
            _lastSourceIndex = _visibleIndices[collected - 1];

            if (timeValid)
            {
                _minTimeMs = minTime;
                _maxTimeMs = maxTime;
            }
            
            bool wantHitAxis = settings != null && settings.TimingScatter_UseHitAxis;
            _useTimeAxis = !wantHitAxis && timeValid && (maxTime - minTime) >= MinTimeAxisRangeMs;

            UpdateLiveTimeWindow();

            float range = maxOffset - minOffset;
            float targetMinY;
            float targetMaxY;
            if (range < MinYRangeMs)
            {
                float mid = (minOffset + maxOffset) * 0.5f;
                targetMinY = mid - MinYRangeMs * 0.5f;
                targetMaxY = mid + MinYRangeMs * 0.5f;
            }
            else
            {
                float pad = Mathf.Max(MinYPadMs, range * YPadRatio);
                targetMinY = minOffset - pad;
                targetMaxY = maxOffset + pad;
            }

            ApplyViewScaling(targetMinY, targetMaxY, minOffset, maxOffset);

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
                _minY = targetMinY;
                _maxY = targetMaxY;
                return;
            }

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

            _minY = _smoothMinY;
            _maxY = _smoothMaxY;
        }


        private void BuildAverageSeries(List<TimingScatterSample> samples, int count)
        {
            Settings settings = ModContext.Settings;
            if (settings == null || !settings.TimingScatter_ShowAvgLine) return;
            
            bool dropOutliers = ModContext.IsLevelFinished && settings.TimingScatter_IgnoreOutliers;

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
            int limit = EffectivePointLimit;

            if (count <= limit)
            {
                for (int i = 0; i < count; i++)
                    _renderPoints.Add(ToRenderPoint(samples, _visibleIndices[i]));
                return;
            }

            int bucketCount = Mathf.Max(1, Mathf.CeilToInt(rectTransform.rect.width));
            int maxPairs = Mathf.Max(1, limit / 2);
            if (bucketCount > maxPairs) bucketCount = maxPairs;

            EnsureBucketCapacity(bucketCount);
            for (int i = 0; i < bucketCount; i++) _bucketFilled[i] = false;

            int indexSpan = _lastSourceIndex - _firstSourceIndex;

            for (int i = 0; i < count; i++)
            {
                int sourceIndex = _visibleIndices[i];
                TimingScatterSample s = samples[sourceIndex];

                float nx;
                if (_useTimeAxis)
                {
                    float liveSpan = _liveRightTimeMs - _liveLeftTimeMs;
                    nx = liveSpan > 0f ? (s.TimeMs - _liveLeftTimeMs) / liveSpan : 0.5f;
                    if (nx < 0f) continue;
                }
                else nx = indexSpan > 0 ? (sourceIndex - _firstSourceIndex) / (float)indexSpan : 0.5f;

                int bucket = (int)(Mathf.Clamp01(nx) * bucketCount);
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

        private float NormalizedX(RenderPoint point)
        {
            if (_useTimeAxis)
            {
                float timeSpan = _liveRightTimeMs - _liveLeftTimeMs;
                if (timeSpan <= 0f) return 0.5f;
                return (point.TimeMs - _liveLeftTimeMs) / timeSpan;
            }

            int indexSpan = _lastSourceIndex - _firstSourceIndex;
            if (indexSpan <= 0) return 0.5f;
            return Mathf.Clamp01((point.SourceIndex - _firstSourceIndex) / (float)indexSpan);
        }

        protected override void DrawReferenceLines(VertexHelper vh, float w, float h)
        {
            Settings settings = ModContext.Settings;
            if (settings == null || !settings.TimingScatter_ShowZeroLine) return;
            if (_minY > 0f || _maxY < 0f) return;

            float rangeY = Mathf.Max(0.01f, _maxY - _minY);
            float y = Mathf.Clamp01((0f - _minY) / rangeY) * h;
            float halfWidth = Mathf.Max(0.5f, 1.5f * Mathf.Max(0.01f, Scale) * 0.5f);
            DrawSegment(vh, new Vector2(0f, y), new Vector2(w, y), halfWidth, settings.TimingScatter_ZeroLineColor);
        }

        protected override void DrawSeries(VertexHelper vh, float w, float h)
        {
            int count = _renderPoints.Count;

            Settings settings = ModContext.Settings;
            bool judgeColor = settings == null || settings.TimingScatter_UseJudgeColor;
            Color uniformColor = settings != null ? settings.TimingScatter_PointColor : DefaultPointColor;
            float scale = Mathf.Max(0.01f, Scale);
            float radius = Mathf.Max(0.5f, (settings != null ? settings.TimingScatter_PointSize : 3f) * scale * 0.5f);
            float rangeY = Mathf.Max(0.01f, _maxY - _minY);
            
            DrawAverageLine(vh, w, h, settings, scale, rangeY);

            int circleSegments = CircleSegmentsFor(count);

            for (int i = 0; i < count; i++)
            {
                RenderPoint point = _renderPoints[i];
                if (!TryGetPointLocalPosition(point, w, h, out Vector2 center)) continue;

                Color color = judgeColor ? JColors.GetColor(point.Judge, point.IsXPerfect, true) : uniformColor;
                DrawCircle(vh, center, radius, circleSegments, color);
            }

            DrawDetailPanel(vh, scale);
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
            if (!_avgAvailable || settings == null || !settings.TimingScatter_ShowAvgLine) return;
            if (_avgValues.Count != _visibleIndices.Count || _avgValues.Count < 2) return;

            int count = _renderPoints.Count;
            float halfWidth = Mathf.Max(0.5f, 1.6f * scale * 0.5f);
            Color color = settings.TimingScatter_AvgLineColor;

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
            Color color = settings != null ? settings.TimingScatter_AxisTextColor : Color.white;

            string startText = string.Empty;
            string endText = string.Empty;

            if (_hasData)
            {
                if (_useTimeAxis)
                {
                    startText = $"{_liveLeftTimeMs / 1000f:F1}s";
                    endText = $"{_liveRightTimeMs / 1000f:F1}s";
                }
                else
                {
                    startText = $"#{_firstSourceIndex + 1}";
                    endText = $"#{_lastSourceIndex + 1}";
                }
            }

            SetupXAxisText(_xStartText, startText, new Vector2(2f * scale, -3f * scale), new Vector2(0f, 1f), TextAlignmentOptions.TopLeft, fontSize, labelHeight, color);
            SetupXAxisText(_xEndText, endText, new Vector2(w - 2f * scale, -3f * scale), new Vector2(1f, 1f), TextAlignmentOptions.TopRight, fontSize, labelHeight, color);

            bool showAvgLabel = _avgAvailable && settings != null && settings.TimingScatter_ShowAvgLine;
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
            _avgValueText.color = settings.TimingScatter_AvgLineColor;
            _avgValueText.alignment = TextAlignmentOptions.MidlineLeft;

            RectTransform rt = _avgValueText.rectTransform;
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(w + 6f * scale, ny * h);
            rt.sizeDelta = new Vector2(120f * (fontSize / 12f), 24f * (fontSize / 12f));
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


        
        // hit detail
        protected override void Update()
        {
            base.Update();
            UpdateInteraction();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ClearDetailInteraction();
        }

        private bool DetailInteractive
        {
            get
            {
                if (!IsEnabled || !ShowGraph) return false;
                if (ModContext.IsLevelFinished) return true;
                return scrController.instance != null && scrController.instance.paused;
            }
        }

        private bool TryGetPointLocalPosition(RenderPoint point, float w, float h, out Vector2 local)
        {
            float nx = NormalizedX(point);
            if (nx < 0f)
            {
                local = Vector2.zero;
                return false;
            }

            float rangeY = Mathf.Max(0.01f, _maxY - _minY);
            float ny = Mathf.Clamp01((point.OffsetMs - _minY) / rangeY);
            local = new Vector2(nx * w, ny * h);
            return true;
        }

        private void UpdateInteraction()
        {
            if (_detailDataVersion != ModContext.TimingScatterVersion)
            {
                _detailDataVersion = ModContext.TimingScatterVersion;
                ClearDetailInteraction();
            }

            if (!DetailInteractive)
            {
                ClearDetailInteraction();
                return;
            }

            float w = rectTransform.rect.width;
            float h = rectTransform.rect.height;
            if (w <= 0f || h <= 0f || _renderPoints.Count == 0)
            {
                _hoverSourceIndex = -1;
                RefreshDetailPanel();
                return;
            }

            bool insideRect = false;
            Vector2 localMouse = Vector2.zero;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, Input.mousePosition, null, out Vector2 converted))
            {
                localMouse = converted;
                insideRect = localMouse.x >= 0f && localMouse.x <= w && localMouse.y >= 0f && localMouse.y <= h;
            }

            _hoverSourceIndex = insideRect ? FindNearestRenderPoint(localMouse, w, h) : -1;

            if (insideRect && _hoverSourceIndex >= 0 && Input.GetMouseButtonDown(0))
            {
                _selectedSourceIndex = _hoverSourceIndex == _selectedSourceIndex ? -1 : _hoverSourceIndex;
            }

            RefreshDetailPanel();
        }

        private int FindNearestRenderPoint(Vector2 localMouse, float w, float h)
        {
            Settings settings = ModContext.Settings;
            float scale = Mathf.Max(0.01f, Scale);
            float pointRadius = Mathf.Max(0.5f, (settings != null ? settings.TimingScatter_PointSize : 3f) * scale * 0.5f);
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
            int index = _selectedSourceIndex;
            if (index < 0 || !IsRenderableSourceIndex(index)) index = _hoverSourceIndex;
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
            _detailText.color = settings != null ? settings.TimingScatter_AxisTextColor : Color.white;
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
            _selectedSourceIndex = -1;
            HideDetailPanel();
        }

        private bool IsRenderableSourceIndex(int index)
        {
            List<TimingScatterSample> samples = ModContext.TimingScatterSamples;
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
            TimingScatterSample sample = ModContext.TimingScatterSamples[index];
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
