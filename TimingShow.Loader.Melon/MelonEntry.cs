using MelonLoader;
using MelonLoader.Utils;
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
            ModContext.DataPath = Path.Combine(MelonEnvironment.UserDataDirectory, "TimingShow");
            bool firstRun = !File.Exists(Path.Combine(modPath, "Settings.json"));
            ModContext.Settings = Settings.Load(modPath);

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
            if (_isRebinding) return;
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
