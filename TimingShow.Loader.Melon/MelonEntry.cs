using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using MelonLoader;
using MelonLoader.Utils;
using TimingShow.Bootstrap;
using TimingShow.Bridge;
using UnityEngine;

namespace TimingShow.Melon
{
    public partial class MelonEntry : MelonMod
    {
        private string _modPath;
        private bool _coreReady;
        private bool _coreLoadAttempted;
        private bool _crashWatcherInstalled;
        private string _crashDetails;
        private RepairPage _repair;

        public override void OnInitializeMelon()
        {
            _modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            StartupIntegrityReport integrity = StartupIntegrity.Check(_modPath, MelonEnvironment.PluginsDirectory, true, StartupIntegrity.ResolveLoaderFileName(_modPath, "TimingShow.Loader.Melon.dll"), Path.Combine(MelonEnvironment.UserDataDirectory, "TimingShow", "Settings.json"));
            if (integrity.NeedsRepair)
            {
                BeginRepair("TimingShow needs a repair (" + integrity.Summary + "); starting the repair downloader.", integrity);
                return;
            }

            if (!File.Exists(Path.Combine(_modPath, RepairInstaller.CoreFileName)))
            {
                BeginRepair("TimingShow.dll is missing; starting the repair downloader.", null);
                return;
            }

            try
            {
                InitializeCore(_modPath);
                InstallCrashWatcher();
            }
            catch (TypeLoadException e)
            {
                BeginFailedRepair("The installed TimingShow core is incompatible with this loader; repair it manually from the window. " + e.Message, e.ToString(), false);
            }
            catch (BadImageFormatException e)
            {
                BeginFailedRepair("The installed TimingShow core is corrupted; repair it manually from the window. " + e.Message, e.ToString(), false);
            }
            catch (Exception e)
            {
                BeginFailedRepair("TimingShow failed to start; repair it manually from the window. " + e.Message, e.ToString(), false);
            }
        }

        private void BeginRepair(string message, StartupIntegrityReport integrity)
        {
            MelonLogger.Warning("[TimingShow] " + message);
            _repair = new RepairPage(_modPath, MelonEnvironment.UserDataDirectory, MelonEnvironment.PluginsDirectory, integrity, null, false);
        }

        private void BeginFailedRepair(string message, string details, bool runtimeFailure)
        {
            MelonLogger.Warning("[TimingShow] " + message);
            _repair = new RepairPage(_modPath, MelonEnvironment.UserDataDirectory, MelonEnvironment.PluginsDirectory, null, details, runtimeFailure);
        }

        private void InstallCrashWatcher()
        {
            if (_crashWatcherInstalled) return;
            _crashWatcherInstalled = true;
            Application.logMessageReceived += OnGameLog;
        }

        private void ReportCoreException(Exception e)
        {
            if (_crashDetails != null || IsGuardDisabled()) return;
            _crashDetails = "TimingShow reported an error at runtime:\n" + e;
        }

        private void OnGameLog(string condition, string stackTrace, LogType type)
        {
            if (_crashDetails != null) return;
            if (!CoreFailureDetector.IsCoreCrash(condition, stackTrace)) return;
            if (IsGuardDisabled()) return;
            _crashDetails = "TimingShow reported an error at runtime:\n" + condition + "\n" + stackTrace;
        }

        private static bool IsGuardDisabled()
        {
            Settings settings = ModContext.Settings;
            if (settings != null) return !settings.Startup_RuntimeGuard;
            string settingsPath = Path.Combine(MelonEnvironment.UserDataDirectory, "TimingShow", "Settings.json");
            return !StartupIntegrity.IsRuntimeGuardEnabled(settingsPath);
        }

