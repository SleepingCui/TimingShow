using System.Collections.Generic;
using TimingShow.Diagnostics;
using UnityEngine;

namespace TimingShow.HUD.ScatterDrawerBase
{
    public abstract partial class ScatterDrawerBase
    {

        protected virtual List<TimingScatterSample> ScatterSamples => ModContext.TimingScatterSamples;
        protected virtual int ScatterVersion => ModContext.TimingScatterVersion;
        private const float MinYRangeMs = 2f;
        private const float MinYPadMs = 1.5f;
        private const float YPadRatio = 0.08f;
        private const float ViewSmoothTauSeconds = 0.30f;
        private const float AvgSmoothTauSeconds = 0.30f;

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
        private int _offsetRangeKept;
        private int _offsetRangeHidden;

        private float _smoothMinY;
        private float _smoothMaxY;
        private bool _smoothViewValid;
        private float _smoothSpanMs;
        private bool _smoothSpanValid;

        private float _dataYMin;
        private float _dataYMax;
        private readonly List<float> _robustScratch = new List<float>(256);
        private int _lastDataSignature = int.MinValue;
        private float _cachedTargetMinY;
        private float _cachedTargetMaxY;

        protected virtual bool HasOffsetRangeFilter => false;
        protected virtual bool IsOffsetInRange(float value) => true;
        protected int OffsetRangeKept => _offsetRangeKept;
        protected int OffsetRangeHidden => _offsetRangeHidden;

        protected override void UpdateData()
        {
            ResetViewOnLayoutChange();

            int dataSignature = ComputeDataSignature();
            if (_hasData && dataSignature == _lastDataSignature)
            {
                RefreshLiveView();
                return;
            }
            _lastDataSignature = dataSignature;

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
            _offsetRangeKept = 0;
            _offsetRangeHidden = 0;

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

            long mark = PerfPhases.Mark();
            int collected = 0;
            int scanned = 0;
            for (int i = total - 1; i >= 0 && scanned < take; i--)
            {
                if (!samples[i].HasJudge) continue;
                scanned++;
                if (_playerFilter >= 0 && samples[i].PlayerId != _playerFilter) continue;
                if (!IsOffsetInRange(samples[i].OffsetMs))
                {
                    _offsetRangeHidden++;
                    continue;
                }
                _offsetRangeKept++;
                _visibleIndices.Add(i);
                collected++;
            }
            PerfPhases.Record(PerfPhase.DataFilter, PerfPhases.Since(mark));
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
            {
                mark = PerfPhases.Mark();
                collected = ApplyOutlierFilter(samples, collected);
                PerfPhases.Record(PerfPhase.DataOutliers, PerfPhases.Since(mark));

                if (PerfStatsRecorder.IsRecording && _ignoredOutlierCount > 0)
                    PerfStatsRecorder.GetDrawer(PerfName).OutliersExcluded += _ignoredOutlierCount;
            }

            mark = PerfPhases.Mark();
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
            PerfPhases.Record(PerfPhase.DataBounds, PerfPhases.Since(mark));

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
            {
                mark = PerfPhases.Mark();
                ComputeRobustYBounds(samples, collected, ref viewLow, ref viewHigh);
                PerfPhases.Record(PerfPhase.DataRobustBounds, PerfPhases.Since(mark));
            }

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

            _cachedTargetMinY = targetMinY;
            _cachedTargetMaxY = targetMaxY;
            ApplyViewScaling(targetMinY, targetMaxY, minOffset, maxOffset);

            if (!_manualViewActive)
            {
                _viewXMin = _autoXMin;
                _viewXMax = _autoXMax;
            }

            mark = PerfPhases.Mark();
            BuildAverageSeries(samples, collected);
            PerfPhases.Record(PerfPhase.DataAverage, PerfPhases.Since(mark));

            mark = PerfPhases.Mark();
            BuildRenderPoints(samples, collected);
            PerfPhases.Record(PerfPhase.DataRender, PerfPhases.Since(mark));
        }

        private int ComputeDataSignature()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + ScatterVersion;
                Rect rect = rectTransform.rect;
                hash = hash * 31 + rect.width.GetHashCode();
                hash = hash * 31 + rect.height.GetHashCode();
                hash = hash * 31 + ComputeSettingsHash();
                return hash;
            }
        }

        protected void InvalidateDataCache()
        {
            _lastDataSignature = int.MinValue;
        }

        private void RefreshLiveView()
        {
            UpdateLiveTimeWindow();

            if (_useTimeAxis)
            {
                _autoXMin = Mathf.Min(_liveLeftTimeMs, _liveRightTimeMs);
                _autoXMax = Mathf.Max(_liveLeftTimeMs, _liveRightTimeMs);
            }

            ApplyViewScaling(_cachedTargetMinY, _cachedTargetMaxY, _dataYMin, _dataYMax);

            if (!_manualViewActive)
            {
                _viewXMin = _autoXMin;
                _viewXMax = _autoXMax;
            }

            BuildRenderPoints(ScatterSamples, _visibleIndices.Count);
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
            bool restrictToVisible = dropOutliers || _playerFilter >= 0 || HasOffsetRangeFilter;

            int visible = 0;
            double sum = 0.0;
            int judged = 0;

            for (int i = 0; i < samples.Count && visible < count; i++)
            {
                TimingScatterSample s = samples[i];
                if (!s.HasJudge) continue;

                if (restrictToVisible && !(visible < count && _visibleIndices[visible] == i))
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
                RecordRenderedPoints(count);
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

            RecordRenderedPoints(count);
        }

        private void RecordRenderedPoints(int sourceCount)
        {
            if (!PerfStatsRecorder.IsRecording) return;
            int dropped = sourceCount - _renderPoints.Count;
            if (dropped <= 0) return;
            PerfStatsRecorder.GetDrawer(PerfName).DroppedPoints += dropped;
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
                IsXPerfect = s.IsXPerfect,
                PlayerId = s.PlayerId
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

    }
}
