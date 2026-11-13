using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEngine;

namespace TimingShow.Update
{
    internal enum UpdateState
    {
        Idle,
        Checking,
        UpToDate,
        Available,
        Downloading,
        Verifying,
        Ready,
        Failed
    }

    internal enum UpdateCheckResult
    {
        None,
        UpToDate,
        Available,
        Failed
    }

    internal static class Updater
    {
        public const string RepositoryId = "TimingShow";
        public const string MelonRepositoryId = "TimingShow.Melon";
        public const string UnsupportedReasonKey = "Updater_Unsupported";
        public const string BootstrapMissingReasonKey = "Updater_BootstrapMissing";

        private static readonly object Sync = new object();

        private static string _modPath;
        private static string _updateRoot;
        private static UpdateTarget _target;
        private static string _repositoryUrl = UpdateBridge.DefaultRepositoryUrl;
        private static Version _currentVersion = new Version(0, 0, 0);
        private static GameVersion _gameVersion;
        private static string _gameVersionText = string.Empty;
        private static IModLogger _logger;
        private static UpdateRelease _release;
        private static string _availableVersion = string.Empty;
        private static string _errorMessage = string.Empty;
        private static float _progress;
        private static UpdateState _state = UpdateState.Idle;
        private static UpdateCheckResult _checkResult = UpdateCheckResult.None;
        private static string _checkError = string.Empty;
        private static Thread _worker;
        private static volatile bool _cancelRequested;
        private static volatile bool _dirty;

        public static event Action Changed;

        public static bool CheckEnabled { get; private set; }

        public static bool AutoUpdateEnabled { get; private set; }

        public static bool Supported { get; private set; }

        public static string UnavailableReasonKey { get; private set; } = UnsupportedReasonKey;

        public static UpdateState State
        {
            get { lock (Sync) return _state; }
        }

        public static UpdateCheckResult CheckResult
        {
            get { lock (Sync) return _checkResult; }
        }

        public static string CheckError
        {
            get { lock (Sync) return _checkError; }
        }

        public static string AvailableVersion
        {
            get { lock (Sync) return _availableVersion; }
        }

        public static string ErrorMessage
        {
            get { lock (Sync) return _errorMessage; }
        }

        public static float Progress
        {
            get { lock (Sync) return _progress; }
        }

        public static string Suffix
        {
            get
            {
                switch (State)
                {
                    case UpdateState.Available:
                        return string.Format(i18n.T("Updater_Suffix_Available"), AvailableVersion);
                    case UpdateState.Downloading:
                    case UpdateState.Verifying:
                        return i18n.T("Updater_Suffix_Downloading");
                    case UpdateState.Ready:
                        return i18n.T("Updater_Suffix_Ready");
                    case UpdateState.Failed:
                        return i18n.T("Updater_Suffix_Failed");
                    default:
                        return string.Empty;
                }
            }
        }

        public static void Initialize(string modPath, string updateRoot, string repositoryUrl, string currentVersion, UpdateTarget target, bool autoApplyAvailable, IModLogger logger)
        {
            lock (Sync)
            {
                Supported = autoApplyAvailable;
                UnavailableReasonKey = autoApplyAvailable ? UnsupportedReasonKey : BootstrapMissingReasonKey;
                _modPath = modPath;
                _updateRoot = string.IsNullOrEmpty(updateRoot) ? Path.Combine(modPath, PendingUpdateApplier.UpdateDirectoryName) : updateRoot;
                _repositoryUrl = string.IsNullOrEmpty(repositoryUrl) ? UpdateBridge.DefaultRepositoryUrl : repositoryUrl;
                _logger = logger;
                _currentVersion = ParseVersion(currentVersion);
                string gameVersion = Application.version;
                _gameVersionText = string.IsNullOrEmpty(gameVersion) ? "unknown" : gameVersion.Trim();
                GameVersion.TryParse(gameVersion, out _gameVersion);
                _target = target;
            }

            Settings settings = ModContext.Settings;
            CheckEnabled = settings != null && settings.UpdateCheckEnabled;
            AutoUpdateEnabled = settings != null && settings.UpdateAutoUpdate;

            if (_gameVersion.IsEmpty) Log("Game version is unavailable; game version constraints in the repository are ignored.");

            UpdateClient.Trace = Trace;
            Trace("Initialized: version " + _currentVersion + ", game " + _gameVersionText + ", target " + _target + ", repository " + _repositoryUrl + ", mod path " + modPath + ", update root " + _updateRoot + ", check " + CheckEnabled + ", auto update " + AutoUpdateEnabled + ", supported " + Supported + ".");

            if (!Supported)
            {
                Trace("Automatic updates are unavailable: " + UnavailableReasonKey + ".");
                return;
            }

            UpdateHost.EnsureHost();
            if (CheckEnabled) BeginCheck(AutoUpdateEnabled);
        }

