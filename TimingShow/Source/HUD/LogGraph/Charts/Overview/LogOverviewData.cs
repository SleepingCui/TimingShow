using System;
using System.Collections.Generic;

namespace TimingShow.HUD
{
    internal sealed class OverviewData
    {
        public int TotalHits;
        public int MaxCombo;
        public double Ur;
        public bool UrAvailable;
        public int[] Counts = new int[16];
        public double Xacc;
        public int RatioNumerator;
        public int RatioDenominator;
        public int XPerfectNumerator;
        public int XPerfectDenominator;
        public bool HasData;

        public static OverviewData Compute(List<TimingScatterSample> samples, int playerId, bool isAngle)
        {
            OverviewData data = new OverviewData();
            data.UrAvailable = !isAngle;
            if (samples == null) return data;

            double sum = 0.0;
            double sumSquares = 0.0;

            int combo = 0;
            int maxCombo = 0;
            int total = 0;

            for (int i = 0; i < samples.Count; i++)
            {
                TimingScatterSample sample = samples[i];
                if (!sample.HasJudge) continue;
                if (playerId >= 0 && sample.PlayerId != playerId) continue;

                total++;
                int judge = (int)sample.Judge;
                if (judge >= 0 && judge < data.Counts.Length) data.Counts[judge]++;

                bool keepsStreak = HitMarginCompat.IsPerfectFamily(sample.Judge) || sample.Judge == HitMan.Auto;
                if (keepsStreak)
                {
                    combo++;
                    if (combo > maxCombo) maxCombo = combo;
                }
                else
                {
                    combo = 0;
                }

                float offset = sample.OffsetMs;
                if (!float.IsNaN(offset) && !float.IsInfinity(offset))
                {
                    sum += offset;
                    sumSquares += offset * offset;
                }
            }

            data.TotalHits = total;
            data.MaxCombo = maxCombo;
            if (total == 0) return data;

            double avg = sum / total;
            double variance = sumSquares / total - avg * avg;
            if (variance < 0.0) variance = 0.0;
            data.Ur = Math.Sqrt(variance) * 10.0;

            int[] counts = data.Counts;
            int failMiss = counts[10] + counts[11] + counts[13] + counts[15];
            int perfectGroup = counts[3] + counts[4] + counts[5] + counts[12];
            int lateGroup = counts[7] + counts[8];

            int[] groups = { failMiss, counts[0], counts[1], counts[2], perfectGroup, counts[6], lateGroup };
            double[] weights = { 0.0, 0.2, 0.4, 0.75, 1.0, 0.75, 0.4 };

            int groupTotal = 0;
            double weightedSum = 0.0;
            for (int i = 0; i < groups.Length; i++)
            {
                groupTotal += groups[i];
                weightedSum += groups[i] * weights[i];
            }
            data.Xacc = groupTotal > 0 ? weightedSum / groupTotal * 100.0 : 0.0;

            data.RatioNumerator = counts[3] + counts[4] + counts[5] + counts[12];
            data.RatioDenominator = total - data.RatioNumerator;
            data.XPerfectNumerator = counts[4] + counts[12];
            data.XPerfectDenominator = total - data.XPerfectNumerator;

            data.HasData = true;
            return data;
        }
    }
}
