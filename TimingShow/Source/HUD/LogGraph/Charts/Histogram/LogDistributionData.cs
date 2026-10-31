using System;
using System.Collections.Generic;

namespace TimingShow.HUD
{
    internal struct HistogramBin
    {
        public float Start;
        public float End;
        public float Center;
        public int Count;
    }

    internal sealed class DistributionStats
    {
        public const int DefaultBinCount = 60;
        public const int CurveSampleCount = 201;

        private static readonly HistogramBin[] NoBins = new HistogramBin[0];

        public HistogramBin[] Bins = NoBins;
        public int BinCount;
        public int ValidCount;
        public int ClippedCount;
        public float AxisMin;
        public float AxisMax;
        public double Mean;
        public double StdDev;
        public double Skewness = double.NaN;
        public double ExcessKurtosis = double.NaN;
        public int MaxBinCount;
        public bool HasCurve;
        public float[] CurveX;
        public float[] CurveY;
        public float CurveMax;

        public bool HasData { get { return ValidCount > 0 && Bins.Length > 0; } }

        public static DistributionStats Compute(List<TimingScatterSample> samples, int binCount)
        {
            if (samples == null || samples.Count == 0)
            {
                DistributionStats empty = new DistributionStats();
                if (binCount < 1) binCount = 1;
                empty.BinCount = binCount;
                return empty;
            }

            List<double> values = new List<double>(Math.Min(samples.Count, 1 << 16));
            for (int i = 0; i < samples.Count; i++)
            {
                TimingScatterSample sample = samples[i];
                if (!sample.HasJudge) continue;

                float value = sample.OffsetMs;
                if (float.IsNaN(value) || float.IsInfinity(value)) continue;
                values.Add(value);
            }

            return ComputeValues(values, binCount, true);
        }

        public static DistributionStats ComputeValues(List<double> values, int binCount, bool buildCurve)
        {
            DistributionStats stats = new DistributionStats();
            if (binCount < 1) binCount = 1;
            stats.BinCount = binCount;
            if (values == null || values.Count == 0) return stats;

            int count = values.Count;
            stats.ValidCount = count;
            if (count == 0) return stats;

            double sum = 0.0;
            for (int i = 0; i < count; i++) sum += values[i];
            double mean = sum / count;

            double sumSquares = 0.0;
            for (int i = 0; i < count; i++)
            {
                double delta = values[i] - mean;
                sumSquares += delta * delta;
            }
            double variance = sumSquares / count;
            double stdDev = variance > 0.0 ? Math.Sqrt(variance) : 0.0;
            if (!IsFinite(mean)) mean = 0.0;
            if (!IsFinite(stdDev)) stdDev = 0.0;

            stats.Mean = mean;
            stats.StdDev = stdDev;

            if (stdDev > 0.0)
            {
                double third = 0.0;
                double fourth = 0.0;
                for (int i = 0; i < count; i++)
                {
                    double z = (values[i] - mean) / stdDev;
                    double z2 = z * z;
                    third += z2 * z;
                    fourth += z2 * z2;
                }

                double skewness = third / count;
                double kurtosis = fourth / count - 3.0;
                stats.Skewness = IsFinite(skewness) ? skewness : double.NaN;
                stats.ExcessKurtosis = IsFinite(kurtosis) ? kurtosis : double.NaN;
            }

            values.Sort();

            double dataMin = values[0];
            double dataMax = values[count - 1];
            double median = Quantile(values, 0.5);
            double q1 = Quantile(values, 0.25);
            double q3 = Quantile(values, 0.75);
            double robustSigma = (q3 - q1) / 1.349;

            double rangeMin;
            double rangeMax;
            if (!(robustSigma > 0.0))
            {
                rangeMin = dataMin;
                rangeMax = dataMax;
            }
            else
            {
                rangeMin = Math.Max(median - 4.0 * robustSigma, dataMin);
                rangeMax = Math.Min(median + 4.0 * robustSigma, dataMax);
            }

            if (!IsFinite(rangeMin) || !IsFinite(rangeMax) || rangeMax < rangeMin)
            {
                rangeMin = dataMin;
                rangeMax = dataMax;
            }

            double padding = (rangeMax - rangeMin) * 0.05;
            double extendedMin = rangeMin - padding;
            double extendedMax = rangeMax + padding;
            double span = extendedMax - extendedMin;
            double binWidth = span == 0.0 ? 1.0 : span / binCount;
            if (!IsFinite(binWidth) || binWidth <= 0.0) binWidth = 1.0;

            HistogramBin[] bins = new HistogramBin[binCount];
            for (int i = 0; i < binCount; i++)
            {
                float start = (float)(extendedMin + i * binWidth);
                float end = (float)(extendedMin + (i + 1) * binWidth);
                bins[i].Start = start;
                bins[i].End = end;
                bins[i].Center = start + (end - start) * 0.5f;
                bins[i].Count = 0;
            }

            stats.Bins = bins;
            stats.AxisMin = bins[0].Start;
            stats.AxisMax = bins[binCount - 1].End;

            int clipped = 0;
            int maxBinCount = 0;
            for (int i = 0; i < count; i++)
            {
                double value = values[i];
                if (value < extendedMin || value > extendedMax)
                {
                    clipped++;
                    continue;
                }

                int index = (int)Math.Floor((value - extendedMin) / binWidth);
                if (index < 0) index = 0;
                if (index >= binCount) index = binCount - 1;
                bins[index].Count++;
                if (bins[index].Count > maxBinCount) maxBinCount = bins[index].Count;
            }

            stats.ClippedCount = clipped;
            stats.MaxBinCount = maxBinCount;

            if (buildCurve) BuildCurve(stats, mean, stdDev, maxBinCount);
            return stats;
        }

