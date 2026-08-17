using HarmonyLib;
using System.Collections.Generic;

namespace TimingShow
{
    public static class ModContext
    {
        public static IModLogger Logger;
        public static string ModPath;
        public static Harmony HarmonyInstance;
        public static bool IsEnabled;
        public static Settings Settings;
        public static double LastTiming;
        public static double LastAngle;
        public static bool LastIsXP;
        public static HitMargin LastHitMargin = HitMargin.Perfect;
        public static List<double> SessionOffsets = new List<double>();
        public static List<float> FullXAccHistory = new List<float>();
        public static bool IsLevelFinished = false;
        public static bool IsPlaying { get; set; } = false;
        
        public static bool UIDirty = true;
        public static int XAccVersion;

        public static void Initialize(string modPath, IModLogger logger)
        {
            ModPath = modPath;
            Logger = logger;
        }

        public static void Enable()
        {
            IsEnabled = true;
            HarmonyInstance?.PatchAll();
        }

        public static void Disable()
        {
            IsEnabled = false;
            HarmonyInstance?.UnpatchAll(HarmonyInstance.Id);
            SessionOffsets.Clear();
            LastTiming = 0;
            LastAngle = 0;
            HUDMan.Destroy();
        }

        public static void OnGUI()
        {
            Options.OnGUI();
        }

        public static void SaveSettings()
        {
            if (Settings == null)
            {
                Logger?.Error("SaveSettings: Settings is NULL!!!");
                return;
            }
            Settings.Save(ModPath);
        }

        public static string Format(double val, int precision)
        {
            return $"{val.ToString("F" + precision)}ms";
        }

        public static string FormatAngle(double val, int precision)
        {
            return $"{val.ToString("F" + precision)}°";
        }
    }
}
