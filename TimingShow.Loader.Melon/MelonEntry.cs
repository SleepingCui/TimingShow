using MelonLoader;
using MelonLoader.Utils;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using TimingShow.Bridge;

namespace TimingShow.Melon
{
    public partial class MelonEntry : MelonMod
    {
        public override void OnInitializeMelon()
        {
            string modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var logger = new MelonLoggerAdapter();
            ModEntry.Initialize(modPath, logger);
            ModContext.IsMelon = true;

            string dataPath = Path.Combine(MelonEnvironment.UserDataDirectory, "TimingShow");
            Directory.CreateDirectory(dataPath);
            ModContext.DataPath = dataPath;
            MigrateSettings(modPath, dataPath, logger);

            bool firstRun = !File.Exists(Path.Combine(dataPath, "Settings.json"));
            ModContext.Settings = Settings.Load(dataPath);
            if (ModContext.Settings != null && Settings.IsLegacyLogDirectory(ModContext.Settings.LogDirectory))
                ModContext.Settings.LogDirectory = Settings.DefaultLogDirectory();

            i18n.LoadLanguages(modPath);
            MelonHotkeys.Initialize(ModContext.Settings);
            ModEntry.InitializeJudgeCompat();
            XPerfectBridge.TryInit();
            var harmony = new HarmonyLib.Harmony("TimingShow.Loader.Melon");
            ModContext.HarmonyInstance = harmony;
            ModEntry.Enable();
            
            ScheduleStartupHint(firstRun);
            InitializeUpdater(modPath, Info.Version, logger);
        }

        private static void MigrateSettings(string modPath, string dataPath, IModLogger logger)
        {
            string legacy = Path.Combine(modPath, "Settings.json");
            string target = Path.Combine(dataPath, "Settings.json");
            if (File.Exists(target) || !File.Exists(legacy)) return;

            try
            {
                File.Move(legacy, target);
                logger.Log("Moved Settings.json to " + target);
            }
            catch (Exception e)
            {
                logger.Error("Failed to move Settings.json to " + target + ": " + e.Message);
            }
        }

        private static void ScheduleStartupHint(bool firstRun)
        {
            Settings settings = ModContext.Settings;
            if (settings == null) return;
            if (!firstRun && !settings.ShowConfigHintOnStart) return;

            KeyCode key = settings.ConfigKey == KeyCode.None ? KeyCode.F9 : settings.ConfigKey;
            HUD.StartupHint.Schedule(string.Format(i18n.T("Hint_ConfigMenu"), key));
        }

        public override void OnUpdate()
        {
            // IMGUI key events reach OnGUI before OnUpdate in the same frame, so the key that finishes rebinding must not toggle the window.
            if (_isRebinding || _suppressHotkeyFrame == Time.frameCount) return;
            var key = ModContext.Settings.ConfigKey;
            if (key == KeyCode.None) key = KeyCode.F9;
            if (!Input.GetKeyDown(key)) return;
            if (_showSettings) CloseSettings();
            else OpenSettings();
        }

        public override void OnApplicationQuit()
        {
            ModEntry.SaveSettings();
            ModEntry.Disable();
        }
    }
}