        private static void BuildCurve(DistributionStats stats, double mean, double stdDev, int maxBinCount)
        {
            if (!(stdDev > 0.0)) return;

            float axisMin = stats.AxisMin;
            float axisMax = stats.AxisMax;
            float axisSpan = axisMax - axisMin;
            if (!(axisSpan > 0f) || float.IsNaN(axisSpan) || float.IsInfinity(axisSpan)) return;

            double peak = GaussianPdf(mean, mean, stdDev);
            if (!(peak > 0.0)) return;
            double scale = (maxBinCount > 0 ? maxBinCount : 1) / peak;
            if (!IsFinite(scale)) return;

            float[] curveX = new float[CurveSampleCount];
            float[] curveY = new float[CurveSampleCount];
            float maxY = 0f;

            for (int i = 0; i < CurveSampleCount; i++)
            {
                float x = axisMin + axisSpan * i / (CurveSampleCount - 1);
                double y = GaussianPdf(x, mean, stdDev) * scale;
                if (!IsFinite(y) || y < 0.0) y = 0.0;
                curveX[i] = x;
                curveY[i] = (float)y;
                if (curveY[i] > maxY) maxY = curveY[i];
            }

            stats.CurveX = curveX;
            stats.CurveY = curveY;
            stats.CurveMax = maxY;
            stats.HasCurve = true;
        }

        private static double GaussianPdf(double x, double mean, double stdDev)
        {
            if (!(stdDev > 0.0)) return 0.0;
            double z = (x - mean) / stdDev;
            return Math.Exp(-0.5 * z * z) / (stdDev * Math.Sqrt(2.0 * Math.PI));
        }

        private static double Quantile(List<double> sorted, double q)
        {
            int count = sorted.Count;
            if (count == 0) return 0.0;

            double position = (count - 1) * q;
            int lower = (int)Math.Floor(position);
            int upper = (int)Math.Ceiling(position);
            if (lower < 0) lower = 0;
            if (lower > count - 1) lower = count - 1;
            if (upper < 0) upper = 0;
            if (upper > count - 1) upper = count - 1;
            if (lower == upper) return sorted[lower];

            return sorted[lower] + (sorted[upper] - sorted[lower]) * (position - lower);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
