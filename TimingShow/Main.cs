using System;
using TimingShow.HUD;
using OptionsPage = TimingShow.Options.Options;

namespace TimingShow
{
    public static class ModEntry
    {
        public static void Initialize(string modPath, IModLogger logger)
        {
            ModContext.ModPath = modPath;
            ModContext.DataPath = modPath;
            ModContext.Logger = logger;
            Patches.PlayStatePatches.RegisterSceneHook();
        }

        public static void InitializeJudgeCompat()
        {
            HitMarginCompat.Initialize();
            ModContext.Logger.Log($"Detected version: {HitMarginCompat.Version}");
            if (HitMarginCompat.Version == HitMarginVersion.Unknown)
                ModContext.Logger.Error("Unrecognized HitMargin enum version");
        }

        public static void Enable()
        {
            ModContext.IsEnabled = true;
            try
            {
                ModContext.HarmonyInstance?.PatchAll();
            }
            catch (Exception e)
            {
                ModContext.Logger.Error($"PatchAll failed: {e.GetType().Name}: {e.Message}");
            }
            if (ModContext.Settings == null || !ModContext.Settings.DisableInputHooks)
                Patches.InputPatches.Install(ModContext.HarmonyInstance);
            SessionReport.SessionReportHotkey.EnsureHost();
            LevelEditor.EditorHitMarkerController.EnsureHost();
        }

        public static void Disable()
        {
            ModContext.IsEnabled = false;
            Patches.InputPatches.Uninstall(ModContext.HarmonyInstance);
            ModContext.HarmonyInstance?.UnpatchAll(ModContext.HarmonyInstance.Id);
            Players.ResetAll();
            ModContext.ClearTimingScatterSamples();
            HUDMan.Destroy();
            LevelEditor.EditorHitSnapshot.Clear();
            SessionReport.SessionReportHotkey.DestroyHost();
            SessionReport.LastSessionReport.Clear();
        }

        public static void OnGUI()
        {
            OptionsPage.OnGUI();
        }

        public static void SaveSettings()
        {
            if (ModContext.Settings == null)
            {
                ModContext.Logger.Error("SaveSettings: Settings is NULL!!!");
                return;
            }
            ModContext.Settings.Save(ModContext.ModPath);
        }
    }
}
