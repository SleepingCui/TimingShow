using static TimingShow.Patches.TimingCalcPatches;

namespace TimingShow
{
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
}