        public static void CheckNow()
        {
            if (!Supported) return;
            UpdateState state = State;
            if (state == UpdateState.Checking || state == UpdateState.Downloading || state == UpdateState.Verifying || state == UpdateState.Ready) return;
            BeginCheck(false);
        }

        public static void Tick()
        {
            if (!_dirty) return;
            _dirty = false;

            Action handler = Changed;
            if (handler == null) return;
            try
            {
                handler();
            }
            catch (Exception e)
            {
                Log("Failed to refresh update UI: " + e.Message);
            }
        }

        public static void StartUpdate()
        {
            bool hasRelease;
            UpdateState state;
            lock (Sync)
            {
                hasRelease = _release != null;
                state = _state;
            }

            if (!hasRelease)
            {
                if (state != UpdateState.Checking && state != UpdateState.Downloading && state != UpdateState.Verifying)
                    BeginCheck(AutoUpdateEnabled);
                return;
            }

            if (state == UpdateState.Available || state == UpdateState.Failed) BeginDownload();
        }

        public static void CancelDownload()
        {
            UpdateState state = State;
            if (state != UpdateState.Downloading && state != UpdateState.Verifying) return;
            _cancelRequested = true;
        }

        public static void Dismiss()
        {
            SetState(UpdateState.Idle);
        }

        private static string ReleaseId
        {
            get
            {
                lock (Sync) return _target == UpdateTarget.Melon ? MelonRepositoryId : RepositoryId;
            }
        }

        private static void BeginCheck(bool autoUpdate)
        {
            Thread worker;
            lock (Sync)
            {
                if (_worker != null && _worker.IsAlive && _worker != Thread.CurrentThread) return;
                _cancelRequested = false;
                _release = null;
                _availableVersion = string.Empty;
                _errorMessage = string.Empty;
                _checkResult = UpdateCheckResult.None;
                _checkError = string.Empty;
                _progress = 0f;
                _state = UpdateState.Checking;
                _dirty = true;
                worker = _worker = new Thread(() => CheckWorker(autoUpdate)) { IsBackground = true, Name = "TimingShow.UpdateCheck" };
            }
            worker.Start();
        }

        private static void CheckWorker(bool autoUpdate)
        {
            string releaseId = ReleaseId;
            Trace("Checking " + _repositoryUrl + " for " + releaseId + " releases newer than " + _currentVersion + " for game " + _gameVersionText + ".");
            Stopwatch elapsed = Stopwatch.StartNew();
            try
            {
                string json = UpdateClient.FetchRepository(_repositoryUrl);
                string skippedForGame;
                UpdateRelease release = UpdateClient.FindRelease(json, releaseId, _currentVersion, _gameVersion, out skippedForGame);
                Trace("Check completed in " + elapsed.ElapsedMilliseconds + " ms.");
                if (release == null)
                {
                    if (string.IsNullOrEmpty(skippedForGame))
                        Log("Mod is up to date (" + _currentVersion + ", game " + _gameVersionText + ").");
                    else
                        Log("No update for game " + _gameVersionText + ": " + skippedForGame + ".");
                    SetCheckResult(UpdateCheckResult.UpToDate, string.Empty);
                    SetState(UpdateState.UpToDate);
                    return;
                }

                lock (Sync)
                {
                    _release = release;
                    _availableVersion = release.Version;
                }
                Log("Update available: " + release.Version + " (current " + _currentVersion + ", game " + _gameVersionText + ").");
                SetCheckResult(UpdateCheckResult.Available, string.Empty);
                SetState(UpdateState.Available);

                if (autoUpdate) BeginDownload();
            }
            catch (Exception e)
            {
                SetCheckResult(UpdateCheckResult.Failed, e.Message);
                Fail(e);
            }
        }