        private void EnterRuntimeFailurePage()
        {
            string details = _crashDetails;
            _crashDetails = null;
            if (_crashWatcherInstalled)
            {
                Application.logMessageReceived -= OnGameLog;
                _crashWatcherInstalled = false;
            }

            _coreReady = false;
            _coreLoadAttempted = false;
            StopCrashedCore();
            BeginFailedRepair("TimingShow stopped after a runtime error; repair it manually from the window.", details, true);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ResumeCore()
        {
            _repair.Dispose();
            _repair = null;
            _coreReady = true;
            _coreLoadAttempted = false;
            InstallCrashWatcher();
            try
            {
                ModEntry.Enable();
            }
            catch (Exception e)
            {
                MelonLogger.Error("[TimingShow] Failed to resume after a runtime error: " + e.Message);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void StopCrashedCore()
        {
            try
            {
                ModEntry.Disable();
            }
            catch (Exception e)
            {
                MelonLogger.Error("[TimingShow] Failed to stop the core after a runtime error: " + e.Message);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void InitializeCore(string modPath)
        {
            ModEntry.Initialize(modPath, MelonLogger.Msg, MelonLogger.Error);
            ModContext.IsMelon = true;

            string dataPath = Path.Combine(MelonEnvironment.UserDataDirectory, "TimingShow");
            Directory.CreateDirectory(dataPath);
            ModContext.DataPath = dataPath;
            MelonCoreSetup.MigrateSettings(modPath, dataPath, message => ModContext.Logger.Log(message), message => ModContext.Logger.Error(message));

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

            MelonCoreSetup.ScheduleStartupHint(firstRun);
            MelonCoreSetup.InitializeUpdater(modPath, Info.Version);
            _coreReady = true;
        }

        public override void OnUpdate()
        {
            if (_crashDetails != null)
            {
                EnterRuntimeFailurePage();
                return;
            }

            if (_repair != null)
            {
                if (_repair.ContinueRequested)
                {
                    ResumeCore();
                    return;
                }

                _repair.Tick();
                if (_repair.ShouldLoadCore && !_coreLoadAttempted)
                {
                    _coreLoadAttempted = true;
                    TryLoadCore();
                }
                return;
            }

            if (!_coreReady) return;
            UpdateCore();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void TryLoadCore()
        {
            string corePath = Path.Combine(_modPath, RepairInstaller.CoreFileName);
            Assembly core = null;
            ResolveEventHandler resolve = null;
            try
            {
                core = Assembly.LoadFrom(corePath);
                string name = core.GetName().Name;
                if (!string.Equals(name, "TimingShow", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Unexpected core assembly " + name + ".");

                resolve = (sender, args) =>
                {
                    try
                    {
                        return string.Equals(new AssemblyName(args.Name).Name, name, StringComparison.OrdinalIgnoreCase) ? core : null;
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                };
                AppDomain.CurrentDomain.AssemblyResolve += resolve;

                InitializeCore(_modPath);
                InstallCrashWatcher();
                _repair.Dispose();
                _repair = null;
                MelonLogger.Msg("[TimingShow] Loaded after the core download.");
            }
            catch (Exception e)
            {
                if (resolve != null) AppDomain.CurrentDomain.AssemblyResolve -= resolve;
                MelonLogger.Error("[TimingShow] Failed to load the downloaded core: " + e);
                _repair?.MarkLoadFailed(e.ToString());
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void UpdateCore()
        {
            // IMGUI key events reach OnGUI before OnUpdate in the same frame, so the key that finishes rebinding must not toggle the window.
            if (_isRebinding || _suppressHotkeyFrame == Time.frameCount) return;
            var key = ModContext.Settings.ConfigKey;
            if (key == KeyCode.None) key = KeyCode.F9;
            if (!Input.GetKeyDown(key)) return;
            if (_showSettings) CloseSettings();
            else OpenSettings();
        }

        public override void OnGUI()
        {
            if (_repair != null)
            {
                _repair.OnGUI();
                return;
            }

            if (!_coreReady) return;
            try
            {
                DrawCoreSettings();
            }
            catch (Exception e)
            {
                ReportCoreException(e);
                throw;
            }
        }

        public override void OnApplicationQuit()
        {
            if (!_coreReady) return;
            QuitCore();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void QuitCore()
        {
            ModEntry.SaveSettings();
            ModEntry.Disable();
        }
    }
}
