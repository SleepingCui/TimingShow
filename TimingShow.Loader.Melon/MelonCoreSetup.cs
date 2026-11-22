using System;
using System.IO;
using MelonLoader.Utils;
using TimingShow.Bootstrap;
using TimingShow.Update;
using UnityEngine;

namespace TimingShow.Melon
{
    internal static class MelonCoreSetup
    {
        private const string UpdateDirectoryName = "Update";

        public static void MigrateSettings(string modPath, string dataPath, Action<string> log, Action<string> error)
        {
            string legacy = Path.Combine(modPath, "Settings.json");
            string target = Path.Combine(dataPath, "Settings.json");
            if (File.Exists(target) || !File.Exists(legacy)) return;

            try
            {
                File.Move(legacy, target);
                log("Moved Settings.json to " + target);
            }
            catch (Exception e)
            {
                error("Failed to move Settings.json to " + target + ": " + e.Message);
            }
        }

        public static void ScheduleStartupHint(bool firstRun)
        {
            Settings settings = ModContext.Settings;
            if (settings == null) return;
            if (!firstRun && !settings.ShowConfigHintOnStart) return;

            KeyCode key = settings.ConfigKey == KeyCode.None ? KeyCode.F9 : settings.ConfigKey;
            HUD.StartupHint.Schedule(string.Format(i18n.T("Hint_ConfigMenu"), key));
        }

        public static void InitializeUpdater(string modPath, string version)
        {
            string updateRoot = Path.Combine(MelonEnvironment.UserDataDirectory, "TimingShow", UpdateDirectoryName);
            string bootstrapPath = Path.Combine(MelonEnvironment.PluginsDirectory, "TimingShow.UpdateBootstrap.dll");
            bool bootstrapInstalled = File.Exists(bootstrapPath);
            if (!bootstrapInstalled) ModContext.Logger.Error("Update bootstrap missing from " + MelonEnvironment.PluginsDirectory + ". Automatic updates disabled.");

            UpdateBridge.Initialize(modPath, updateRoot, UpdateBridge.DefaultRepositoryUrl, version, UpdateTarget.Melon, bootstrapInstalled, ModContext.Logger);
        }
    }
}
