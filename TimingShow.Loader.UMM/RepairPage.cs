using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using TimingShow.Bootstrap;
using UnityModManagerNet;
using UnityEngine;

namespace TimingShow.UMM
{
    internal sealed class RepairPage
    {
        private const string CoreAssemblyName = "TimingShow";
        private const string BrokenDisplayNameColor = "#FF5555";

        private readonly UnityModManager.ModEntry _modEntry;
        private readonly RepairInstaller _installer;
        private readonly StartupIntegrityReport _integrity;
        private readonly string _baseDisplayName;
        private bool _loadAttempted;
        private bool _loaded;
        private bool _restartRequired;
        private bool _loadFailed;
        private string _failureDetails;
        private GUIStyle _detailStyle;
        private Vector2 _scroll;

        private RepairPage(UnityModManager.ModEntry modEntry, StartupIntegrityReport integrity, Exception failure)
        {
            _modEntry = modEntry;
            _integrity = integrity;
            _baseDisplayName = string.IsNullOrEmpty(modEntry.Info.DisplayName) ? CoreAssemblyName : modEntry.Info.DisplayName;
            _loadFailed = failure != null;
            _failureDetails = failure == null ? string.Empty : failure.ToString();
            string repository = string.IsNullOrEmpty(modEntry.Info.Repository) ? RepairInstaller.DefaultRepositoryUrl : modEntry.Info.Repository;
            _installer = new RepairInstaller(
                modEntry.Path,
                null,
                false,
                "TimingShow",
                repository,
                Path.Combine(modEntry.Path, "Settings.json"),
                Path.Combine(modEntry.Path, "Update"),
                "TimingShow.Loader.UMM.dll",
                message => modEntry.Logger.Log("[Repair] " + message),
                message => modEntry.Logger.Error("[Repair] " + message));
        }

        public static bool Start(UnityModManager.ModEntry modEntry, StartupIntegrityReport integrity)
        {
            return Create(modEntry, integrity, null);
        }

        public static bool StartAfterLoadFailure(UnityModManager.ModEntry modEntry, StartupIntegrityReport integrity, Exception failure)
        {
            return Create(modEntry, integrity, failure);
        }

        private static bool Create(UnityModManager.ModEntry modEntry, StartupIntegrityReport integrity, Exception failure)
        {
            RepairPage page = new RepairPage(modEntry, integrity, failure);
            page.Begin();
            return true;
        }

        private void Begin()
        {
            if (_integrity != null) _modEntry.Logger.Warning("[Integrity] Repairing TimingShow: " + _integrity.Summary);
            _modEntry.Info.DisplayName = ColoredDisplayName();
            _modEntry.OnToggle = (entry, value) => true;
            _modEntry.OnGUI = entry => Draw();
            _modEntry.OnSaveGUI = entry => { };
            _modEntry.OnUpdate = (entry, delta) => Tick();
            if (!_loadFailed) _installer.Start();
        }

        private void Tick()
        {
            if (_loaded) return;

            RepairState state = _installer.State;
            if (!_loadAttempted && (state == RepairState.ReadyToLoad || CanLoadUsableCore(state)))
            {
                _loadAttempted = true;
                if (state != RepairState.ReadyToLoad) _modEntry.Logger.Warning("[Integrity] Repair incomplete. Using installed files.");
                TryLoadCore();
            }

            UpdateDisplayName(state);
        }

        private bool CanLoadUsableCore(RepairState state)
        {
            return _integrity != null && _integrity.CoreUsable && (state == RepairState.Failed || state == RepairState.Cancelled);
        }

        private bool RestartPending(RepairState state)
        {
            return _restartRequired || state == RepairState.RestartRequired;
        }

        private void UpdateDisplayName(RepairState state)
        {
            string name = DisplayNameFor(state);
            if (!string.Equals(_modEntry.Info.DisplayName, name, StringComparison.Ordinal)) _modEntry.Info.DisplayName = name;
        }

        private string DisplayNameFor(RepairState state)
        {
            string name = ColoredDisplayName();
            if (_installer.IsRunning) return name + " " + ProgressSuffix();
            if (RestartPending(state)) return name + " [Restart required]";
            return name;
        }

        private string ColoredDisplayName()
        {
            return "<color=" + BrokenDisplayNameColor + ">" + _baseDisplayName + "</color>";
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
            string corePath = Path.Combine(_modEntry.Path, RepairInstaller.CoreFileName);
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
                _loadFailed = false;
                _failureDetails = string.Empty;
                _modEntry.Logger.Log("[Repair] TimingShow was loaded after the core download.");
            }
            catch (Exception e)
            {
                _loadFailed = true;
                _restartRequired = true;
                _failureDetails = e.ToString();
                UpdateDisplayName(_installer.State);
                _modEntry.Logger.Error("[Repair] Failed to start the downloaded core: " + e);
            }
        }

        private GUIStyle DetailStyle
        {
            get
            {
                if (_detailStyle == null) _detailStyle = new GUIStyle(GUI.skin.label) { wordWrap = true, richText = false };
                return _detailStyle;
            }
        }

        private void Draw()
        {
            if (_loaded) return;

            RepairState state = _installer.State;
            GUILayout.Label("TimingShow is not running.", DetailStyle);
            GUILayout.Label(StatusText(state), DetailStyle);
            DrawButtons(state);
            DrawDetails();
        }

        private void DrawButtons(RepairState state)
        {
            if (_installer.IsRunning)
            {
                if (GUILayout.Button("Cancel", GUILayout.Width(120f))) _installer.Cancel();
                return;
            }

            if (RestartPending(state)) return;
            if (GUILayout.Button("Repair", GUILayout.Width(120f))) Repair();
        }

        private string StatusText(RepairState state)
        {
            if (RestartPending(state)) return "Restart the game to finish repairing.";
            switch (state)
            {
                case RepairState.Checking:
                    return "Checking the latest release.";
                case RepairState.Downloading:
                    return "Downloading the latest package (" + FormatPercent() + ").";
                case RepairState.Verifying:
                    return "Verifying the downloaded package.";
                case RepairState.Installing:
                    return "Installing the downloaded files.";
                case RepairState.ReadyToLoad:
                    return "Starting the downloaded core.";
                case RepairState.Failed:
                    return "The repair download failed.";
                case RepairState.Cancelled:
                    return "The repair download was cancelled.";
                default:
                    return _loadFailed ? "The installed core could not be started." : "The core is not loaded.";
            }
        }

        private void DrawDetails()
        {
            string details = BuildDetails();
            if (details.Length == 0) return;

            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(180f));
            GUILayout.Label(details, DetailStyle);
            GUILayout.EndScrollView();
        }

        private string BuildDetails()
        {
            StringBuilder builder = new StringBuilder();
            if (!string.IsNullOrEmpty(_installer.Version)) builder.AppendLine("Version: " + _installer.Version);
            if (_integrity != null)
            {
                string check = _integrity.Enabled ? (_integrity.Problems.Count == 0 ? "passed" : "failed") : "disabled";
                builder.AppendLine("File check: " + check);
                foreach (string problem in _integrity.Problems) builder.AppendLine("  " + problem);
            }
            if (!string.IsNullOrEmpty(_installer.ErrorMessage)) builder.AppendLine("Download error: " + _installer.ErrorMessage);
            if (!string.IsNullOrEmpty(_failureDetails)) builder.AppendLine(_failureDetails);
            return builder.ToString().Trim();
        }

        private void Repair()
        {
            _loadAttempted = false;
            _restartRequired = false;
            _loadFailed = false;
            _failureDetails = string.Empty;
            _installer.Start();
        }
    }
}