        private static void BeginDownload()
        {
            Thread worker;
            lock (Sync)
            {
                if (_release == null) return;
                if (_worker != null && _worker.IsAlive && _worker != Thread.CurrentThread) return;
                _cancelRequested = false;
                _errorMessage = string.Empty;
                _progress = 0f;
                _state = UpdateState.Downloading;
                _dirty = true;
                worker = _worker = new Thread(DownloadWorker) { IsBackground = true, Name = "TimingShow.UpdateDownload" };
            }
            worker.Start();
        }

        private static void DownloadWorker()
        {
            UpdateRelease release;
            string modPath;
            string updateRoot;
            UpdateTarget target;
            Version current;
            lock (Sync)
            {
                release = _release;
                modPath = _modPath;
                updateRoot = _updateRoot;
                target = _target;
                current = _currentVersion;
            }
            if (release == null || string.IsNullOrEmpty(modPath))
            {
                SetState(UpdateState.Idle);
                return;
            }

            string stagingDirectory = Path.Combine(updateRoot, PendingUpdateApplier.StagingDirectoryName);
            string pendingPath = PendingUpdateFile.GetPath(updateRoot);
            string zipPath = null;
            try
            {
                string downloadDirectory = Path.Combine(updateRoot, PendingUpdateApplier.DownloadDirectoryName);
                Directory.CreateDirectory(downloadDirectory);
                zipPath = Path.Combine(downloadDirectory, "TimingShow-" + UpdatePackage.SanitizeVersion(release.Version) + ".zip");

                Trace("Staging " + release.Version + " in " + stagingDirectory + " (current " + current + ").");
                UpdateClient.Download(release, zipPath, SetProgress, IsCancellationRequested);
                if (IsCancellationRequested()) throw new OperationCanceledException();

                SetState(UpdateState.Verifying);
                UpdatePackage.Extract(zipPath, stagingDirectory, target);
                Trace("Package extracted.");
                UpdatePackage.Verify(stagingDirectory, modPath, release, target);
                Trace("Package verified.");
                UpdatePackage.WritePending(updateRoot, stagingDirectory, modPath, release, current.ToString(), target);

                bool applied = UpdatePackage.ApplyStaged(updateRoot, modPath);
                if (applied)
                {
                    Trace("Package applied.");
                    Log("Update " + release.Version + " installed; restart the game to finish updating.");
                }
                else if (File.Exists(pendingPath))
                {
                    Trace("Immediate apply deferred.");
                    Log("Update " + release.Version + " was downloaded but could not be installed while the game is running; restart the game to apply it.");
                }
                else
                {
                    throw new InvalidDataException("The downloaded package could not be applied.");
                }

                SetState(UpdateState.Ready);
            }
            catch (OperationCanceledException)
            {
                Log("Update download cancelled.");
                UpdatePackage.TryDeleteDirectory(stagingDirectory);
                SetState(UpdateState.Available);
            }
            catch (Exception e)
            {
                Fail(e);
            }
            finally
            {
                UpdatePackage.TryDeleteFile(zipPath);
            }
        }

        private static bool IsCancellationRequested()
        {
            return _cancelRequested;
        }

        private static void SetProgress(float value)
        {
            lock (Sync) _progress = value;
        }

        private static void SetState(UpdateState state)
        {
            lock (Sync)
            {
                _state = state;
                if (state != UpdateState.Failed) _errorMessage = string.Empty;
                if (state != UpdateState.Downloading) _progress = 0f;
                _dirty = true;
            }
            Trace("State: " + state + ".");
        }

        private static void SetCheckResult(UpdateCheckResult result, string error)
        {
            lock (Sync)
            {
                _checkResult = result;
                _checkError = error ?? string.Empty;
            }
        }

        private static void Fail(Exception e)
        {
            Log("Update failed: " + e.Message);
            lock (Sync) _errorMessage = e.Message;
            SetState(UpdateState.Failed);
        }

        private static void Log(string message)
        {
            IModLogger logger;
            lock (Sync) logger = _logger;
            if (logger != null) logger.Log(message);
        }

        private static bool TraceEnabled
        {
            get
            {
                Settings settings = ModContext.Settings;
                return settings != null && settings.Diag_UpdateTrace;
            }
        }

        private static void Trace(string message)
        {
            if (!TraceEnabled) return;
            Log("[Update] " + message);
        }

        private static Version ParseVersion(string text)
        {
            Version parsed;
            return UpdateClient.TryParseVersion(text, out parsed) ? parsed : new Version(0, 0, 0);
        }
    }
}
