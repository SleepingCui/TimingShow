using System;
using System.IO;
using HarmonyLib;
using UnityModManagerNet;
using TimingShow.Bridge;
using TimingShow.Update;

namespace TimingShow.UMM
{
    internal static class CoreStart
    {
        private static string _displayName = "TimingShow";

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            var logger = new UmmLogger(modEntry.Logger);
            ModContext.Initialize(modEntry.Path, logger);
            ModContext.Settings = Settings.Load(modEntry.Path);

            i18n.LoadLanguages(ModContext.ModPath);
            ModContext.InitializeJudgeCompat();
            XPerfectBridge.TryInit();

            var harmony = new Harmony(modEntry.Info.Id);
            ModContext.HarmonyInstance = harmony;

            _displayName = modEntry.Info.DisplayName;
            Updater.Changed += () => ApplyDisplayName(modEntry);
            Updater.Initialize(modEntry.Path, Path.Combine(modEntry.Path, PendingUpdateApplier.UpdateDirectoryName), modEntry.Info.Repository, modEntry.Info.Version.ToString(), UpdateTarget.Umm, true, logger);

            modEntry.OnToggle = (entry, value) =>
            {
                if (value) ModContext.Enable();
                else ModContext.Disable();
                return true;
            };
            modEntry.OnGUI = (entry) =>
            {
                ApplyDisplayName(entry);
                ModContext.OnGUI();
            };
            modEntry.OnSaveGUI = (entry) => ModContext.SaveSettings();

            return true;
        }

        private static void ApplyDisplayName(UnityModManager.ModEntry modEntry)
        {
            try
            {
                string suffix = Updater.Suffix;
                string name = string.IsNullOrEmpty(suffix) ? _displayName : _displayName + " " + suffix;
                if (modEntry.Info.DisplayName != name) modEntry.Info.DisplayName = name;
            }
            catch (Exception e)
            {
                ModContext.Logger?.Error("Failed to update mod display name: " + e.Message);
            }
        }
    }
}
