using System;
using System.Collections.Generic;

namespace TimingShow.HUD
{
    internal sealed class DeltaData
    {
        public DistributionStats Histogram;
        public int PairCount;
        public double Mean;
        public double MeanAbs;
        public double MedianAbs;
        public double StdDev;
        public bool HasData;

        public static DeltaData Compute(List<TimingScatterSample> samples, int playerId)
        {
            DeltaData data = new DeltaData();
            List<double> deltas = new List<double>();

            bool hasPrevious = false;
            double previous = 0.0;

            if (samples != null)
            {
                for (int i = 0; i < samples.Count; i++)
                {
                    TimingScatterSample sample = samples[i];
                    if (playerId >= 0 && sample.PlayerId != playerId) continue;

                    if (!sample.HasJudge || !IsTap(sample.Judge))
                    {
                        hasPrevious = false;
                        continue;
                    }

                    float value = sample.OffsetMs;
                    if (float.IsNaN(value) || float.IsInfinity(value))
                    {
                        hasPrevious = false;
                        continue;
                    }

                    if (hasPrevious) deltas.Add(value - previous);
                    previous = value;
                    hasPrevious = true;
                }
            }

            data.PairCount = deltas.Count;
            if (deltas.Count == 0) return data;

            int count = deltas.Count;
            double sum = 0.0;
            double sumAbs = 0.0;
            for (int i = 0; i < count; i++)
            {
                sum += deltas[i];
                sumAbs += Math.Abs(deltas[i]);
            }

            data.Mean = sum / count;
            data.MeanAbs = sumAbs / count;

            double sumSquares = 0.0;
            for (int i = 0; i < count; i++)
            {
                double delta = deltas[i] - data.Mean;
                sumSquares += delta * delta;
            }
            data.StdDev = Math.Sqrt(sumSquares / count);

            double[] magnitudes = new double[count];
            for (int i = 0; i < count; i++) magnitudes[i] = Math.Abs(deltas[i]);
            Array.Sort(magnitudes);
            int mid = count >> 1;
            data.MedianAbs = (count & 1) != 0
                ? magnitudes[mid]
                : (magnitudes[mid - 1] + magnitudes[mid]) * 0.5;

            data.Histogram = DistributionStats.ComputeValues(
                deltas, DistributionStats.DefaultBinCount, false);
            data.HasData = data.Histogram != null && data.Histogram.HasData;
            return data;
        }

        private static bool IsTap(HitMan judge)
        {
            return ReturnData.IsTap(judge);
        }
    }
}
