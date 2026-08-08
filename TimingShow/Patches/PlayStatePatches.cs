using HarmonyLib;
using System;
using static TimingShow.Patches.TimingCalcPatches;

namespace TimingShow.Patches
{
    public static class PlayStatePatches
    {
        // start playing
        [HarmonyPatch(typeof(scrController), "Start_Rewind")]
        public static class LevelStartPatch
        {
            public static void Postfix()
            {
                Main.IsPlaying = true;
                Main.IsLevelFinished = false;
                Main.LastTiming = 0;
                Main.LastHitMargin = HitMargin.Perfect;
                Main.SessionOffsets.Clear();
                Main.FullXAccHistory.Clear();
                MarginTrackerAddHitPatch.ResetCounts();

                bool isAuto = RDC.auto;
                bool shouldLogAuto = isAuto && Main.Settings.LogAutoplay;
                bool shouldLogPlayer = !isAuto;

                if (!Main.IsEnabled || scrController.instance == null || !Main.Settings.EnableLogging || (!shouldLogAuto && !shouldLogPlayer))
                {
                    TimingLogger.CloseSession();
                    return;
                }

                try
                {
                    if (scnGame.instance == null || scnGame.instance.levelData == null) return;
                    if (Main.SessionOffsets != null) Main.SessionOffsets.Clear();

                    TimingLogger.StartNewSession(scnGame.instance.levelPath, scnGame.instance.levelData.songFilename, Main.Settings.LogDirectory, Main.Settings.LogBufferSizeKB);
                }
                catch (Exception e)
                {
                    Main.Logger.Error($"Failed to start timing session: {e.Message}");
                }
            }
        }

        // quit (editor)
        [HarmonyPatch(typeof(scnEditor), "SwitchToEditMode")]
        public static class scnEditor_SwitchToEditModePatch
        {
            public static void Prefix()
            {
                Main.IsPlaying = false;
                MarginTrackerAddHitPatch.ResetCounts(); 
                HUDMan.Destroy();
                TimingLogger.CloseSession();
            }
        }

        // quit (general)
        [HarmonyPatch(typeof(scrController), "QuitToMainMenu")]
        public static class QuitToMainMenu_Patch
        {
            public static void Prefix()
            {
                Main.IsPlaying = false;
                MarginTrackerAddHitPatch.ResetCounts(); 
                TimingLogger.CloseSession();
                HUDMan.Destroy();
            }
        }

        // ckpoint
        [HarmonyPatch(typeof(scrMarginTracker), "RevertToLastCheckpoint")]
        public static class MarginTrackerRevertPatch
        {
            public static void Postfix(scrMarginTracker __instance)
            {
                MarginTrackerAddHitPatch.SyncFromTracker(__instance);
            }
        }

        // fail
        [HarmonyPatch(typeof(scrController), "Fail2Action")]
        public static class FailPatch
        {
            public static void Postfix()
            {
                Main.IsLevelFinished = true;
            }
        }

        // land
        [HarmonyPatch(typeof(scrController), "OnLandOnPortal")]
        public static class OnLandOnPortalPatch
        {
            public static void Postfix()
            {
                Main.IsLevelFinished = true;
            }
        }
    }
}