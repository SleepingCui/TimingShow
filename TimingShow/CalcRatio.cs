using static TimingShow.Patches.TimingCalcPatches;

namespace TimingShow
{
    public class CalcRatio
    {
        public static string GetRatioString()
        {
            bool useXP = Main.Settings.Ratio_UseXPerfect;
            int targetHits = useXP ? MarginTrackerAddHitPatch.XPerfectCount : MarginTrackerAddHitPatch.PerfectCount;
            int total = MarginTrackerAddHitPatch.TotalHitsCount;
            int otherHits = total - targetHits;
            if (total == 0) return "0";
            if (otherHits == 0) return "infinity";

            double ratio = (double)targetHits / otherHits;
            return ratio.ToString("F" + Main.Settings.PercRatioHUD);
        }
    }
}
