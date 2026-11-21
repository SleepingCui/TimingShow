using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using TimingShow.Bootstrap;
using UnityModManagerNet;
using UnityEngine;

namespace TimingShow.UMM
{
    internal sealed class MissingCoreFallback
    {
        private const string CoreAssemblyName = "TimingShow";

        private readonly UnityModManager.ModEntry _modEntry;
        private readonly MissingCoreInstaller _installer;
        private readonly string _baseDisplayName;
        private bool _loadAttempted;
        private bool _loaded;
        private bool _restartRequired;
        private string _failureMessage;

        private MissingCoreFallback(UnityModManager.ModEntry modEntry)
        {
            _modEntry = modEntry;
            _baseDisplayName = string.IsNullOrEmpty(modEntry.Info.DisplayName) ? CoreAssemblyName : modEntry.Info.DisplayName;
            string repository = string.IsNullOrEmpty(modEntry.Info.Repository) ? MissingCoreInstaller.DefaultRepositoryUrl : modEntry.Info.Repository;
            _installer = new MissingCoreInstaller(
                modEntry.Path,
                null,
                false,
                "TimingShow",
                repository,
                Path.Combine(modEntry.Path, "Update"),
                "TimingShow.Loader.UMM.dll",
                message => modEntry.Logger.Log("[MissingCore] " + message),
                message => modEntry.Logger.Error("[MissingCore] " + message));
        }

        public static bool Start(UnityModManager.ModEntry modEntry)
        {
            MissingCoreFallback fallback = new MissingCoreFallback(modEntry);
            fallback.Begin();
            return true;
        }

        private void Begin()
        {
            _modEntry.Info.DisplayName = ColoredDisplayName() + " " + ProgressSuffix();
            _modEntry.OnToggle = (entry, value) => true;
            _modEntry.OnGUI = entry => Draw();
            _modEntry.OnSaveGUI = entry => { };
            _modEntry.OnUpdate = (entry, delta) => Tick();
            _installer.Start();
        }

        private void Tick()
        {
            if (_loaded || _restartRequired) return;

            MissingCoreState state = _installer.State;
            if (state == MissingCoreState.ReadyToLoad && !_loadAttempted)
            {
                _loadAttempted = true;
                TryLoadCore();
            }

            UpdateDisplayName(state);
        }

        private void UpdateDisplayName(MissingCoreState state)
        {
            string name = ColoredDisplayName() + SuffixFor(state);
            if (!string.Equals(_modEntry.Info.DisplayName, name, StringComparison.Ordinal)) _modEntry.Info.DisplayName = name;
        }

        private string SuffixFor(MissingCoreState state)
        {
            if (_restartRequired) return " [Restart required]";

            switch (state)
            {
                case MissingCoreState.Checking:
                case MissingCoreState.Downloading:
                case MissingCoreState.Verifying:
                case MissingCoreState.Installing:
                    return " " + ProgressSuffix();
                case MissingCoreState.RestartRequired:
                    return " [Restart required]";
                case MissingCoreState.Failed:
                    return " [Download failed]";
                case MissingCoreState.Cancelled:
                    return " [Download cancelled]";
                default:
                    return string.Empty;
            }
        }

        private string ColoredDisplayName()
        {
            return "<color=#FF5555>" + _baseDisplayName + "</color>";
        }

        private string ProgressSuffix()
        {
            return "[Downloading... " + FormatPercent() + "]";
        }

        private string FormatPercent()
        {
            return (_installer.Progress * 100f).ToString("0.0", CultureInfo.InvariantCulture) + "%";
        }

        private void TryLoadCore()
        {
            string corePath = Path.Combine(_modEntry.Path, MissingCoreInstaller.CoreFileName);
            try
            {
                Assembly core = Assembly.LoadFrom(corePath);
                string name = core.GetName().Name;
                if (!string.Equals(name, CoreAssemblyName, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Unexpected core assembly " + name + ".");

                ResolveEventHandler resolve = (sender, args) =>
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

                _modEntry.Info.DisplayName = _baseDisplayName;
                if (!UMMEntry.StartCore(_modEntry))
                {
                    AppDomain.CurrentDomain.AssemblyResolve -= resolve;
                    throw new InvalidOperationException("TimingShow could not be initialized.");
                }

                _loaded = true;
                _modEntry.Logger.Log("[MissingCore] TimingShow was loaded after the core download.");
            }
            catch (Exception e)
            {
                _restartRequired = true;
                _failureMessage = e.Message;
                UpdateDisplayName(_installer.State);
                _modEntry.Logger.Error("[MissingCore] Failed to start the downloaded core: " + e);
            }
        }

        private static bool IsRunning(MissingCoreState state)
        {
            return state == MissingCoreState.Checking || state == MissingCoreState.Downloading || state == MissingCoreState.Verifying || state == MissingCoreState.Installing;
        }

        private void Draw()
        {
            if (_loaded) return;

            MissingCoreState state = _installer.State;
            bool failed = state == MissingCoreState.Failed || state == MissingCoreState.Cancelled;
            bool running = IsRunning(state);
            if (!failed && !running && !_restartRequired) return;

            GUILayout.BeginVertical(GUI.skin.box);
            if (_restartRequired)
            {
                GUILayout.Label("Core downloaded, but it could not start: " + _failureMessage);
            }
            else if (state == MissingCoreState.Failed)
            {
                GUILayout.Label("Download failed: " + _installer.ErrorMessage);
                if (GUILayout.Button("Retry", GUILayout.Width(120f))) _installer.Start();
            }
            else if (running)
            {
                if (GUILayout.Button("Cancel", GUILayout.Width(120f))) _installer.Cancel();
            }
            else
            {
                GUILayout.Label("Download cancelled.");
                if (GUILayout.Button("Retry", GUILayout.Width(120f))) _installer.Start();
            }
            GUILayout.EndVertical();
        }
    }
}
