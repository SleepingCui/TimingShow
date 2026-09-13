using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using Newtonsoft.Json;
using UnityEngine;

namespace TimingShow
{
    // Log list panel: directory scan, per-file metadata parsing, sorting and deletion.
    // The scan runs on a worker thread; only LogScanSync/_logScanCompleted/_logScanRunning cross threads.
    internal static class OptionLogList
    {
        private static bool _showLogList;
        private static GUIStyle _deleteButtonStyle;
        private static GUIStyle _deleteArmedButtonStyle;
        private static GUIStyle _warningLabelStyle;

        private static readonly List<LogListEntry> _logEntries = new List<LogListEntry>();
        private static readonly Dictionary<string, DateTime> _deleteArmedUntil = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> _deletePending = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<string> _deleteCompleted = new List<string>();
        private static readonly object DeleteSync = new object();
        private static Vector2 _logListScroll;
        private static string _logListDirectory;
        private static float _nextLogListRefresh;

        // The directory walk and the per-file metadata parsing run on a worker thread so the
        // GUI frame never blocks on disk IO. Only these fields cross the two threads.
        private static readonly object LogScanSync = new object();
        private static LogScanResult _logScanCompleted;
        private static bool _logScanRunning;

        private sealed class LogListEntry
        {
            public string FullPath;
            public string FileName;
            public string SongName;
            public DateTime LastWriteTime;
            public long Length;
            public long Timestamp = -1;
        }

        private sealed class LogScanResult
        {
            public string Directory;
            public List<LogListEntry> Entries;
        }

        // Called when the config window has just been opened.
        public static void OnConfigOpened()
        {
            _logListDirectory = null;
            _nextLogListRefresh = 0f;
            try { RequestLogScan(GetLogDirectory()); }
            catch (Exception e) { ModContext.Logger.Error("Failed to refresh logs on config open: " + e.Message); }
        }

        public static void Draw()
        {
            EnsureStyles();
            ProcessDeleteResults();
            ApplyLogScanResults();

            string foldoutArrow = _showLogList ? "▲" : "▼";
            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"{i18n.T("Label_LogList")} {foldoutArrow}", GUILayout.Width(150)))
                _showLogList = !_showLogList;
            GUILayout.EndHorizontal();

            if (!_showLogList) return;

            string logDir;
            try { logDir = GetLogDirectory(); }
            catch { logDir = string.Empty; }

