using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

namespace TimingShow.Logging
{
    internal static class LogRetention
    {
        private const int MaxKeepCount = 100000;

        private static readonly object Sync = new object();
        private static bool _running;
        private static bool _pending;
        private static int _completedVersion;

        public static int CompletedVersion => Volatile.Read(ref _completedVersion);
        
        public static void RequestCleanup()
        {
            Settings settings = ModContext.Settings;
            if (settings == null || !settings.LogAutoClean) return;

            string directory = ResolveLogDirectory(settings.LogDirectory);
            int keep = Math.Max(0, Math.Min(settings.LogKeepCount, MaxKeepCount));
            int sort = settings.LogSort;

            lock (Sync)
            {
                if (_running)
                {
                    _pending = true;
                    return;
                }
                _running = true;
            }

            ThreadPool.QueueUserWorkItem(_ => RunCleanup(directory, keep, sort));
        }

        private static void RunCleanup(string directory, int keep, int sort)
        {
            while (true)
            {
                lock (Sync) _pending = false;

                try { CleanupOnce(directory, keep, sort); }
                catch (Exception e) { ModContext.Logger?.Error("Log auto-clean failed: " + e.Message); }

                Interlocked.Increment(ref _completedVersion);

                lock (Sync)
                {
                    if (!_pending)
                    {
                        _running = false;
                        return;
                    }
                }
            }
        }

        private static void CleanupOnce(string directory, int keep, int sort)
        {
            Settings settings = ModContext.Settings;
            if (settings == null || !settings.LogAutoClean) return;
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory)) return;

            string[] files;
            try
            {
                files = Directory.GetFiles(directory);
            }
            catch (Exception e)
            {
                ModContext.Logger?.Log("Log auto-clean skipped: " + e.Message);
                return;
            }

            var entries = new List<RetainedLog>(files.Length);
            for (int i = 0; i < files.Length; i++)
            {
                string path = files[i];
                string lower = path.ToLowerInvariant();
                if (!lower.EndsWith(".json") && !lower.EndsWith(".tlog") && !lower.EndsWith(".tlog.gz")) continue;
                if (TimingLogger.IsFileBeingWritten(path)) continue;

                FileInfo info;
                try
                {
                    info = new FileInfo(path);
                    if (!info.Exists) continue;
                }
                catch
                {
                    continue;
                }

                long timestamp = TimestampFromFileName(info.Name);
                if (timestamp < 0)
                {
                    try { timestamp = new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero).ToUnixTimeSeconds(); }
                    catch { timestamp = 0; }
                }

                entries.Add(new RetainedLog
                {
                    Path = path,
                    FileName = info.Name,
                    Length = info.Length,
                    Timestamp = timestamp
                });
            }

            entries.Sort((a, b) => Compare(a, b, sort));

            for (int i = keep; i < entries.Count; i++)
            {
                try
                {
                    File.Delete(entries[i].Path);
                }
                catch (Exception e)
                {
                    ModContext.Logger?.Error("Failed to auto-clean log " + entries[i].FileName + ": " + e.Message);
                }
            }
        }

        private static int Compare(RetainedLog a, RetainedLog b, int sort)
        {
            int result;
            switch (sort)
            {
                case Settings.LogSort_Size:
                    result = b.Length.CompareTo(a.Length);
                    break;
                case Settings.LogSort_SongName:
                    result = StringComparer.OrdinalIgnoreCase.Compare(SongNameFromFileName(a.FileName), SongNameFromFileName(b.FileName));
                    break;
                default:
                    result = b.Timestamp.CompareTo(a.Timestamp);
                    break;
            }

            if (result != 0) return result;

            result = b.Timestamp.CompareTo(a.Timestamp);
            return result != 0 ? result : StringComparer.OrdinalIgnoreCase.Compare(a.FileName, b.FileName);
        }

        private static long TimestampFromFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return -1;

            int end = fileName.IndexOf('_');
            if (end <= 0) end = fileName.IndexOf('.');
            if (end <= 0) return -1;

            long value;
            return long.TryParse(fileName.Substring(0, end), out value) ? value : -1;
        }

        private static string SongNameFromFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return fileName;

            string name = fileName;
            if (name.EndsWith(".tlog.gz", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - ".tlog.gz".Length);
            else if (name.EndsWith(".tlog", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - ".tlog".Length);
            else if (name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - ".json".Length);

            int underscore = name.IndexOf('_');
            return underscore > 0 && underscore + 1 < name.Length ? name.Substring(underscore + 1) : name;
        }

        private static string ResolveLogDirectory(string logDir)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(logDir))
                    return Path.GetFullPath(Path.Combine(Application.dataPath, "../Mods/TimingShow/Logs"));
                if (Path.IsPathRooted(logDir))
                    return Path.GetFullPath(logDir);

                string gameRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                return Path.GetFullPath(Path.Combine(gameRoot, logDir));
            }
            catch
            {
                return null;
            }
        }

        private sealed class RetainedLog
        {
            public string Path;
            public string FileName;
            public long Length;
            public long Timestamp;
        }
    }
}
