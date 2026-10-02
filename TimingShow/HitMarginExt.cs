using System;

namespace TimingShow
{

    public static class HitMarginExt
    {
 
        public const byte MarginVersion = 2;
        public const string MarginVersionName = "Game34";
        public const int JudgeCodeVersion = 1;

        public static readonly HitMargin[] All = (HitMargin[])Enum.GetValues(typeof(HitMargin));

        public static bool IsPerfectFamily(this HitMargin m)
        {
            return m == HitMargin.EarlyPerfect
                || m == HitMargin.PerfectMinus
                || m == HitMargin.XPerfect
                || m == HitMargin.PerfectPlus
                || m == HitMargin.LatePerfect;
        }

        public static bool IsNormalPerfect(this HitMargin m)
        {
            return m == HitMargin.PerfectMinus || m == HitMargin.PerfectPlus;
        }

        public static bool IsFailFamily(this HitMargin m)
        {
            return m == HitMargin.FailMiss
                || m == HitMargin.FailOverload
                || m == HitMargin.OverPress
                || m == HitMargin.FailedFloor;
        }
    }
}
