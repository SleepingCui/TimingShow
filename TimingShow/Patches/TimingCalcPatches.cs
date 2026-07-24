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

                double bpm = (double)__instance.conductor.bpm;
                double speed = (double)scrController.instance.planetarySystem.speed;
                double pitch = (double)__instance.conductor.song.pitch;
                bool isCW = scrController.instance.planetarySystem.isCW;

                if (bpm * speed * pitch == 0) return;
                double diff = (__instance.angle - __instance.targetExitAngle) * (isCW ? 1.0 : -1.0) * 60000.0 / (Math.PI * bpm * speed * pitch);

                Main.LastTiming = diff;
                UIPatches.UIReplacePatch.dirty = true;

                Main.LastIsXP = CalcXP.IsXPerfect(diff, bpm, speed, pitch);

                bool isAuto = RDC.auto;
                bool canRecord = !isAuto || Main.Settings.LogAutoplay;

                if (Main.IsPlaying && canRecord)
                {
                    bool needRecord = Main.Settings.ShowInWinPage || Main.Settings.ShowURHUD;
                    if (needRecord && Main.SessionOffsets != null)
                    {
                        Main.SessionOffsets.Add(diff);
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
            public static void Prefix(HitMargin hit)
            {
                if (!Main.IsEnabled || !Main.IsPlaying || RDC.auto) return;
                Main.LastHitMargin = hit;
                TimingLogger.LogHit(Main.LastTiming, hit);
            }
        }
    }
}
