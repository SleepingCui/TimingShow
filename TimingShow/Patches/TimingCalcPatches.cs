using HarmonyLib;
using System;

namespace TimingShow.Patches
{
    public static class TimingCalcPatches
    {

        [HarmonyPatch(typeof(scrMisc), "GetHitMarginInSec")]
        public static class HitMarginInSecPatch
        {
            public static void Postfix(double timeDiff)
            {
                if (!ModContext.IsEnabled) return;
                SetTiming(timeDiff * 1000.0);
            }
        }

        [HarmonyPatch(typeof(scrMisc), "GetHitMarginInDeg")]
        public static class HitMarginInDegPatch
        {
            public static void Postfix(float hitAngle, float refAngle, bool clockwise, float floorBpm, float conductorPitch)
            {
                if (!ModContext.IsEnabled) return;
                if (floorBpm == 0f || conductorPitch == 0f) return;
                
                double deltaRad = (hitAngle - refAngle) * (clockwise ? 1.0 : -1.0);
                SetTiming(deltaRad * 60000.0 / (Math.PI * floorBpm * conductorPitch));
            }
        }

        private static void SetTiming(double timingMs)
        {
            ModContext.LastTiming = timingMs;
            ModContext.LastSongTimeMs = PlayStatePatches.GetSessionTimeMs();
            ModContext.UIDirty = true;
        }


        [HarmonyPatch(typeof(scrPlanet), "SwitchChosen")]
        public static class PlanetSwitchPatch
        {
            public static void Postfix(scrPlanet __instance)
            {
                if (!ModContext.IsEnabled || scrController.instance == null) return;
                if (!ModContext.IsPlaying) return;

                double diff = ModContext.LastTiming;
                bool isAuto = RDC.auto;

                bool needRecord = ModContext.Settings.ShowInWinPage || ModContext.Settings.ShowURHUD || !isAuto || ModContext.Settings.LogAutoplay || ModContext.Settings.ShowXACCGraph;
                if (needRecord && ModContext.SessionOffsets != null)
                {
                    ModContext.SessionOffsets.Add(diff);
                    CalcUR.AddSample(diff);
                }

                if (ModContext.FullXAccHistory != null && scrController.instance?.playerOne?.marginTracker != null)
                {
                    float curXAcc = scrController.instance.playerOne.marginTracker.percentXAcc * 100f;
                    ModContext.FullXAccHistory.Add(curXAcc);
                    ModContext.XAccVersion++;
                }

                if (isAuto)
                {
                    MarginTrackerAddHitPatch.ApplyAutoHit(diff, ModContext.Settings.EnableLogging);
                }
            }
        }

        // hit
        [HarmonyPatch(typeof(scrMarginTracker), "AddHit")]
        public static class MarginTrackerAddHitPatch
        {
            public static int NormalPerfectCount;
            public static int PerfectFamilyCount;
            public static int XPerfectCount;
            public static int TotalHitsCount;

            public static void Prefix(HitMargin hit)
            {
                if (!ModContext.IsEnabled || !ModContext.IsPlaying) return;

                ApplyHit(hit, allowLog: !RDC.auto);
            }


            public static void ApplyAutoHit(double timing, bool allowLog)
            {
                ApplyHit(HitMargin.Auto, allowLog: allowLog, countHit: false, timingOverride: timing);
            }

            private static void ApplyHit(HitMargin judge, bool allowLog, bool countHit = true, double? timingOverride = null)
            {
                ModContext.LastJudge = judge;

                if (countHit)
                {
                    TotalHitsCount++;
                    if (judge.IsPerfectFamily()) PerfectFamilyCount++;
                    if (judge.IsNormalPerfect()) NormalPerfectCount++;
                    if (judge == HitMargin.XPerfect) XPerfectCount++;
                }

                if (allowLog)
                {
                    TimingLogger.LogHit(timingOverride ?? ModContext.LastTiming, (int)judge, judge);
                }
            }

            public static void ResetCounts()
            {
                NormalPerfectCount = 0;
                PerfectFamilyCount = 0;
                XPerfectCount = 0;
                TotalHitsCount = 0;
            }


            public static void SyncFromTracker(scrMarginTracker tracker)
            {
                if (tracker == null) return;

                try
                {
                    HitMargin[] all = HitMarginExt.All;
                    int normalPerfect = 0;
                    int perfectFamily = 0;

                    for (int i = 0; i < all.Length; i++)
                    {
                        HitMargin judge = all[i];
                        int count = tracker.GetHits(judge);
                        if (count <= 0) continue;

                        if (judge.IsPerfectFamily()) perfectFamily += count;
                        if (judge.IsNormalPerfect()) normalPerfect += count;
                    }

                    NormalPerfectCount = normalPerfect;
                    PerfectFamilyCount = perfectFamily;
                    TotalHitsCount = (int)tracker.GetTotalHits();
                    XPerfectCount = tracker.GetHits(HitMargin.XPerfect);
                }
                catch (Exception e)
                {
                    ModContext.Logger?.Log($"SyncFromTracker: {e.Message}");
                }
            }
        }
    }
}
