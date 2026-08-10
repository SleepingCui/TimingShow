using HarmonyLib;
using System;

namespace TimingShow.Patches
{
    public static class TimingCalcPatches
    {
        // timing calc
        [HarmonyPatch(typeof(scrPlanet), "SwitchChosen")]
        public static class PlanetSwitchPatch
        {
            public static void Prefix(scrPlanet __instance)
            {
                if (!ModContext.IsEnabled || scrController.instance == null) return;
                if (__instance.conductor == null || __instance.conductor.song == null) return;

                double bpm = __instance.conductor.bpm;
                double speed = scrController.instance.planetarySystem.speed;
                double pitch = __instance.conductor.song.pitch;
                bool isCW = scrController.instance.planetarySystem.isCW;

                if (bpm * speed * pitch == 0) return;
                double diff = (__instance.angle - __instance.targetExitAngle) * (isCW ? 1.0 : -1.0) * 60000.0 / (Math.PI * bpm * speed * pitch);

                ModContext.LastTiming = diff;
                UIPatches.UIReplacePatch.dirty = true;

                ModContext.LastIsXP = CalcXP.IsXPerfect(diff, bpm, speed, pitch);

                bool isAuto = RDC.auto;

                if (ModContext.IsPlaying)
                {
                    bool needRecord = ModContext.Settings.ShowInWinPage || ModContext.Settings.ShowURHUD || !isAuto || ModContext.Settings.LogAutoplay || ModContext.Settings.ShowXACCGraph;
                    if (needRecord && ModContext.SessionOffsets != null)
                    {
                        ModContext.SessionOffsets.Add(diff);
                    }

                    if (ModContext.FullXAccHistory != null && scrController.instance?.playerOne?.marginTracker != null)
                    {
                        float curXAcc = scrController.instance.playerOne.marginTracker.percentXAcc * 100f;
                        ModContext.FullXAccHistory.Add(curXAcc);
                    }

                    if (isAuto && ModContext.Settings.EnableLogging)
                    {
                        ModContext.LastHitMargin = HitMargin.Perfect;
                        TimingLogger.LogHit(diff, HitMargin.Perfect);
                    }
                }
            }
        }

        // hit
        [HarmonyPatch(typeof(scrMarginTracker), "AddHit")]
        public static class MarginTrackerAddHitPatch
        {
            public static int PerfectCount;
            public static int XPerfectCount;
            public static int TotalHitsCount;

            public static void Prefix(HitMargin hit)
            {
                if (!ModContext.IsEnabled || !ModContext.IsPlaying) return;

                ModContext.LastHitMargin = hit;
                TimingLogger.LogHit(ModContext.LastTiming, hit);
                TotalHitsCount++;
                if (hit == HitMargin.Perfect) PerfectCount++;
                if (ModContext.LastIsXP) XPerfectCount++;
            }

            public static void ResetCounts()
            {
                PerfectCount = 0;
                XPerfectCount = 0;
                TotalHitsCount = 0;
            }

            public static void SyncFromTracker(scrMarginTracker tracker)
            {
                if (tracker == null) return;
                PerfectCount = tracker.GetHits(HitMargin.Perfect);
                TotalHitsCount = (int)tracker.GetTotalHits();
            }
        }
    }
}
