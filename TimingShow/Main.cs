using HarmonyLib;
using System.Collections.Generic;
using System.Threading;
using UnityModManagerNet;

namespace TimingShow
{
    public static class Main
    {
        public static UnityModManager.ModEntry.ModLogger Logger;
        public static bool IsEnabled;
        public static Settings Settings;
        public static double LastTiming = 0;
        public static bool LastIsXP;
        public static HitMargin LastHitMargin = HitMargin.Perfect;
        public static List<double> SessionOffsets = new List<double>();

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            Logger = modEntry.Logger;
            Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);

            LangMan.LoadLanguages(modEntry.Path);
            XPerfectBridge.TryInit();

            var harmony = new Harmony(modEntry.Info.Id);
            modEntry.OnToggle = (entry, value) => {
                IsEnabled = value;
                if (value) harmony.PatchAll();
                else
                {
                    harmony.UnpatchAll(modEntry.Info.Id);
                    SessionOffsets.Clear();
                    LastTiming = 0;
                    HUDMan.Destroy();
                }
                return true;
            };

            modEntry.OnGUI = Options.OnGUI;
            modEntry.OnSaveGUI = (entry) => Settings.Save(entry);

            return true;
        }

        public static bool IsPlaying() => scrController.instance != null && scrController.instance.state == States.PlayerControl;
        public static string Format(double val, int precision) => $"{val.ToString("F" + precision)}ms";
    }
}