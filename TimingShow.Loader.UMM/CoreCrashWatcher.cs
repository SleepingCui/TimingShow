using System;
using System.IO;
using System.Runtime.CompilerServices;
using TimingShow.Bootstrap;
using UnityModManagerNet;
using UnityEngine;

namespace TimingShow.UMM
{
    internal static class CoreCrashWatcher
    {
        private static UnityModManager.ModEntry _modEntry;
        private static string _settingsPath;
        private static string _details;

        public static void Install(UnityModManager.ModEntry modEntry)
        {
            if (_modEntry != null) return;
            _modEntry = modEntry;
            _settingsPath = Path.Combine(modEntry.Path, "Settings.json");
            Application.logMessageReceived += OnGameLog;
            modEntry.OnUpdate = (entry, delta) => Tick();
        }

        public static void ReportException(Exception e)
        {
            if (_details != null || IsGuardDisabled()) return;
            _details = "TimingShow reported an error at runtime:\n" + e;
        }

        private static bool IsGuardDisabled()
        {
            TimingShow.Settings settings = TimingShow.ModContext.Settings;
            if (settings != null) return !settings.Startup_RuntimeGuard;
            return !StartupIntegrity.IsRuntimeGuardEnabled(_settingsPath);
        }

        private static void OnGameLog(string condition, string stackTrace, LogType type)
        {
            if (_details != null) return;
            if (!CoreFailureDetector.IsCoreCrash(condition, stackTrace)) return;
            if (IsGuardDisabled()) return;
            _details = "TimingShow reported an error at runtime:\n" + condition + "\n" + stackTrace;
        }

        private static void Tick()
        {
            if (_details == null) return;
            UnityModManager.ModEntry modEntry = _modEntry;
            string details = _details;
            _details = null;
            _modEntry = null;
            if (modEntry == null) return;

            Application.logMessageReceived -= OnGameLog;
            modEntry.Logger.Error("[Repair] TimingShow stopped after a runtime error; entering the repair page.");
            StopCore(modEntry);
            RepairPage.StartAfterRuntimeFailure(modEntry, details);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void StopCore(UnityModManager.ModEntry modEntry)
        {
            try
            {
                TimingShow.ModEntry.Disable();
            }
            catch (Exception e)
            {
                modEntry.Logger.Error("[Repair] Failed to stop the TimingShow core after a runtime error: " + e.Message);
            }
        }
    }
}
