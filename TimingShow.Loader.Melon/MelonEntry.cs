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
        private MissingCoreFallback _missingCore;

        public override void OnInitializeMelon()
        {
            _modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            StartupIntegrityReport integrity = StartupIntegrity.Check(_modPath, MelonEnvironment.PluginsDirectory, true, StartupIntegrity.ResolveLoaderFileName(_modPath, "TimingShow.Loader.Melon.dll"), Path.Combine(MelonEnvironment.UserDataDirectory, "TimingShow", "Settings.json"));
            if (integrity.NeedsRepair)
            {
                BeginMissingCoreDownload("TimingShow needs a repair (" + integrity.Summary + "); starting the repair downloader.", integrity);
                return;
            }

            if (!File.Exists(Path.Combine(_modPath, MissingCoreInstaller.CoreFileName)))
            {
                BeginMissingCoreDownload("TimingShow.dll is missing; starting the missing-core downloader.", null);
                return;
            }

            try
            {
                InitializeCore(_modPath);
            }
            catch (TypeLoadException e)
            {
                BeginMissingCoreDownload("The installed TimingShow core is incompatible with this loader; starting the missing-core downloader. " + e.Message, null);
            }
            catch (BadImageFormatException e)
            {
                BeginMissingCoreDownload("The installed TimingShow core is corrupted; starting the missing-core downloader. " + e.Message, null);
            }
        }

        private void BeginMissingCoreDownload(string message, StartupIntegrityReport integrity)
        {
            MelonLogger.Warning("[TimingShow] " + message);
            _missingCore = new MissingCoreFallback(_modPath, MelonEnvironment.UserDataDirectory, MelonEnvironment.PluginsDirectory, integrity);
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
            if (_missingCore != null)
            {
                _missingCore.Tick();
                if (_missingCore.ShouldLoadCore && !_coreLoadAttempted)
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
            string corePath = Path.Combine(_modPath, MissingCoreInstaller.CoreFileName);
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
                _missingCore.Dispose();
                _missingCore = null;
                MelonLogger.Msg("[TimingShow] Loaded after the core download.");
            }
            catch (Exception e)
            {
                if (resolve != null) AppDomain.CurrentDomain.AssemblyResolve -= resolve;
                MelonLogger.Error("[TimingShow] Failed to load the downloaded core: " + e);
                _missingCore?.MarkLoadFailed(e.Message);
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
            if (_missingCore != null) return;

            if (!_coreReady) return;
            DrawCoreSettings();
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
