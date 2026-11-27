using System;
using System.IO;
using HarmonyLib;
using UnityModManagerNet;
using TimingShow.Bootstrap;
using TimingShow.Bridge;
using TimingShow.Update;

namespace TimingShow.UMM
{
    internal static class ModBootstrap
    {
        private static string _displayName = "TimingShow";

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModEntry.Initialize(modEntry.Path, message => modEntry.Logger.Log(message), message => modEntry.Logger.Error(message));
            ModContext.Settings = Settings.Load(modEntry.Path);

            i18n.LoadLanguages(ModContext.ModPath);
            ModEntry.InitializeJudgeCompat();
            XPerfectBridge.TryInit();

            var harmony = new Harmony(modEntry.Info.Id);
            ModContext.HarmonyInstance = harmony;

            _displayName = modEntry.Info.DisplayName;
            UpdateBridge.Changed += () => ApplyDisplayName(modEntry);
            UpdateBridge.Initialize(modEntry.Path, Path.Combine(modEntry.Path, PendingUpdateApplier.UpdateDirectoryName), modEntry.Info.Repository, modEntry.Info.Version, UpdateTarget.Umm, true, ModContext.Logger);

            Attach(modEntry);
            return true;
        }

        internal static void Attach(UnityModManager.ModEntry modEntry)
        {
            modEntry.OnToggle = (entry, value) =>
            {
                if (value) ModEntry.Enable();
                else ModEntry.Disable();
                return true;
            };
            modEntry.OnGUI = (entry) =>
            {
                ApplyDisplayName(entry);
                try
                {
                    ModEntry.OnGUI();
                }
                catch (Exception e)
                {
                    CoreCrashWatcher.ReportException(e);
                    throw;
                }
            };
            modEntry.OnSaveGUI = (entry) => ModEntry.SaveSettings();
            CoreCrashWatcher.Install(modEntry);
        }

        private static void ApplyDisplayName(UnityModManager.ModEntry modEntry)
        {
            try
            {
                string suffix = UpdateBridge.Suffix;
                string name = string.IsNullOrEmpty(suffix) ? _displayName : _displayName + " " + suffix;
                modEntry.Info.DisplayName = name; 
            }
            catch (Exception e)
            {
                ModContext.Logger?.Error("Failed to update mod display name: " + e.Message);
            }
        }
    }
}
