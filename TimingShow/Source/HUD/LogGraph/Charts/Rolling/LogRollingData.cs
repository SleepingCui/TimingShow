using System;
using System.Collections.Generic;

namespace TimingShow.HUD
{
    internal sealed class RollingData
    {
        public float[] Mean;
        public float[] Upper;
        public float[] Lower;
        public int HitCount;
        public int WindowSize;
        public bool HasData;
        public double HeadMean = double.NaN;
        public double TailMean = double.NaN;
        public double Drift = double.NaN;

        public static RollingData Compute(List<TimingScatterSample> samples, int playerId, int windowSize)
        {
            RollingData data = new RollingData();
            if (windowSize < 1) windowSize = 1;
            data.WindowSize = windowSize;

            if (samples == null) return data;

            int count = 0;
            for (int i = 0; i < samples.Count; i++)
            {
                TimingScatterSample sample = samples[i];
                if (!sample.HasJudge) continue;
                if (playerId >= 0 && sample.PlayerId != playerId) continue;
                count++;
            }

            data.HitCount = count;
            if (count == 0) return data;

            data.Mean = new float[count];
            data.Upper = new float[count];
            data.Lower = new float[count];

            double[] window = new double[windowSize];
            int windowCount = 0;
            int windowPos = 0;
            double sum = 0.0;
            double sumSquares = 0.0;
            double headSum = 0.0;
            double tailSum = 0.0;
            int headCount = 0;
            int tailCount = 0;
            int headLimit = Math.Max(1, (int)Math.Floor(count * 0.25));
            int tailStart = count - Math.Max(1, (int)Math.Floor(count * 0.25));
            int ordinal = 0;
            bool hasValue = false;

            for (int i = 0; i < samples.Count; i++)
            {
                TimingScatterSample sample = samples[i];
                if (!sample.HasJudge) continue;
                if (playerId >= 0 && sample.PlayerId != playerId) continue;

                int index = ordinal++;
                float value = sample.OffsetMs;
                if (!float.IsNaN(value) && !float.IsInfinity(value))
                {
                    if (windowCount < windowSize)
                    {
                        window[(windowPos + windowCount) % windowSize] = value;
                        windowCount++;
                    }
                    else
                    {
                        double dropped = window[windowPos];
                        window[windowPos] = value;
                        windowPos = (windowPos + 1) % windowSize;
                        sum -= dropped;
                        sumSquares -= dropped * dropped;
                    }
                    sum += value;
                    sumSquares += value * value;

                    if (headCount < headLimit)
                    {
                        headSum += value;
                        headCount++;
                    }
                    if (index >= tailStart)
                    {
                        tailSum += value;
                        tailCount++;
                    }
                }

                if (windowCount == 0)
                {
                    data.Mean[index] = float.NaN;
                    data.Upper[index] = float.NaN;
                    data.Lower[index] = float.NaN;
                    continue;
                }

                double mean = sum / windowCount;
                double variance = sumSquares / windowCount - mean * mean;
                double sigma = variance > 0.0 ? Math.Sqrt(variance) : 0.0;
                data.Mean[index] = (float)mean;
                data.Upper[index] = (float)(mean + sigma);
                data.Lower[index] = (float)(mean - sigma);
                hasValue = true;
            }

            if (!hasValue) return data;

            data.HeadMean = headCount > 0 ? headSum / headCount : double.NaN;
            data.TailMean = tailCount > 0 ? tailSum / tailCount : double.NaN;
            data.Drift = !double.IsNaN(data.HeadMean) && !double.IsNaN(data.TailMean)
                ? data.TailMean - data.HeadMean
                : double.NaN;
            data.HasData = true;
            return data;
        }
    }
}
