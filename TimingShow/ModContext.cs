using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using TimingShow.HUD;
using OptionsPage = TimingShow.Options.Options;

namespace TimingShow
{

    public struct TimingScatterSample
    {
        public float TimeMs;
        public float OffsetMs;
        public HitMan Judge;
        public bool IsXPerfect;
        public bool HasJudge;
        public int PlayerId;
    }

    public static class ModContext
    {
        public static IModLogger Logger;
        public static string ModPath;
        public static Harmony HarmonyInstance;
        public static bool IsEnabled;
        public static Settings Settings;

        public static double LastTiming
        {
            get { return Players.Current.LastTiming; }
            set { Players.Current.LastTiming = value; }
        }

        public static double LastAngle
        {
            get { return Players.Current.LastAngle; }
            set { Players.Current.LastAngle = value; }
        }

        public static double LastSongTimeMs
        {
            get { return Players.Current.LastSongTimeMs; }
            set { Players.Current.LastSongTimeMs = value; }
        }

        public static HitMan LastJudge
        {
            get { return Players.Current.LastJudge; }
            set { Players.Current.LastJudge = value; }
        }

        public static int LastRawMargin
        {
            get { return Players.Current.LastRawMargin; }
            set { Players.Current.LastRawMargin = value; }
        }

        public static bool LastIsXP
        {
            get { return Players.Current.LastIsXP; }
            set { Players.Current.LastIsXP = value; }
        }

        public static double LastBpm
        {
            get { return Players.Current.LastBpm; }
            set { Players.Current.LastBpm = value; }
        }

        public static double LastSpeed
        {
            get { return Players.Current.LastSpeed; }
            set { Players.Current.LastSpeed = value; }
        }

        public static double LastPitch
        {
            get { return Players.Current.LastPitch; }
            set { Players.Current.LastPitch = value; }
        }

        public static readonly List<float>[] FullXAccHistory = CreateXAccHistories();

        private static List<float>[] CreateXAccHistories()
        {
            List<float>[] all = new List<float>[Players.MaxPlayers];
            for (int i = 0; i < all.Length; i++)
                all[i] = new List<float>();
            return all;
        }

        public static void ClearXAccHistory()
        {
            for (int i = 0; i < FullXAccHistory.Length; i++)
                FullXAccHistory[i].Clear();
        }

        public static bool IsLevelFinished = false;
        public static bool IsPlaying { get; set; } = false;
        
        public static bool UIDirty = true;
        public static int XAccVersion;
        
        public static readonly List<TimingScatterSample> TimingScatterSamples = new List<TimingScatterSample>(1024);
        
        public static int TimingScatterVersion;
        
        public static void ClearTimingScatterSamples()
        {
            TimingScatterSamples.Clear();
            Patches.TimingCalcPatches.InvalidatePendingScatterSample();
            TimingScatterVersion++;
        }
        
        public static int LastConfigGuiFrame = -1;
        public static bool IsConfigOpen => LastConfigGuiFrame >= 0 && Time.frameCount - LastConfigGuiFrame <= 1;

        public static void Initialize(string modPath, IModLogger logger)
        {
            ModPath = modPath;
            Logger = logger;
            Patches.PlayStatePatches.RegisterSceneHook();
        }
        
        public static void InitializeJudgeCompat()
        {
            HitMarginCompat.Initialize();
            Logger.Log($"Detected version: {HitMarginCompat.Version}");
            if (HitMarginCompat.Version == HitMarginVersion.Unknown)
                Logger.Error("Unrecognized HitMargin enum version");
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
            Players.ResetAll();
            ClearTimingScatterSamples();
            HUDMan.Destroy();
        }

        public static void OnGUI()
        {
            OptionsPage.OnGUI();
        }

        public static void SaveSettings()
        {
            if (Settings == null)
            {
                Logger.Error("SaveSettings: Settings is NULL!!!");
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
