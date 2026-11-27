using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using TimingShow.Update;

namespace TimingShow.Bootstrap
{
    internal enum RepairState
    {
        Idle,
        Checking,
        Downloading,
        Verifying,
        Installing,
        ReadyToLoad,
        RestartRequired,
        Loaded,
        Failed,
        Cancelled
    }

    internal sealed class RepairInstaller
    {
        public const string CoreFileName = "TimingShow.dll";
        public const string LanguageFileName = "lang.json";
        public const string InfoFileName = "Info.json";
        public const string DefaultRepositoryUrl = "https://raw.githubusercontent.com/SleepingCui/TimingShow/refs/heads/master/Repository.json";

        private const string UserAgent = "TimingShow-Loader";
        private const int RequestTimeoutMs = 20000;
        private const int DownloadTimeoutMs = 60000;
        private const string MelonModsPrefix = "Mods/";
        private const string MelonPluginsPrefix = "Plugins/";
        private const string PluginStagingDirectoryName = "plugins-emergency";

        private static readonly string[] TrustedHosts = { "github.com", "githubusercontent.com" };

        private readonly object _sync = new object();
        private readonly string _modPath;
        private readonly string _pluginsPath;
        private readonly string _updateRoot;
        private readonly string _releaseId;
        private readonly string _repositoryUrl;
        private readonly bool _customRepository;
        private readonly bool _skipChecksum;
        private readonly bool _skipTlsValidation;
        private readonly string _loaderFileName;
        private readonly string _runningLoaderPath;
        private readonly string _runningLoaderHash;
        private readonly bool _melonLayout;
        private readonly Action<string> _log;
        private readonly Action<string> _error;

        private Thread _worker;
        private volatile bool _cancelRequested;
        private RepairState _state;
        private float _progress;
        private string _errorMessage = string.Empty;
        private string _version = string.Empty;
        private bool _loaderChanged;

        public RepairInstaller(string modPath, string pluginsPath, bool melonLayout, string releaseId, string repositoryUrl, string settingsPath, string updateRoot, string loaderFileName, Action<string> log, Action<string> error)
        {
            _modPath = PendingUpdateApplier.NormalizeDirectory(modPath);
            _pluginsPath = string.IsNullOrEmpty(pluginsPath) ? null : PendingUpdateApplier.NormalizeDirectory(pluginsPath);
            _melonLayout = melonLayout;
            _releaseId = releaseId;
            _log = log ?? delegate { };
            _error = error ?? delegate { };
            string defaultRepository = string.IsNullOrEmpty(repositoryUrl) ? DefaultRepositoryUrl : repositoryUrl.Trim();
            UpdateOverrides overrides = LoadUpdateOverrides(settingsPath);
            bool useCustomRepository = overrides.CustomRepository && !string.IsNullOrEmpty(overrides.RepositoryUrl);
            _repositoryUrl = useCustomRepository ? overrides.RepositoryUrl.Trim() : defaultRepository;
            _customRepository = useCustomRepository;
            _skipChecksum = overrides.SkipChecksum;
            _skipTlsValidation = overrides.SkipTlsValidation;
            _updateRoot = PendingUpdateApplier.NormalizeDirectory(updateRoot);
            _loaderFileName = loaderFileName;
            _runningLoaderPath = ResolveRunningLoaderPath();
            _runningLoaderHash = TryComputeHash(_runningLoaderPath);
            if (_customRepository) _log("Using the custom update source from Settings.json: " + _repositoryUrl);
            if (_skipChecksum) _log("Checksum verification skipped (Debug_SkipUpdateChecksum).");
            if (_skipTlsValidation) _log("TLS certificate validation skipped (Debug_SkipTlsValidation).");
        }

        private sealed class UpdateOverrides
        {
            public bool CustomRepository;
            public string RepositoryUrl = string.Empty;
            public bool SkipChecksum;
            public bool SkipTlsValidation;
        }

