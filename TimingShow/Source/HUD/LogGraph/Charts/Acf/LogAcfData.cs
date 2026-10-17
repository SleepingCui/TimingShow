using System;
using System.Collections.Generic;

namespace TimingShow.HUD
{
    internal sealed class AcfEntry
    {
        public int Lag;
        public int Pairs;
        public double R = double.NaN;
        public double Band = double.NaN;
    }

    internal sealed class AcfData
    {
        public const int MaxLag = 50;
        private const double TieEpsilon = 1e-9;

        public AcfEntry[] Entries = new AcfEntry[MaxLag];
        public int TotalTaps;
        public AcfEntry Lag1;
        public AcfEntry Strongest;
        public double YSpan = 0.2;
        public bool HasData;

        public static AcfData Compute(List<TimingScatterSample> samples, int playerId)
        {
            AcfData data = new AcfData();
            if (samples == null) return data;

            List<double> values = new List<double>();
            List<int> runStarts = new List<int>();
            List<int> runEnds = new List<int>();

            int runStart = -1;
            for (int i = 0; i < samples.Count; i++)
            {
                TimingScatterSample sample = samples[i];
                if (playerId >= 0 && sample.PlayerId != playerId) continue;
                if (!sample.HasJudge) continue;

                if (!ReturnData.IsTap(sample.Judge))
                {
                    if (runStart >= 0)
                    {
                        runStarts.Add(runStart);
                        runEnds.Add(values.Count);
                        runStart = -1;
                    }
                    continue;
                }

                float value = sample.OffsetMs;
                if (float.IsNaN(value) || float.IsInfinity(value))
                {
                    if (runStart >= 0)
                    {
                        runStarts.Add(runStart);
                        runEnds.Add(values.Count);
                        runStart = -1;
                    }
                    continue;
                }

                if (runStart < 0) runStart = values.Count;
                values.Add(value);
            }

            if (runStart >= 0)
            {
                runStarts.Add(runStart);
                runEnds.Add(values.Count);
            }

            data.TotalTaps = values.Count;
            if (values.Count < 3) return data;

            for (int lag = 1; lag <= MaxLag; lag++)
            {
                AcfEntry entry = new AcfEntry();
                entry.Lag = lag;

                int pairs = 0;
                double sumA = 0.0, sumB = 0.0, sumAB = 0.0, sumAA = 0.0, sumBB = 0.0;

                for (int r = 0; r < runStarts.Count; r++)
                {
                    int first = runStarts[r];
                    int last = runEnds[r] - 1;
                    for (int i = first; i + lag <= last; i++)
                    {
                        double a = values[i];
                        double b = values[i + lag];
                        pairs++;
                        sumA += a;
                        sumB += b;
                        sumAB += a * b;
                        sumAA += a * a;
                        sumBB += b * b;
                    }
                }

                entry.Pairs = pairs;
                if (pairs >= 2)
                {
                    double meanA = sumA / pairs;
                    double meanB = sumB / pairs;
                    double sigmaA = Math.Sqrt(Math.Max(0.0, sumAA / pairs - meanA * meanA));
                    double sigmaB = Math.Sqrt(Math.Max(0.0, sumBB / pairs - meanB * meanB));
                    double product = sigmaA * sigmaB;
                    if (product > 0.0)
                        entry.R = (sumAB / pairs - meanA * meanB) / product;
                }

                if (pairs > 2)
                    entry.Band = 1.96 / Math.Sqrt(pairs - 1);

                data.Entries[lag - 1] = entry;
            }

            data.Lag1 = data.Entries[0];
            data.Strongest = FindStrongest(data.Entries);
            data.HasData = true;
            data.YSpan = ComputeYSpan(data.Entries);
            return data;
        }

        private static AcfEntry FindStrongest(AcfEntry[] entries)
        {
            AcfEntry best = null;
            for (int i = 0; i < entries.Length; i++)
            {
                AcfEntry entry = entries[i];
                if (entry == null || entry.Lag < 2) continue;
                if (double.IsNaN(entry.R) || double.IsNaN(entry.Band)) continue;
                if (Math.Abs(entry.R) <= entry.Band) continue;
                if (best == null || Math.Abs(entry.R) > Math.Abs(best.R) + TieEpsilon)
                    best = entry;
            }
            return best;
        }

        private static double ComputeYSpan(AcfEntry[] entries)
        {
            double maxAbs = 0.0;
            double maxBand = 0.0;
            for (int i = 0; i < entries.Length; i++)
            {
                AcfEntry entry = entries[i];
                if (entry == null) continue;
                if (!double.IsNaN(entry.R)) maxAbs = Math.Max(maxAbs, Math.Abs(entry.R));
                if (!double.IsNaN(entry.Band)) maxBand = Math.Max(maxBand, entry.Band);
            }

            double span = Math.Max(0.2, Math.Max(maxAbs * 1.3, maxBand * 1.3));
            return Math.Min(1.0, span);
        }
    }
}