            if (_logListDirectory != logDir || Time.realtimeSinceStartup >= _nextLogListRefresh)
                RequestLogScan(logDir);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(i18n.T("Btn_RefreshLogs"), GUILayout.Width(70)))
                RequestLogScan(logDir);
            if (GUILayout.Button(GetSortButtonText(), GUILayout.Width(120)))
            {
                ModContext.Settings.LogSort = (ModContext.Settings.LogSort + 1) % 3;
                _logEntries.Sort(CompareLogEntries);
            }
            long totalBytes = 0;
            for (int i = 0; i < _logEntries.Count; i++) totalBytes += _logEntries[i].Length;
            GUILayout.Label(string.Format(i18n.T("LogSummary"), _logEntries.Count, FormatFileSize(totalBytes)), GUILayout.ExpandWidth(true));
            if (!ModContext.Settings.AnalyzerBridgeEnabled)
            {
                GUILayout.Space(10);
                GUILayout.Label(i18n.T("LogAnalyzerBridgeDisabled"), _warningLabelStyle, GUILayout.ExpandWidth(false));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUI.skin.box);
            if (_logEntries.Count == 0)
            {
                GUILayout.Label(i18n.T("Label_NoLogs"));
            }
            else
            {
                _logListScroll = GUILayout.BeginScrollView(_logListScroll, GUILayout.Height(180));
                for (int i = 0; i < _logEntries.Count; i++)
                {
                    LogListEntry entry = _logEntries[i];
                    GUILayout.BeginHorizontal(GUI.skin.box);
                    GUILayout.Label(entry.FileName, GUILayout.MinWidth(190), GUILayout.ExpandWidth(true));
                    GUILayout.Label(FormatFileSize(entry.Length), GUILayout.Width(78));
                    GUILayout.Label(FormatTimestamp(entry.Timestamp), GUILayout.Width(145));
                    if (GUILayout.Button(i18n.T("Btn_AnalyzeLog"), GUILayout.Width(70)))
                        OpenLogInAnalyzer(entry.FullPath);
                    bool deletePending;
                    lock (DeleteSync) deletePending = _deletePending.Contains(entry.FullPath);
                    bool deleteArmed = IsDeleteArmed(entry.FullPath);
                    bool previousEnabled = GUI.enabled;
                    GUI.enabled = previousEnabled && !deletePending;
                    GUIStyle deleteStyle = deleteArmed ? _deleteArmedButtonStyle : _deleteButtonStyle;
                    string deleteLabel = deletePending ? i18n.T("Btn_DeletingLog") : i18n.T("Btn_DeleteLog");
                    if (GUILayout.Button(deleteLabel, deleteStyle, GUILayout.Width(70)))
                        HandleDeleteClick(entry.FullPath);
                    GUI.enabled = previousEnabled;
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private static void EnsureStyles()
        {
            if (_deleteButtonStyle != null) return;
            _deleteButtonStyle = new GUIStyle(GUI.skin.button);
            _deleteArmedButtonStyle = new GUIStyle(_deleteButtonStyle);
            _warningLabelStyle = new GUIStyle(GUI.skin.label);
            SetButtonTextColor(_deleteButtonStyle, Color.white);
            SetButtonTextColor(_deleteArmedButtonStyle, Color.red);
            SetButtonTextColor(_warningLabelStyle, Color.yellow);
        }

        private static void RequestLogScan(string logDir)
        {
            lock (LogScanSync)
            {
                if (_logScanRunning) return;
                _logScanRunning = true;
            }
            ThreadPool.QueueUserWorkItem(_ => ScanLogDirectory(logDir));
        }

        private static void ScanLogDirectory(string logDir)
        {
            var result = new LogScanResult { Directory = logDir, Entries = new List<LogListEntry>() };
            try
            {
                if (!string.IsNullOrWhiteSpace(logDir) && Directory.Exists(logDir))
                {
                    string[] files = Directory.GetFiles(logDir);
                    for (int i = 0; i < files.Length; i++)
                    {
                        string file = files[i];
                        string lower = file.ToLowerInvariant();
                        if (!lower.EndsWith(".json") && !lower.EndsWith(".tlog") && !lower.EndsWith(".tlog.gz")) continue;
                        LogListEntry entry = ReadLogEntry(file);
                        if (entry != null) result.Entries.Add(entry);
                    }
                }
            }
            catch (Exception e)
            {
                ModContext.Logger.Error("Failed to scan log directory: " + e.Message);
            }

            lock (LogScanSync)
            {
                _logScanCompleted = result;
                _logScanRunning = false;
            }
        }

        private static void ApplyLogScanResults()
        {
            LogScanResult result;
            lock (LogScanSync)
            {
                result = _logScanCompleted;
                _logScanCompleted = null;
            }
            if (result == null) return;

            // Sorted here rather than on the worker so the ordering always matches the
            // current sort mode, even if it changed while the scan was running.
            result.Entries.Sort(CompareLogEntries);
            _logEntries.Clear();
            _logEntries.AddRange(result.Entries);
            _logListDirectory = result.Directory;
            _nextLogListRefresh = Time.realtimeSinceStartup + 2f;
        }

        private static LogListEntry ReadLogEntry(string filePath)
        {
            try
            {
                var entry = new LogListEntry
                {
                    FullPath = filePath,
                    FileName = Path.GetFileName(filePath),
                    LastWriteTime = File.GetLastWriteTime(filePath),
                    Length = new FileInfo(filePath).Length
                };
                ReadLogMetadata(filePath, entry);
                return entry;
            }
            catch
            {
                // The file was removed, rotated or locked between the directory walk and this read.
                return null;
            }
        }

        private static void ReadLogMetadata(string filePath, LogListEntry entry)
        {
            try
            {
                string lower = filePath.ToLowerInvariant();
                if (lower.EndsWith(".json"))
                {
                    using (var reader = new JsonTextReader(new StreamReader(filePath)))
                    {
                        while (reader.Read())
                        {
                            if (reader.TokenType != JsonToken.PropertyName) continue;
                            string propertyName = reader.Value?.ToString();
                            if (!reader.Read()) continue;
                            if (string.Equals(propertyName, "timestamp", StringComparison.Ordinal) && reader.TokenType == JsonToken.Integer)
                                entry.Timestamp = Convert.ToInt64(reader.Value);
                            else if (string.Equals(propertyName, "songName", StringComparison.Ordinal) && reader.TokenType == JsonToken.String)
                                entry.SongName = reader.Value?.ToString();
                        }
                    }
                    return;
                }

                using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (Stream input = lower.EndsWith(".tlog.gz") ? (Stream)new GZipStream(fs, CompressionMode.Decompress) : fs)
                using (var reader = new BinaryReader(input, System.Text.Encoding.UTF8))
                {
                    string magic = new string(reader.ReadChars(4));
                    if (magic != "TSMZ") return;
                    reader.ReadByte();
                    entry.Timestamp = reader.ReadInt64();
                    entry.SongName = reader.ReadString();
                }
            }
            catch
            {
                // Keep defaults for incomplete or currently-writing files.
            }
        }

        private static int CompareLogEntries(LogListEntry a, LogListEntry b)
        {
            switch (ModContext.Settings.LogSort)
            {
                case Settings.LogSort_Size:
                    return b.Length.CompareTo(a.Length);
                case Settings.LogSort_SongName:
                    return StringComparer.OrdinalIgnoreCase.Compare(a.SongName ?? a.FileName, b.SongName ?? b.FileName);
                default:
                    return b.Timestamp.CompareTo(a.Timestamp);
            }
        }

        private static string GetSortButtonText()
        {
            string label;
            switch (ModContext.Settings.LogSort)
            {
                case Settings.LogSort_Size:
                    label = i18n.T("SortBySize");
                    break;
                case Settings.LogSort_SongName:
                    label = i18n.T("SortBySongName");
                    break;
                default:
                    label = i18n.T("SortByTime");
                    break;
            }
            return i18n.T("Btn_Sort") + ": " + label;
        }

        private static string FormatTimestamp(long timestamp)
        {
            if (timestamp < 0) return "-";
            try
            {
                return DateTimeOffset.FromUnixTimeSeconds(timestamp).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch
            {
                return "-";
            }
        }

        private static string FormatFileSize(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = Math.Max(0, bytes);
            int unitIndex = 0;
            while (size >= 1024.0 && unitIndex < units.Length - 1)
            {
                size /= 1024.0;
                unitIndex++;
            }
            return size.ToString("F1") + " " + units[unitIndex];
        }

        private static void OpenLogInAnalyzer(string filePath)
        {
            try
            {
                if (!ModContext.Settings.AnalyzerBridgeEnabled)
                {
                    ModContext.Logger.Log("Log analyzer bridge is disabled");
                    return;
                }
                string url = LogAnalyzerBridge.CreateUrl(filePath, ModContext.Settings.AnalyzerBridgePort);
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception e)
            {
                ModContext.Logger.Error("Failed to open log analyzer: " + e.Message);
            }
        }

        private static bool IsDeleteArmed(string filePath)
        {
            DateTime until;
            if (!_deleteArmedUntil.TryGetValue(filePath, out until)) return false;
            if (DateTime.UtcNow <= until) return true;
            _deleteArmedUntil.Remove(filePath);
            return false;
        }

        private static void HandleDeleteClick(string filePath)
        {
            if (IsDeleteArmed(filePath))
            {
                _deleteArmedUntil.Remove(filePath);
                lock (DeleteSync)
                {
                    if (!_deletePending.Add(filePath)) return;
                }
                ThreadPool.QueueUserWorkItem(_ => DeleteLogFileWorker(filePath));
                return;
            }

            _deleteArmedUntil[filePath] = DateTime.UtcNow.AddSeconds(3);
            GUI.changed = true;
        }

        private static void DeleteLogFileWorker(string filePath)
        {
            try
            {
                File.Delete(filePath);
                lock (DeleteSync) _deleteCompleted.Add(filePath);
            }
            catch (Exception e)
            {
                // Not reported as completed: the file is still on disk, so the row stays put
                // and the button is re-enabled for a retry.
                ModContext.Logger.Error("Failed to delete log file " + Path.GetFileName(filePath) + ": " + e.Message);
            }
            finally
            {
                lock (DeleteSync) _deletePending.Remove(filePath);
            }
        }

        private static void ProcessDeleteResults()
        {
            List<string> completed = null;
            lock (DeleteSync)
            {
                if (_deleteCompleted.Count > 0)
                {
                    completed = new List<string>(_deleteCompleted);
                    _deleteCompleted.Clear();
                }
            }
            if (completed == null) return;

            // A scan that started before the delete finished may still be in flight; drop the
            // affected rows right away so the list never shows a file that is already gone.
            for (int i = 0; i < completed.Count; i++)
                RemoveLogEntry(completed[i]);

            _logListDirectory = null;
            _nextLogListRefresh = 0f;
        }

        private static void RemoveLogEntry(string fullPath)
        {
            for (int i = _logEntries.Count - 1; i >= 0; i--)
            {
                if (string.Equals(_logEntries[i].FullPath, fullPath, StringComparison.OrdinalIgnoreCase))
                    _logEntries.RemoveAt(i);
            }
        }

        private static void SetButtonTextColor(GUIStyle style, Color color)
        {
            style.normal.textColor = color;
            style.hover.textColor = color;
            style.active.textColor = color;
            style.focused.textColor = color;
            style.onNormal.textColor = color;
            style.onHover.textColor = color;
            style.onActive.textColor = color;
            style.onFocused.textColor = color;
        }

        public static string GetDefaultLogDirectory()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "../Mods/TimingShow/Logs"));
        }

        public static string GetLogDirectory()
        {
            string abs = AbsLogPath(ModContext.Settings.LogDirectory);
            return string.IsNullOrWhiteSpace(abs) ? GetDefaultLogDirectory() : abs;
        }

        public static string AbsLogPath(string logDir)
        {
            if (string.IsNullOrWhiteSpace(logDir))
                return null;
            try
            {
                if (Path.IsPathRooted(logDir)) return Path.GetFullPath(logDir);
                string gameRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                return Path.GetFullPath(Path.Combine(gameRoot, logDir));
            }
            catch (Exception e)
            {
                ModContext.Logger.Log($"Err parsing path: {e.Message}");
                return logDir;
            }
        }
    }
}
