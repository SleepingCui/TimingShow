using HarmonyLib;
using System;

namespace TimingShow
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
                Main.LastTiming = 0;
                Main.LastHitMargin = HitMargin.Perfect;
                Main.SessionOffsets?.Clear();

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

        // editor
        [HarmonyPatch(typeof(scnEditor), "SwitchToEditMode")]
        public static class scnEditor_SwitchToEditMode_Patch
        {
            public static void Prefix()
            {
                Main.IsPlaying = false;
                HUDMan.Destroy();
                TimingLogger.CloseSession();
            }
        }

        // general
        [HarmonyPatch(typeof(scrController), "QuitToMainMenu")]
        public static class QuitToMainMenu_Patch
        {
            public static void Prefix()
            {
                Main.IsPlaying = false;
                TimingLogger.CloseSession();
                HUDMan.Destroy();
            }
        }
    }
}