        private UpdateOverrides LoadUpdateOverrides(string settingsPath)
        {
            UpdateOverrides settings = new UpdateOverrides();
            if (string.IsNullOrEmpty(settingsPath) || !File.Exists(settingsPath)) return settings;
            try
            {
                Dictionary<string, object> values = JsonText.ParseObject(File.ReadAllText(settingsPath));
                settings.RepositoryUrl = GetString(values, "Update_SourceUrl").Trim();
                if (string.IsNullOrEmpty(settings.RepositoryUrl) && GetBool(values, "Debug_CustomUpdateUrl"))
                    settings.RepositoryUrl = GetString(values, "Debug_UpdateUrl").Trim();
                settings.CustomRepository = !string.IsNullOrEmpty(settings.RepositoryUrl);
                settings.SkipChecksum = GetBool(values, "Debug_SkipUpdateChecksum");
                settings.SkipTlsValidation = GetBool(values, "Debug_SkipTlsValidation");
            }
            catch (Exception e)
            {
                _log("Failed to read update settings: " + e.Message);
            }
            return settings;
        }

        public RepairState State
        {
            get
            {
                lock (_sync) return _state;
            }
        }

        public float Progress
        {
            get
            {
                lock (_sync) return _progress;
            }
        }

        public string ErrorMessage
        {
            get
            {
                lock (_sync) return _errorMessage;
            }
        }

        public string Version
        {
            get
            {
                lock (_sync) return _version;
            }
        }

        public bool LoaderChanged
        {
            get
            {
                lock (_sync) return _loaderChanged;
            }
        }

        public bool IsRunning
        {
            get
            {
                lock (_sync)
                {
                    return _state == RepairState.Checking || _state == RepairState.Downloading
                        || _state == RepairState.Verifying || _state == RepairState.Installing;
                }
            }
        }

        public void Start()
        {
            lock (_sync)
            {
                if (_worker != null && _worker.IsAlive) return;
                _cancelRequested = false;
                _state = RepairState.Checking;
                _progress = 0f;
                _errorMessage = string.Empty;
                _version = string.Empty;
                _loaderChanged = false;
                _worker = new Thread(DownloadWorker) { IsBackground = true, Name = "TimingShow.Repair" };
                _worker.Start();
            }
        }

        public void Cancel()
        {
            lock (_sync)
            {
                if (!IsRunning) return;
                _cancelRequested = true;
            }
        }

        private void DownloadWorker()
        {
            string pluginStaging = Path.Combine(_updateRoot, PluginStagingDirectoryName);
            string downloadDirectory = Path.Combine(_updateRoot, PendingUpdateApplier.DownloadDirectoryName);
            string zipPath = null;
            try
            {
                string manifest = FetchManifest();
                ThrowIfCancelled();

                ReleaseInfo release = SelectRelease(manifest);
                lock (_sync) _version = release.Version;
                string sizeText = release.Size > 0 ? " (" + FormatSize(release.Size) + ")" : string.Empty;
                _log("Downloading TimingShow " + release.Version + sizeText + ".");

                Directory.CreateDirectory(downloadDirectory);
                zipPath = Path.Combine(downloadDirectory, "TimingShow-" + Sanitize(release.Version) + ".zip");
                SetState(RepairState.Downloading);
                DownloadPackage(release, zipPath);
                ThrowIfCancelled();

                SetState(RepairState.Verifying);
                VerifyDownload(release, zipPath);
                ThrowIfCancelled();

                string staging = Path.Combine(_updateRoot, PendingUpdateApplier.StagingDirectoryName);
                ExtractPackage(zipPath, staging, pluginStaging);
                VerifyStaging(staging);
                ThrowIfCancelled();

                SetState(RepairState.Installing);
                InstallStaging(staging, pluginStaging);

                SetState(_loaderChanged ? RepairState.RestartRequired : RepairState.ReadyToLoad);
                if (_loaderChanged) _log("TimingShow " + release.Version + " installed. Restart required.");
                else _log("TimingShow " + release.Version + " installed. Ready to load.");
            }
            catch (OperationCanceledException)
            {
                _log("The core download was cancelled.");
                SetState(RepairState.Cancelled);
            }
            catch (Exception e)
            {
                lock (_sync) _errorMessage = e.Message;
                _error("The core download failed: " + e.Message);
                SetState(RepairState.Failed);
            }
            finally
            {
                DeleteDirectory(pluginStaging);
                PendingUpdateFile.TryDelete(zipPath);
            }
        }

