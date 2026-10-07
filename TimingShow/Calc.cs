using System;
using System.Collections.Generic;
using static TimingShow.Patches.TimingCalcPatches;

namespace TimingShow
{
    public static class CalcUR
    {
        private static int _count;
        private static double _mean;
        private static double _s;

        public static void AddSample(double x)
        {
            int n = ++_count;
            double d = x - _mean;
            _mean += d / n;
            _s += d * (x - _mean);
        }

        public static void Reset()
        {
            _count = 0;
            _mean = 0.0;
            _s = 0.0;
        }

        public static double Calc()
        {
            if (_count == 0) return 0.0;
            return Math.Sqrt(_s / _count) * 10.0;
        }

        public static double Mean()
        {
            return _count == 0 ? 0.0 : _mean;
        }

        public static double calc(List<double> offsets)
        {
            if (offsets == null || offsets.Count == 0) return 0.0;

            double avg = 0.0;
            int count = offsets.Count;
            for (int i = 0; i < count; i++) avg += offsets[i];
            avg /= count;

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
        public static string GetRatioString()
        {
            int targetHits;
            switch (ModContext.Settings.Ratio_Mode)
            {
                case Settings.RatioMode_PerfectFamily:
                    targetHits = MarginTrackerAddHitPatch.PerfectFamilyCount;
                    break;
                case Settings.RatioMode_XPerfect:
                    targetHits = MarginTrackerAddHitPatch.XPerfectCount;
                    break;
                case Settings.RatioMode_NormalPerfect:
                    targetHits = MarginTrackerAddHitPatch.NormalPerfectCount;
                    break;
                default:
                    targetHits = MarginTrackerAddHitPatch.NormalPerfectCount;
                    break;
            }

            int total = MarginTrackerAddHitPatch.TotalHitsCount;
            int otherHits = total - targetHits;
            if (total == 0) return "0";
            if (otherHits <= 0) return "infinity";

            double ratio = (double)targetHits / otherHits;
            return ratio.ToString("F" + ModContext.Settings.PercRatioHUD);
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
