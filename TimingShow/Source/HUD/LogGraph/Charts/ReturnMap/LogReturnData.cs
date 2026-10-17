using System;
using System.Collections.Generic;

namespace TimingShow.HUD
{
    internal sealed class ReturnPair
    {
        public float Previous;
        public float Current;
        public int Hit;
    }

    internal sealed class ReturnData
    {
        public const int MaxRenderPairs = 4000;

        public List<ReturnPair> RenderPairs = new List<ReturnPair>();
        public int PairCount;
        public double Correlation = double.NaN;
        public float Limit = 1f;
        public int OutsideCount;
        public bool HasData;

        public static ReturnData Compute(List<TimingScatterSample> samples, int playerId)
        {
            ReturnData data = new ReturnData();
            if (samples == null) return data;

            int pairCount = CountPairs(samples, playerId);
            data.PairCount = pairCount;
            if (pairCount == 0) return data;

            int stride = pairCount > MaxRenderPairs
                ? (int)Math.Ceiling(pairCount / (double)MaxRenderPairs)
                : 1;

            float[] magnitudes = new float[pairCount * 2];
            int magnitudeIndex = 0;

            double sumX = 0.0, sumY = 0.0, sumXY = 0.0, sumXX = 0.0, sumYY = 0.0;

            bool hasPrevious = false;
            float previous = 0f;
            int ordinal = 0;
            int pairIndex = 0;

            for (int i = 0; i < samples.Count; i++)
            {
                TimingScatterSample sample = samples[i];
                if (playerId >= 0 && sample.PlayerId != playerId) continue;
                if (!sample.HasJudge) continue;

                ordinal++;
                if (!IsTap(sample.Judge))
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

                if (hasPrevious)
                {
                    pairIndex++;
                    if (pairIndex % stride == 0 || pairIndex == pairCount)
                    {
                        ReturnPair pair = new ReturnPair();
                        pair.Previous = previous;
                        pair.Current = value;
                        pair.Hit = ordinal;
                        data.RenderPairs.Add(pair);
                    }

                    magnitudes[magnitudeIndex++] = Math.Abs(previous);
                    magnitudes[magnitudeIndex++] = Math.Abs(value);

                    sumX += previous;
                    sumY += value;
                    sumXY += previous * value;
                    sumXX += previous * previous;
                    sumYY += value * value;
                }

                previous = value;
                hasPrevious = true;
            }

            data.Correlation = ComputeCorrelation(pairCount, sumX, sumY, sumXY, sumXX, sumYY);
            data.Limit = ComputeLimit(magnitudes);

            for (int i = 0; i < magnitudes.Length; i++)
            {
                if (magnitudes[i] > data.Limit) data.OutsideCount++;
            }

            data.HasData = true;
            return data;
        }

        private static int CountPairs(List<TimingScatterSample> samples, int playerId)
        {
            int count = 0;
            bool hasPrevious = false;

            for (int i = 0; i < samples.Count; i++)
            {
                TimingScatterSample sample = samples[i];
                if (playerId >= 0 && sample.PlayerId != playerId) continue;
                if (!sample.HasJudge) continue;

                if (!IsTap(sample.Judge))
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

                if (hasPrevious) count++;
                hasPrevious = true;
            }

            return count;
        }

        public static bool IsTap(HitMan judge)
        {
            switch (judge)
            {
                case HitMan.FailMiss:
                case HitMan.FailOverload:
                case HitMan.Auto:
                case HitMan.OverPress:
                case HitMan.Midspin:
                case HitMan.FailedFloor:
                case HitMan.Unknown:
                    return false;
                default:
                    return true;
            }
        }

        private static double ComputeCorrelation(int count,
            double sumX, double sumY, double sumXY, double sumXX, double sumYY)
        {
            if (count < 2) return double.NaN;

            double meanX = sumX / count;
            double meanY = sumY / count;
            double sigmaX = Math.Sqrt(Math.Max(0.0, sumXX / count - meanX * meanX));
            double sigmaY = Math.Sqrt(Math.Max(0.0, sumYY / count - meanY * meanY));
            if (!(sigmaX > 0.0) || !(sigmaY > 0.0)) return double.NaN;

            return (sumXY / count - meanX * meanY) / (sigmaX * sigmaY);
        }

        private static float ComputeLimit(float[] magnitudes)
        {
            if (magnitudes == null || magnitudes.Length == 0) return 1f;

            Array.Sort(magnitudes);
            int index = Math.Min(magnitudes.Length - 1, (int)Math.Floor(magnitudes.Length * 0.98));
            double value = magnitudes[index];
            if (!(value > 1.0)) value = 1.0;
            return (float)value;
        }
    }
}