        private string FetchManifest()
        {
            Uri uri = CreateUri(_repositoryUrl);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            ConfigureRequest(request, RequestTimeoutMs);
            using (WebResponse response = request.GetResponse())
            {
                ValidateResponse(response);
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private ReleaseInfo SelectRelease(string manifest)
        {
            Dictionary<string, object> root;
            try
            {
                root = JsonText.ParseObject(manifest);
            }
            catch (FormatException e)
            {
                throw new InvalidDataException("The update manifest is corrupted: " + e.Message);
            }

            object releasesValue;
            if (!root.TryGetValue("Releases", out releasesValue)) throw new InvalidDataException("The update manifest lists no releases.");
            List<object> releases = releasesValue as List<object>;
            if (releases == null) throw new InvalidDataException("The update manifest lists no releases.");

            ReleaseInfo best = null;
            Version bestVersion = null;
            foreach (object item in releases)
            {
                Dictionary<string, object> release = item as Dictionary<string, object>;
                if (release == null) continue;
                if (!string.Equals(GetString(release, "Id"), _releaseId, StringComparison.OrdinalIgnoreCase)) continue;

                string versionText = GetString(release, "Version");
                Version version;
                if (!TryParseVersion(versionText, out version)) continue;

                string downloadUrl = GetString(release, "DownloadUrl");
                string sha256 = GetString(release, "Sha256");
                long size = GetLong(release, "Size");
                if (string.IsNullOrEmpty(downloadUrl)) continue;
                if (!_skipChecksum && (sha256.Length != 64 || size <= 0)) continue;
                if (bestVersion != null && version <= bestVersion) continue;

                best = new ReleaseInfo { Version = versionText.Trim(), DownloadUrl = downloadUrl.Trim(), Sha256 = sha256.Trim(), Size = size };
                bestVersion = version;
            }

            if (best == null) throw new InvalidDataException("No downloadable release was found for " + _releaseId + ".");
            return best;
        }

        private void DownloadPackage(ReleaseInfo release, string destination)
        {
            Uri uri = CreateUri(release.DownloadUrl);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            ConfigureRequest(request, DownloadTimeoutMs);

            using (WebResponse response = request.GetResponse())
            {
                ValidateResponse(response);
                long total = release.Size > 0 ? release.Size : response.ContentLength;
                long received = 0;
                byte[] buffer = new byte[81920];
                using (Stream input = response.GetResponseStream())
                using (FileStream output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    int read;
                    while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        ThrowIfCancelled();
                        output.Write(buffer, 0, read);
                        received += read;
                        if (total > 0 && received > total) throw new InvalidDataException("The downloaded package is larger than expected.");
                        if (total > 0) SetProgress((float)received / total);
                    }
                }

                if (total > 0 && received != total) throw new InvalidDataException("The downloaded package is incomplete.");
            }
        }

        private void VerifyDownload(ReleaseInfo release, string path)
        {
            FileInfo file = new FileInfo(path);
            if (!file.Exists) throw new FileNotFoundException("The downloaded package is missing.", path);
            if (release.Size > 0 && file.Length != release.Size) throw new InvalidDataException("The downloaded package has an unexpected size.");
            if (_skipChecksum)
            {
                _log("Skipping the download checksum (Debug_SkipUpdateChecksum is enabled).");
                return;
            }
            if (!string.Equals(PendingUpdateFile.ComputeHash(path), release.Sha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("The downloaded package failed its checksum.");
        }

        private void ExtractPackage(string zipPath, string stagingDirectory, string pluginStagingDirectory)
        {
            DeleteDirectory(stagingDirectory);
            DeleteDirectory(pluginStagingDirectory);
            Directory.CreateDirectory(stagingDirectory);

            using (FileStream stream = File.OpenRead(zipPath))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                List<ZipArchiveEntry> entries = new List<ZipArchiveEntry>();
                bool hasLoaderLayout = false;
                bool hasRootFiles = false;
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name)) continue;
                    entries.Add(entry);
                    string relative = entry.FullName.Replace('\\', '/');
                    if (relative.StartsWith(MelonModsPrefix, StringComparison.OrdinalIgnoreCase) || relative.StartsWith(MelonPluginsPrefix, StringComparison.OrdinalIgnoreCase)) hasLoaderLayout = true;
                    else hasRootFiles = true;
                }

                foreach (ZipArchiveEntry entry in entries)
                {
                    string relative = entry.FullName.Replace('\\', '/');
                    bool inMods = relative.StartsWith(MelonModsPrefix, StringComparison.OrdinalIgnoreCase);
                    bool inPlugins = relative.StartsWith(MelonPluginsPrefix, StringComparison.OrdinalIgnoreCase);
                    if (_melonLayout && !hasLoaderLayout)
                    {
                        if (!inMods && !inPlugins) ExtractEntry(entry, SafeCombine(stagingDirectory, relative));
                    }
                    else if (_melonLayout)
                    {
                        if (inMods) ExtractEntry(entry, SafeCombine(stagingDirectory, relative.Substring(MelonModsPrefix.Length)));
                        else if (inPlugins) ExtractEntry(entry, SafeCombine(pluginStagingDirectory, relative.Substring(MelonPluginsPrefix.Length)));
                    }
                    else if (!hasRootFiles)
                    {
                        if (inMods) ExtractEntry(entry, SafeCombine(stagingDirectory, relative.Substring(MelonModsPrefix.Length)));
                    }
                    else if (!inMods && !inPlugins)
                    {
                        ExtractEntry(entry, SafeCombine(stagingDirectory, relative));
                    }
                }
            }
        }

