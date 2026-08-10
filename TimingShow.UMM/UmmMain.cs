using HarmonyLib;
using UnityModManagerNet;

namespace TimingShow
{
    public static class Main
    {
        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            var logger = new UmmLogger(modEntry.Logger);
            ModContext.Initialize(modEntry.Path, logger);
            ModContext.Settings = Settings.Load(modEntry.Path);

            LangMan.LoadLanguages(ModContext.ModPath);
            XPerfectBridge.TryInit();

            var harmony = new Harmony(modEntry.Info.Id);
            ModContext.HarmonyInstance = harmony;

            modEntry.OnToggle = (entry, value) =>
            {
                if (value)
                    ModContext.Enable();
                else
                    ModContext.Disable();
                return true;
            };
            modEntry.OnGUI = (entry) => ModContext.OnGUI();
            modEntry.OnSaveGUI = (entry) => ModContext.SaveSettings();

            return true;
        }
    }
}
