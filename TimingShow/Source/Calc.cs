using System;
using System.Collections.Generic;
using TimingShow.Bridge;

namespace TimingShow
{
    public static class CalcUR
    {

        public static double Mean(List<double> offsets)
        {
            if (offsets == null || offsets.Count == 0) return 0.0;

            double avg = 0.0;
            int count = offsets.Count;
            for (int i = 0; i < count; i++) avg += offsets[i];
            return avg / count;
        }

        public static double calc(List<double> offsets)
        {
            if (offsets == null || offsets.Count == 0) return 0.0;

            double avg = Mean(offsets);
            int count = offsets.Count;

            double sumOfSquares = 0.0;
            for (int i = 0; i < count; i++)
            {
                double diff = offsets[i] - avg;
                sumOfSquares += diff * diff;
            }

            double stdDev = Math.Sqrt(sumOfSquares / count);
            return stdDev * 10.0;
        }
    }

    public class CalcRatio
    {
        public static string GetRatioString(PlayerSession player)
        {
            Settings settings = ModContext.Settings;

            int targetHits;
            switch (settings.Ratio_Mode)
            {
                case Settings.RatioMode_PerfectFamily:
                    targetHits = player.PerfectFamilyCount;
                    break;
                case Settings.RatioMode_XPerfect:
                    targetHits = player.XPerfectCount;
                    break;
                case Settings.RatioMode_NormalPerfect:
                    targetHits = player.NormalPerfectCount;
                    break;
                default:
                    targetHits = player.NormalPerfectCount;
                    break;
            }

            int total = player.TotalHitsCount;
            int otherHits = total - targetHits;
            if (total == 0) return "0";
            if (otherHits <= 0) return "infinity";

            double ratio = (double)targetHits / otherHits;
            return ratio.ToString("F" + settings.PercRatioHUD);
        }

        public static string GetRatioText(PlayerSession player)
        {
            return GetRatioString(player) + ":1";
        }
    }

    public static class CalcXP
    {
        public static bool IsLegacyXPerfect(double diff, double bpm, double speed, double pitch)
        {
            if (HitMarginCompat.IsGame34) return false;
            if (XPerfectBridge.IsXPerfect()) return true;

            return Compute(diff, bpm, speed, pitch);
        }
        
        // from https://github.com/8100print/XPerfect
        // Licensed under the MIT License.
        public static double GetBoundaryMs(double effectiveBpm)
        {
            double denominator = Math.PI * effectiveBpm;
            if (denominator == 0.0) return 0.0;

            double angleR = 0.01667 * (denominator / 60.0);
            double angleD = angleR * 57.295780181884766;
            double fBoundaryD = Math.Max(15.0, angleD);

            return (fBoundaryD * 60000.0) / (57.295780181884766 * denominator);
        }

        private static bool Compute(double diff, double bpm, double speed, double pitch)
        {
            double fBoundary = GetBoundaryMs(bpm * speed * pitch);
            if (fBoundary == 0.0) return false;

            return Math.Abs(diff) <= fBoundary;
        }
    }
}