        private void VerifyStaging(string staging)
        {
            string core = Path.Combine(staging, CoreFileName);
            if (!File.Exists(core)) throw new InvalidDataException("The package does not contain " + CoreFileName + ".");
            AssemblyName coreName = AssemblyName.GetAssemblyName(core);
            if (!string.Equals(coreName.Name, "TimingShow", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("The package contains an unexpected core assembly.");

            string language = Path.Combine(staging, LanguageFileName);
            if (!File.Exists(language) || new FileInfo(language).Length == 0) throw new InvalidDataException("The package does not contain " + LanguageFileName + ".");

            string loader = Path.Combine(staging, _loaderFileName);
            if (!File.Exists(loader)) throw new InvalidDataException("The package does not contain " + _loaderFileName + ".");
            AssemblyName loaderName = AssemblyName.GetAssemblyName(loader);
            if (!string.Equals(loaderName.Name, Path.GetFileNameWithoutExtension(_loaderFileName), StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("The package contains an unexpected loader assembly.");
        }

        private void InstallStaging(string staging, string pluginStaging)
        {
            string stagedLoader = Path.Combine(staging, _loaderFileName);
            string stagedHash = PendingUpdateFile.ComputeHash(stagedLoader);
            bool canLoadNow = _runningLoaderHash.Length > 0
                ? string.Equals(stagedHash, _runningLoaderHash, StringComparison.OrdinalIgnoreCase)
                : FilesEqual(stagedLoader, _runningLoaderPath);
            
            if (!canLoadNow)
            {
                _log("Loader changed. Restart required.");
                ReplaceLoadedFile(stagedLoader, _runningLoaderPath);
            }

            lock (_sync) _loaderChanged = !canLoadNow;
            if (canLoadNow) _log("Loader unchanged. No restart required.");

            string stagingRoot = Path.GetFullPath(staging);
            if (!stagingRoot.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)) stagingRoot += Path.DirectorySeparatorChar;

            List<PendingFileEntry> files = new List<PendingFileEntry>();
            foreach (string file in Directory.GetFiles(staging, "*", SearchOption.AllDirectories))
            {
                string relative = file.Substring(stagingRoot.Length).Replace('\\', '/');
                if (string.Equals(relative, _loaderFileName, StringComparison.OrdinalIgnoreCase)) continue;
                PendingFileEntry entry = new PendingFileEntry();
                entry.RelativePath = relative;
                entry.Size = new FileInfo(file).Length;
                entry.Sha256 = PendingUpdateFile.ComputeHash(file);
                files.Add(entry);
            }

            if (files.Count == 0) throw new InvalidDataException("The downloaded package contains no installable files.");

            PendingUpdatePlan plan = new PendingUpdatePlan();
            plan.Schema = PendingUpdateFile.SchemaVersion;
            plan.Id = _releaseId;
            plan.Version = Version;
            plan.FromVersion = string.Empty;
            plan.Target = _melonLayout ? "Melon" : "Umm";
            plan.ModPath = _modPath;
            plan.Created = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            plan.Files = files;
            PendingUpdateFile.Write(_updateRoot, plan);

            string applyError = null;
            Action<string> error = message =>
            {
                applyError = message;
                _error(message);
            };
            if (!PendingUpdateApplier.Apply(_updateRoot, _modPath, _log, error))
            {
                throw new InvalidDataException("The downloaded files could not be installed" + (string.IsNullOrEmpty(applyError) ? "." : ": " + applyError));
            }

            InstallPlugins(pluginStaging);
        }

        private void InstallPlugins(string pluginStaging)
        {
            if (!_melonLayout || _pluginsPath == null || !Directory.Exists(pluginStaging)) return;

            string root = Path.GetFullPath(pluginStaging);
            if (!root.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)) root += Path.DirectorySeparatorChar;
            foreach (string file in Directory.GetFiles(pluginStaging, "*", SearchOption.AllDirectories))
            {
                string relative = file.Substring(root.Length);
                string target = Path.Combine(_pluginsPath, relative);
                try
                {
                    ReplaceLoadedFile(file, target);
                }
                catch (Exception e)
                {
                    _error("Failed to install " + relative + " into Plugins: " + e.Message);
                }
            }
        }

        private static void ReplaceLoadedFile(string source, string target)
        {
            string directory = Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            string incoming = target + ".incoming";
            File.Copy(source, incoming, true);
            try
            {
                if (!File.Exists(target))
                {
                    File.Move(incoming, target);
                    return;
                }

                try
                {
                    File.Replace(incoming, target, null);
                }
                catch (IOException)
                {
                    ReplaceByRename(incoming, target);
                }
                catch (UnauthorizedAccessException)
                {
                    ReplaceByRename(incoming, target);
                }
            }
            finally
            {
                PendingUpdateFile.TryDelete(incoming);
            }
        }

        private static void ReplaceByRename(string incoming, string target)
        {
            string backup = target + ".old-" + DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture);
            File.Move(target, backup);
            try
            {
                File.Move(incoming, target);
            }
            catch
            {
                try
                {
                    File.Move(backup, target);
                }
                catch (Exception)
                {
                }
                throw;
            }
            PendingUpdateFile.TryDelete(backup);
        }

        private string ResolveRunningLoaderPath()
        {
            try
            {
                string location = Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(location) && File.Exists(location) && string.Equals(Path.GetDirectoryName(Path.GetFullPath(location)), _modPath, StringComparison.OrdinalIgnoreCase))
                    return Path.GetFullPath(location);
            }
            catch (Exception)
            {
            }
            return Path.Combine(_modPath, _loaderFileName);
        }

