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
                if (!Main.IsEnabled || scrController.instance == null) return;
                if (__instance.conductor == null || __instance.conductor.song == null) return;

                double bpm = __instance.conductor.bpm;
                double speed = scrController.instance.planetarySystem.speed;
                double pitch = __instance.conductor.song.pitch;
                bool isCW = scrController.instance.planetarySystem.isCW;

                if (bpm * speed * pitch == 0) return;
                double diff = (__instance.angle - __instance.targetExitAngle) * (isCW ? 1.0 : -1.0) * 60000.0 / (Math.PI * bpm * speed * pitch);

                Main.LastTiming = diff;
                UIPatches.UIReplacePatch.dirty = true;

                Main.LastIsXP = CalcXP.IsXPerfect(diff, bpm, speed, pitch);

                bool isAuto = RDC.auto;

                if (Main.IsPlaying)
                {
                    bool needRecord = Main.Settings.ShowInWinPage || Main.Settings.ShowURHUD || !isAuto || Main.Settings.LogAutoplay || Main.Settings.ShowXACCGraph;
                    if (needRecord && Main.SessionOffsets != null)
                    {
                        Main.SessionOffsets.Add(diff);
                    }

                    if (Main.FullXAccHistory != null && scrController.instance?.playerOne?.marginTracker != null)
                    {
                        float curXAcc = scrController.instance.playerOne.marginTracker.percentXAcc * 100f;
                        Main.FullXAccHistory.Add(curXAcc);
                    }

                    if (isAuto && Main.Settings.EnableLogging)
                    {
                        Main.LastHitMargin = HitMargin.Perfect;
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
                if (!Main.IsEnabled || !Main.IsPlaying) return;

                Main.LastHitMargin = hit;
                TimingLogger.LogHit(Main.LastTiming, hit);
                TotalHitsCount++;
                if (hit == HitMargin.Perfect) PerfectCount++;

                var controller = scrController.instance;
                var conductor = scrController.conductor ?? scrConductor.instance ?? (controller != null && controller.chosenPlanet != null ? controller.chosenPlanet.conductor : null);

                if (controller != null && conductor != null && conductor.song != null)
                {
                    double bpm = conductor.bpm;
                    double speed = controller.planetarySystem != null ? controller.planetarySystem.speed : 1.0;
                    double pitch = conductor.song.pitch;

                    if (CalcXP.IsXPerfect(Main.LastTiming, bpm, speed, pitch)) XPerfectCount++;
                }
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
