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
                ModContext.IsPlaying = true;
                ModContext.IsLevelFinished = false;
                ModContext.LastTiming = 0;
                ModContext.LastHitMargin = HitMargin.Perfect;
                ModContext.SessionOffsets.Clear();
                CalcUR.Reset();
                ModContext.FullXAccHistory.Clear();
                ModContext.XAccVersion++;
                ModContext.UIDirty = true;
                MarginTrackerAddHitPatch.ResetCounts();

                bool isAuto = RDC.auto;
                bool shouldLogAuto = isAuto && ModContext.Settings.LogAutoplay;
                bool shouldLogPlayer = !isAuto;

                if (!ModContext.IsEnabled || scrController.instance == null || !ModContext.Settings.EnableLogging || (!shouldLogAuto && !shouldLogPlayer))
                {
                    TimingLogger.CloseSession();
                    return;
                }

                try
                {
                    if (scnGame.instance == null || scnGame.instance.levelData == null) return;
                    if (ModContext.SessionOffsets != null) ModContext.SessionOffsets.Clear();

                    TimingLogger.StartNewSession(scnGame.instance.levelPath, scnGame.instance.levelData.songFilename, ModContext.Settings.LogDirectory, ModContext.Settings.LogBufferSizeKB);
                }
                catch (Exception e)
                {
                    ModContext.Logger.Error($"Failed to start timing session: {e.Message}");
                }
            }
        }

        // quit (editor)
        [HarmonyPatch(typeof(scnEditor), "SwitchToEditMode")]
        public static class scnEditor_SwitchToEditModePatch
        {
            public static void Prefix()
            {
                ModContext.IsPlaying = false;
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
                ModContext.IsPlaying = false;
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
                ModContext.IsLevelFinished = true;
                ModContext.XAccVersion++;
                ModContext.UIDirty = true;
            }
        }

        // land
        [HarmonyPatch(typeof(scrController), "OnLandOnPortal")]
        public static class OnLandOnPortalPatch
        {
            public static void Postfix()
            {
                ModContext.IsLevelFinished = true;
                ModContext.XAccVersion++;
                ModContext.UIDirty = true;
            }
        }
    }
}