        private static string TryComputeHash(string path)
        {
            try
            {
                if (File.Exists(path)) return PendingUpdateFile.ComputeHash(path);
            }
            catch (Exception)
            {
            }
            return string.Empty;
        }

        private static bool FilesEqual(string left, string right)
        {
            try
            {
                FileInfo leftInfo = new FileInfo(left);
                FileInfo rightInfo = new FileInfo(right);
                if (!leftInfo.Exists || !rightInfo.Exists || leftInfo.Length != rightInfo.Length) return false;
                return string.Equals(PendingUpdateFile.ComputeHash(left), PendingUpdateFile.ComputeHash(right), StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void ExtractEntry(ZipArchiveEntry entry, string target)
        {
            string directory = Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            entry.ExtractToFile(target, true);
        }

        private static string SafeCombine(string root, string relative)
        {
            string normalized = relative.Replace('\\', '/');
            string full = Path.GetFullPath(Path.Combine(root, normalized));
            string prefix = Path.GetFullPath(root);
            if (!prefix.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)) prefix += Path.DirectorySeparatorChar;
            if (!full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("The package contains an unsafe path: " + relative);
            return full;
        }

        private void SetState(RepairState state)
        {
            lock (_sync) _state = state;
        }

        private void SetProgress(float value)
        {
            lock (_sync) _progress = value < 0f ? 0f : (value > 1f ? 1f : value);
        }

        private void ThrowIfCancelled()
        {
            if (_cancelRequested) throw new OperationCanceledException();
        }

        private Uri CreateUri(string url)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri)) throw new InvalidDataException("The update source is not a valid address: " + url);
            bool https = uri.Scheme == Uri.UriSchemeHttps;
            if (!https && !(_skipTlsValidation && uri.Scheme == Uri.UriSchemeHttp)) throw new InvalidDataException("The update source is not an HTTPS address: " + url);
            if (!_customRepository && !IsTrustedHost(uri.Host)) throw new InvalidDataException("The update source is not trusted: " + uri.Host);
            return uri;
        }

