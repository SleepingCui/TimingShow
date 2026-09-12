using System;

namespace TimingShow
{

    public static class CalcXP
    {
        public static bool IsLegacyXPerfect(double diff, double bpm, double speed, double pitch)
        {
            if (HitMarginCompat.IsGame34) return false;
            if (XPerfectBridge.IsXPerfect()) return true;

            return Compute(diff, bpm, speed, pitch);
        }
        
        public static bool Compute(double diff, double bpm, double speed, double pitch)
        {
            double denominator = Math.PI * bpm * speed * pitch;
            if (denominator == 0) return false;

            // from https://github.com/8100print/XPerfect
            // Licensed under the MIT License.
            double absDiff = Math.Abs(diff);
            double angleR = 0.01667 * (denominator / 60.0);
            double angleD = angleR * 57.295780181884766;
            double fBoundaryD = Math.Max(15.0, angleD);
            double fBoundary = (fBoundaryD * 60000.0) / (57.295780181884766 * denominator);

            return absDiff <= fBoundary;
        }
    }
}
