using System;
using System.IO;
using System.Threading;

namespace TimingShow.Update
{
    public enum UpdateState
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

    public static class Updater
    {
        public const string DefaultRepositoryUrl = "https://raw.githubusercontent.com/SleepingCui/TimingShow/refs/heads/master/Repository.json";
        public const string RepositoryId = "TimingShow";

        private static readonly object Sync = new object();

        private static string _modPath;
        private static string _repositoryUrl = DefaultRepositoryUrl;
        private static Version _currentVersion = new Version(0, 0, 0);
        private static IModLogger _logger;
        private static UpdateRelease _release;
        private static string _availableVersion = string.Empty;
        private static string _errorMessage = string.Empty;
        private static float _progress;
        private static UpdateState _state = UpdateState.Idle;
        private static Thread _worker;
        private static volatile bool _cancelRequested;
        private static volatile bool _dirty;

        public static event Action Changed;

        public static bool CheckEnabled { get; private set; }

        public static bool AutoUpdateEnabled { get; private set; }

        public static bool Supported { get; private set; }

        public static UpdateState State
        {
            get { lock (Sync) return _state; }
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

        public static void Initialize(string modPath, string repositoryUrl, string currentVersion, IModLogger logger)
        {
            lock (Sync)
            {
                Supported = true;
                _modPath = modPath;
                _repositoryUrl = string.IsNullOrEmpty(repositoryUrl) ? DefaultRepositoryUrl : repositoryUrl;
                _logger = logger;
                _currentVersion = ParseVersion(currentVersion);
            }

            Settings settings = ModContext.Settings;
            CheckEnabled = settings == null || settings.UpdateCheckEnabled;
            AutoUpdateEnabled = settings != null && settings.UpdateAutoUpdate;

            UpdateHost.EnsureHost();
            if (CheckEnabled) BeginCheck();
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
                    BeginCheck();
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

        private static void BeginCheck()
        {
            Thread worker;
            lock (Sync)
            {
                if (_worker != null && _worker.IsAlive && _worker != Thread.CurrentThread) return;
                _cancelRequested = false;
                _release = null;
                _availableVersion = string.Empty;
                _errorMessage = string.Empty;
                _progress = 0f;
                _state = UpdateState.Checking;
                _dirty = true;
                worker = _worker = new Thread(CheckWorker) { IsBackground = true, Name = "TimingShow.UpdateCheck" };
            }
            worker.Start();
        }

        private static void CheckWorker()
        {
            try
            {
                string json = UpdateClient.FetchRepository(_repositoryUrl);
                UpdateRelease release = UpdateClient.FindRelease(json, RepositoryId, _currentVersion);
                if (release == null)
                {
                    Log("Mod is up to date (" + _currentVersion + ").");
                    SetState(UpdateState.UpToDate);
                    return;
                }

                lock (Sync)
                {
                    _release = release;
                    _availableVersion = release.Version;
                }
                Log("Update available: " + release.Version + " (current " + _currentVersion + ").");
                SetState(UpdateState.Available);

                if (AutoUpdateEnabled) BeginDownload();
            }
            catch (Exception e)
            {
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
            Version current;
            lock (Sync)
            {
                release = _release;
                modPath = _modPath;
                current = _currentVersion;
            }
            if (release == null || string.IsNullOrEmpty(modPath))
            {
                SetState(UpdateState.Idle);
                return;
            }

            string updateDirectory = Path.Combine(modPath, UpdatePackage.UpdateDirectoryName);
            string stagingDirectory = Path.Combine(updateDirectory, UpdatePackage.StagingDirectoryName);
            string zipPath = null;
            try
            {
                string downloadDirectory = Path.Combine(updateDirectory, "download");
                Directory.CreateDirectory(downloadDirectory);
                zipPath = Path.Combine(downloadDirectory, "TimingShow-" + UpdatePackage.SanitizeVersion(release.Version) + ".zip");

                UpdateClient.Download(release, zipPath, SetProgress, IsCancellationRequested);
                if (IsCancellationRequested()) throw new OperationCanceledException();

                SetState(UpdateState.Verifying);
                UpdatePackage.Extract(zipPath, stagingDirectory);
                UpdatePackage.Verify(stagingDirectory, modPath, release);
                UpdatePackage.WriteManifest(stagingDirectory);
                UpdatePackage.WritePending(modPath, release.Version, current.ToString());

                Log("Update " + release.Version + " verified and staged; it will be applied on the next launch.");
                SetState(UpdateState.Ready);
            }
            catch (OperationCanceledException)
            {
                Log("Update download cancelled.");
                DeleteDirectory(stagingDirectory);
                SetState(UpdateState.Available);
            }
            catch (Exception e)
            {
                Fail(e);
            }
            finally
            {
                TryDeleteFile(zipPath);
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
        }

        private static void Fail(Exception e)
        {
            Log("Update failed: " + e.Message);
            lock (Sync)
            {
                _errorMessage = e.Message;
                _state = UpdateState.Failed;
                _progress = 0f;
                _dirty = true;
            }
        }

        private static void Log(string message)
        {
            IModLogger logger;
            lock (Sync) logger = _logger;
            if (logger != null) logger.Log(message);
        }

        private static Version ParseVersion(string text)
        {
            Version parsed;
            return UpdateClient.TryParseVersion(text, out parsed) ? parsed : new Version(0, 0, 0);
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path)) File.Delete(path);
            }
            catch (Exception)
            {
            }
        }

        private static void DeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path)) Directory.Delete(path, true);
            }
            catch (Exception)
            {
            }
        }
    }
}