        private static bool IsTrustedHost(string host)
        {
            if (string.IsNullOrEmpty(host)) return false;
            foreach (string trusted in TrustedHosts)
            {
                if (string.Equals(host, trusted, StringComparison.OrdinalIgnoreCase)) return true;
                if (host.EndsWith("." + trusted, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private void ConfigureRequest(HttpWebRequest request, int timeout)
        {
            request.UserAgent = UserAgent;
            request.Timeout = timeout;
            request.ReadWriteTimeout = timeout;
            request.AllowAutoRedirect = true;
            request.KeepAlive = false;
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            if (_skipTlsValidation) ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
        }

        private void ValidateResponse(WebResponse response)
        {
            Uri responseUri = response.ResponseUri;
            if (responseUri == null) throw new InvalidDataException("The update server redirected to an untrusted address.");
            bool https = responseUri.Scheme == Uri.UriSchemeHttps;
            if (!https && !(_skipTlsValidation && responseUri.Scheme == Uri.UriSchemeHttp)) throw new InvalidDataException("The update server redirected to an untrusted address.");
            if (!_customRepository && !IsTrustedHost(responseUri.Host)) throw new InvalidDataException("The update server redirected to an untrusted address.");
        }

        private static bool GetBool(Dictionary<string, object> values, string key)
        {
            object value;
            if (!values.TryGetValue(key, out value) || value == null) return false;
            if (value is bool) return (bool)value;
            string text = value as string;
            if (text != null) return string.Equals(text.Trim(), "true", StringComparison.OrdinalIgnoreCase) || text.Trim() == "1";
            try
            {
                return Convert.ToInt64(value, CultureInfo.InvariantCulture) != 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string GetString(Dictionary<string, object> values, string key)
        {
            object value;
            if (!values.TryGetValue(key, out value) || value == null) return string.Empty;
            return value as string ?? Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static long GetLong(Dictionary<string, object> values, string key)
        {
            object value;
            if (!values.TryGetValue(key, out value) || value == null) return 0;
            try
            {
                return Convert.ToInt64(value, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private static bool TryParseVersion(string text, out Version version)
        {
            version = null;
            if (string.IsNullOrEmpty(text)) return false;
            string trimmed = text.Trim();
            if (trimmed.Length > 0 && (trimmed[0] == 'v' || trimmed[0] == 'V')) trimmed = trimmed.Substring(1);
            return System.Version.TryParse(trimmed, out version);
        }

        private static string Sanitize(string text)
        {
            if (string.IsNullOrEmpty(text)) return "update";
            StringBuilder builder = new StringBuilder(text.Length);
            foreach (char character in text)
            {
                if (char.IsLetterOrDigit(character) || character == '.' || character == '-' || character == '_') builder.Append(character);
            }
            return builder.Length == 0 ? "update" : builder.ToString();
        }

        private static string FormatSize(long bytes)
        {
            if (bytes >= 1024L * 1024L) return (bytes / (1024f * 1024f)).ToString("0.0", CultureInfo.InvariantCulture) + " MB";
            if (bytes >= 1024L) return (bytes / 1024f).ToString("0.0", CultureInfo.InvariantCulture) + " KB";
            return bytes.ToString(CultureInfo.InvariantCulture) + " B";
        }

        private static void DeleteDirectory(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && Directory.Exists(path)) Directory.Delete(path, true);
            }
            catch (Exception)
            {
            }
        }

        private sealed class ReleaseInfo
        {
            public string Version;
            public string DownloadUrl;
            public string Sha256;
            public long Size;
        }
    }
}
