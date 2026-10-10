using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using UnityEngine;
using TimingShow.Bridge;
using TimingShow.HUD;
using TimingShow.Logging;

namespace TimingShow.Options
{
    internal static class OptionLogList
    {
        private const string Ellipsis = "...";

        private static bool _showLogList;
        private static GUIStyle _deleteButtonStyle;
        private static GUIStyle _deleteArmedButtonStyle;
        private static GUIStyle _warningLabelStyle;
        private static GUIStyle _logNameLabelStyle;
        private static readonly GUIContent _measureContent = new GUIContent();

        private static readonly List<LogListEntry> _logEntries = new List<LogListEntry>();
        private static readonly Dictionary<string, DateTime> _deleteArmedUntil = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> _deletePending = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<string> _deleteCompleted = new List<string>();
        private static readonly object DeleteSync = new object();
        private static Vector2 _logListScroll;
        private static string _logListDirectory;
        private static bool _logListDirty = true;

        private const float LogListHeight = 180f;
        private static float _logRowHeight;

        private static int LogListDrawBudget => Mathf.Clamp(ModContext.Settings.LogListDrawBudget, 4, 400);
        
        private static readonly object LogScanSync = new object();
        private static LogScanResult _logScanCompleted;
        private static bool _logScanRunning;
        private static bool _cacheLoaded;

        private static readonly object GraphLoadSync = new object();
        private static LogGraphLoad _graphLoadPending;
        private static LogGraphLoad _graphLoadCompleted;

        private enum LogGraphLoadResult { Success, Failed, Missing }

        private sealed class LogGraphLoad
        {
            public string FilePath;
            public string FileName;
            public TimingLogData Data;
            public string Error;
            public LogGraphLoadResult Result;
        }

        private const int MetaCacheVersion = 1;
        private const int MetaHeaderProbeBytes = 64 * 1024;
        private static readonly Dictionary<string, MetaCacheEntry> MetaCache = new Dictionary<string, MetaCacheEntry>(StringComparer.OrdinalIgnoreCase);

        private sealed class MetaCacheEntry
        {
            public long Length;
            public long LastWriteTicks;
            public long Timestamp = -1;
            public string SongName;
        }

        private sealed class LogListEntry
        {
            public string FullPath;
            public string FileName;
            public string SongName;
            public DateTime LastWriteTime;
            public long Length;
            public long Timestamp = -1;

            public void ApplyCached(long length, long lastWriteTicks, string songName, long timestamp)
            {
                Length = length;
                LastWriteTime = SafeFromTicks(lastWriteTicks);
                SongName = songName;
                Timestamp = timestamp;
            }
        }

        private sealed class LogScanResult
        {
            public string Directory;
            public List<LogListEntry> Entries;
        }
        
        public static void OnConfigOpened()
        {
            _logListDirectory = null;
            _logListDirty = true;
            try { RequestLogScan(GetLogDirectory()); }
            catch (Exception e) { ModContext.Logger.Error("Failed to refresh logs on config open: " + e.Message); }
        }

        public static void Draw()
        {
            EnsureStyles();
            ProcessDeleteResults();
            ApplyLogScanResults();
            ApplyLogGraphLoadResult();
            RemoveActiveLogEntries();
            UpdatePendingHintSnapshot();

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

            if (_logListDirty || _logListDirectory != logDir)
                RequestLogScan(logDir);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(i18n.T("Btn_RefreshLogs"), GUILayout.Width(70)))
            {
                _logListDirty = true;
                RequestLogScan(logDir);
            }
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

            DrawLogGraphPendingOverlay();

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUI.skin.box);
            if (_logEntries.Count == 0)
            {
                GUILayout.Label(i18n.T("Label_NoLogs"));
            }
            else
            {
                Rect viewport = GUILayoutUtility.GetRect(1f, 1f, LogListHeight, LogListHeight, GUILayout.ExpandWidth(true));
                HandleLogListScroll(viewport);
                DrawVirtualLogList(viewport, ref _logListScroll);
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private static void HandleLogListScroll(Rect viewport)
        {
            Event current = Event.current;
            if (current == null || current.type != EventType.ScrollWheel) return;
            if (!viewport.Contains(current.mousePosition)) return;

            float contentHeight = _logEntries.Count * _logRowHeight;
            float max = Mathf.Max(0f, contentHeight - viewport.height);
            _logListScroll.y = Mathf.Clamp(_logListScroll.y + current.delta.y * _logRowHeight * 3f, 0f, max);
            current.Use();
        }

        private static void DrawVirtualLogList(Rect viewport, ref Vector2 scroll)
        {
            if (_logRowHeight <= 0f) _logRowHeight = MeasureRowHeight();
            float rowHeight = _logRowHeight;

            Event current = Event.current;
            if (current != null && current.type == EventType.Layout) return;

            int budget = LogListDrawBudget;

            GUI.BeginClip(viewport);
            try
            {
                int first = Mathf.Max(0, Mathf.FloorToInt(scroll.y / rowHeight));
                int last = Mathf.Min(_logEntries.Count - 1, Mathf.FloorToInt((scroll.y + viewport.height) / rowHeight));
                int drawn = 0;

                for (int i = first; i <= last; i++)
                {
                    LogListEntry entry = _logEntries[i];
                    if (TimingLogger.IsFileBeingWritten(entry.FullPath)) continue;

                    if (drawn >= budget) break;
                    drawn++;

                    float top = i * rowHeight - scroll.y;
                    DrawLogRow(entry, new Rect(0f, top, viewport.width, rowHeight));
                }
            }
            catch (Exception e)
            {
                ModContext.Logger.Error("Failed to draw log list row: " + e.Message);
            }
            finally
            {
                GUI.EndClip();
            }
        }

        private static float MeasureRowHeight()
        {
            try
            {
                float label = GUI.skin.label.CalcSize(new GUIContent("Ag")).y;
                float button = GUI.skin.button.CalcSize(new GUIContent("Ag")).y;
                float height = Mathf.Max(Mathf.Max(label, button) + 8f, 20f);
                return Mathf.Min(height, 48f);
            }
            catch
            {
                return 24f;
            }
        }

        private static void DrawLogRow(LogListEntry entry, Rect row)
        {
            GUI.Box(row, GUIContent.none);

            float x = row.x + 6f;
            float right = row.xMax - 4f;
            float innerWidth = Mathf.Max(1f, right - x);
            float labelHeight = Mathf.Max(1f, GUI.skin.label.CalcSize(new GUIContent("Ag")).y);
            float rowY = row.y + (row.height - labelHeight) * 0.5f;
            float buttonY = row.y + 1f;
            float buttonHeight = Mathf.Max(1f, row.height - 2f);

            const float sizeWidth = 78f;
            const float timeWidth = 145f;
            const float deleteWidth = 70f;

            string analyzeLabel = i18n.T("Btn_AnalyzeLog");
            string graphLabel = i18n.T("Btn_ViewLogGraph");
            string openFileLabel = i18n.T("Btn_OpenLogFile");
            float analyzeWidth = MeasureButtonWidth(analyzeLabel, 70f);
            float graphWidth = MeasureButtonWidth(graphLabel, 70f);
            float openWidth = MeasureButtonWidth(openFileLabel, 70f);

            float fixedWidth = sizeWidth + timeWidth + deleteWidth + analyzeWidth + graphWidth + openWidth;
            float nameWidth = Mathf.Max(80f, innerWidth - fixedWidth);

            GUI.Label(new Rect(x, rowY, nameWidth, labelHeight),
                TruncateToWidth(entry.FileName, _logNameLabelStyle, nameWidth), _logNameLabelStyle);
            x += nameWidth;

            GUI.Label(new Rect(x, rowY, sizeWidth, labelHeight), FormatFileSize(entry.Length));
            x += sizeWidth;

            GUI.Label(new Rect(x, rowY, timeWidth, labelHeight), FormatTimestamp(entry.Timestamp));
            x += timeWidth;

            if (GUI.Button(new Rect(x, buttonY, analyzeWidth, buttonHeight), analyzeLabel))
                OpenLogInAnalyzer(entry.FullPath);
            x += analyzeWidth;

            if (GUI.Button(new Rect(x, buttonY, graphWidth, buttonHeight), graphLabel))
                OpenLogGraphWindow(entry.FullPath);
            x += graphWidth;

            if (GUI.Button(new Rect(x, buttonY, openWidth, buttonHeight), openFileLabel))
                OpenLogFile(entry.FullPath);
            x += openWidth;

            bool deletePending;
            lock (DeleteSync) deletePending = _deletePending.Contains(entry.FullPath);
            bool deleteArmed = IsDeleteArmed(entry.FullPath);
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && !deletePending;
            GUIStyle deleteStyle = deleteArmed ? _deleteArmedButtonStyle : _deleteButtonStyle;
            string deleteLabel = deletePending ? i18n.T("Btn_DeletingLog") : i18n.T("Btn_DeleteLog");
            if (GUI.Button(new Rect(x, buttonY, deleteWidth, buttonHeight), deleteLabel, deleteStyle))
                HandleDeleteClick(entry.FullPath);
            GUI.enabled = previousEnabled;
        }

        private static void EnsureStyles()
        {
            if (_deleteButtonStyle != null) return;
            _deleteButtonStyle = new GUIStyle(GUI.skin.button);
            _deleteArmedButtonStyle = new GUIStyle(_deleteButtonStyle);
            _warningLabelStyle = new GUIStyle(GUI.skin.label);
            _logNameLabelStyle = new GUIStyle(GUI.skin.label);
            _logNameLabelStyle.wordWrap = false;
            _logNameLabelStyle.clipping = TextClipping.Clip;
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
            bool cacheDirty = false;
            try
            {
                EnsureMetaCacheLoaded();

                if (!string.IsNullOrWhiteSpace(logDir) && Directory.Exists(logDir))
                {
                    string[] files = Directory.GetFiles(logDir);
                    for (int i = 0; i < files.Length; i++)
                    {
                        string file = files[i];
                        string lower = file.ToLowerInvariant();
                        if (!lower.EndsWith(".json") && !lower.EndsWith(".tlog") && !lower.EndsWith(".tlog.gz")) continue;
                        if (TimingLogger.IsFileBeingWritten(file)) continue;

                        LogListEntry entry = ReadLogEntry(file, ref cacheDirty);
                        if (entry != null) result.Entries.Add(entry);
                    }
                }

                if (_cacheLoaded) PruneMetaCache(ref cacheDirty);
            }
            catch (Exception e)
            {
                ModContext.Logger.Error("Failed to scan log directory: " + e.Message);
            }

            if (cacheDirty) SaveMetaCache();

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
            
            result.Entries.Sort(CompareLogEntries);
            _logEntries.Clear();
            _logEntries.AddRange(result.Entries);
            _logListDirectory = result.Directory;
            _logListDirty = false;
        }

        private static LogListEntry ReadLogEntry(string filePath, ref bool cacheDirty)
        {
            try
            {
                var info = new FileInfo(filePath);
                long length = info.Length;
                long ticks = info.LastWriteTimeUtc.Ticks;

                var entry = new LogListEntry
                {
                    FullPath = filePath,
                    FileName = info.Name,
                    LastWriteTime = info.LastWriteTime,
                    Length = length
                };

                MetaCacheEntry cached;
                if (_cacheLoaded && MetaCache.TryGetValue(filePath, out cached) &&
                    cached.Length == length && cached.LastWriteTicks == ticks)
                {
                    entry.ApplyCached(length, ticks, cached.SongName, cached.Timestamp);
                    return entry;
                }

                ReadLogMetadata(filePath, entry);

                CacheMeta(filePath, new MetaCacheEntry
                {
                    Length = length,
                    LastWriteTicks = ticks,
                    SongName = entry.SongName,
                    Timestamp = entry.Timestamp
                }, ref cacheDirty);
                return entry;
            }
            catch
            {
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
                    ReadJsonMetadata(filePath, entry);
                    return;
                }

                using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (Stream input = lower.EndsWith(".tlog.gz") ? (Stream)new GZipStream(fs, CompressionMode.Decompress) : fs)
                using (var reader = new BinaryReader(input, System.Text.Encoding.UTF8))
                {
                    byte[] magic = reader.ReadBytes(4);
                    if (magic.Length < 4 || magic[0] != 'T' || magic[1] != 'S' || magic[2] != 'M' || magic[3] != 'Z') return;
                    reader.ReadByte();
                    entry.Timestamp = reader.ReadInt64();
                    entry.SongName = reader.ReadString();
                }
            }
            catch
            { }
        }

        private static void ReadJsonMetadata(string filePath, LogListEntry entry)
        {
            try
            {
                byte[] probe = new byte[MetaHeaderProbeBytes];
                int read;
                using (var fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    read = ReadUpTo(fs, probe);
                }

                string text = new UTF8Encoding(false).GetString(probe, 0, read);
                entry.Timestamp = ReadJsonLongField(text, "timestamp", entry.Timestamp);
                string song = ReadJsonStringField(text, "songName");
                if (!string.IsNullOrEmpty(song)) entry.SongName = song;

                if (read >= probe.Length && entry.Timestamp < 0 && string.IsNullOrEmpty(entry.SongName))
                {
                    entry.Timestamp = TimestampFromFileName(entry.FileName);
                }
            }
            catch
            { }
        }

        private static int ReadUpTo(Stream stream, byte[] buffer)
        {
            int total = 0;
            while (total < buffer.Length)
            {
                int n = stream.Read(buffer, total, buffer.Length - total);
                if (n <= 0) break;
                total += n;
            }
            return total;
        }

        private static string ReadJsonStringField(string text, string key)
        {
            int at = FindField(text, key);
            if (at < 0) return null;

            int colon = text.IndexOf(':', at);
            if (colon < 0) return null;

            int start = text.IndexOf('"', colon + 1);
            if (start < 0) return null;

            var sb = new System.Text.StringBuilder();
            for (int i = start + 1; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\\') { if (i + 1 < text.Length) { sb.Append(text[++i]); } continue; }
                if (c == '"') return sb.ToString();
                sb.Append(c);
            }
            return null;
        }

        private static long ReadJsonLongField(string text, string key, long fallback)
        {
            int at = FindField(text, key);
            if (at < 0) return fallback;

            int colon = text.IndexOf(':', at);
            if (colon < 0) return fallback;

            int i = colon + 1;
            while (i < text.Length && (text[i] == ' ' || text[i] == '\t')) i++;

            int start = i;
            if (i < text.Length && (text[i] == '-' || text[i] == '+')) i++;
            while (i < text.Length && char.IsDigit(text[i])) i++;
            if (i == start) return fallback;

            long value;
            return long.TryParse(text.Substring(start, i - start), out value) ? value : fallback;
        }

        private static int FindField(string text, string key)
        {
            string needle = "\"" + key + "\"";
            int index = text.IndexOf(needle, StringComparison.Ordinal);
            while (index >= 0)
            {
                int after = index + needle.Length;
                int colon = text.IndexOf(':', after);
                if (colon < 0) return -1;
                int quote = text.IndexOf('"', after);
                if (quote < 0 || quote > colon) return index;
                index = text.IndexOf(needle, index + 1, StringComparison.Ordinal);
            }
            return -1;
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

        private static DateTime SafeFromTicks(long ticks)
        {
            try { return new DateTime(ticks, DateTimeKind.Utc).ToLocalTime(); }
            catch { return DateTime.MinValue; }
        }

        private static string MetaCachePath => Path.Combine(GetCacheDirectory(), "logmeta.cache");

        private static string GetCacheDirectory()
        {
            string root;
            try { root = Path.GetFullPath(Path.Combine(Application.dataPath, "..")); }
            catch { root = Path.GetTempPath(); }
            return Path.Combine(root, "Mods", "TimingShow", "Cache");
        }

        private static void EnsureMetaCacheLoaded()
        {
            if (_cacheLoaded) return;
            _cacheLoaded = true;

            try
            {
                string path = MetaCachePath;
                if (!File.Exists(path)) return;

                using (var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var reader = new BinaryReader(fs, System.Text.Encoding.UTF8))
                {
                    if (reader.ReadInt32() != MetaCacheVersion) return;

                    int count = reader.ReadInt32();
                    if (count <= 0 || count > 100000) return;

                    for (int i = 0; i < count; i++)
                    {
                        string file = reader.ReadString();
                        long length = reader.ReadInt64();
                        long ticks = reader.ReadInt64();
                        long timestamp = reader.ReadInt64();
                        string song = reader.ReadString();
                        MetaCache[file] = new MetaCacheEntry
                        {
                            Length = length,
                            LastWriteTicks = ticks,
                            Timestamp = timestamp,
                            SongName = song
                        };
                    }
                }
            }
            catch (Exception e)
            {
                ModContext.Logger.Log("Log metadata cache unreadable, rebuilding: " + e.Message);
                MetaCache.Clear();
            }
        }

        private static void SaveMetaCache()
        {
            try
            {
                string dir = GetCacheDirectory();
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                string path = MetaCachePath;
                using (var fs = File.Create(path))
                using (var writer = new BinaryWriter(fs, System.Text.Encoding.UTF8))
                {
                    writer.Write(MetaCacheVersion);
                    writer.Write(MetaCache.Count);
                    foreach (KeyValuePair<string, MetaCacheEntry> pair in MetaCache)
                    {
                        writer.Write(pair.Key);
                        writer.Write(pair.Value.Length);
                        writer.Write(pair.Value.LastWriteTicks);
                        writer.Write(pair.Value.Timestamp);
                        writer.Write(pair.Value.SongName ?? string.Empty);
                    }
                }
            }
            catch (Exception e)
            {
                ModContext.Logger.Log("Failed to persist log metadata cache: " + e.Message);
            }
        }

        private static void CacheMeta(string filePath, MetaCacheEntry value, ref bool cacheDirty)
        {
            MetaCacheEntry previous;
            if (MetaCache.TryGetValue(filePath, out previous) &&
                previous.Length == value.Length &&
                previous.LastWriteTicks == value.LastWriteTicks &&
                previous.Timestamp == value.Timestamp &&
                string.Equals(previous.SongName, value.SongName, StringComparison.Ordinal))
            {
                return;
            }

            MetaCache[filePath] = value;
            cacheDirty = true;
        }

        private static void PruneMetaCache(ref bool cacheDirty)
        {
            if (MetaCache.Count == 0) return;

            List<string> stale = null;
            foreach (KeyValuePair<string, MetaCacheEntry> pair in MetaCache)
            {
                if (File.Exists(pair.Key)) continue;
                if (stale == null) stale = new List<string>();
                stale.Add(pair.Key);
            }

            if (stale == null) return;

            for (int i = 0; i < stale.Count; i++) MetaCache.Remove(stale[i]);
            cacheDirty = true;
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
                string url = LogAnalyzerBridge.CreateUrl(filePath, ModContext.Settings.AnalyzerBridgePort, ModContext.Settings.AnalyzerBridgeTimeoutSec);
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception e)
            {
                ModContext.Logger.Error("Failed to open log analyzer: " + e.Message);
            }
        }

        private static void OpenLogGraphWindow(string filePath)
        {
            var job = new LogGraphLoad { FilePath = filePath, FileName = SafeFileName(filePath) };

            lock (GraphLoadSync)
            {
                if (_graphLoadPending != null) return;
                if (string.Equals(job.FilePath, _lastGraphOpenedPath, StringComparison.OrdinalIgnoreCase)) return;

                _lastGraphOpenedPath = job.FilePath;
                _graphLoadPending = job;
                _graphLoadCompleted = null;
            }

            ThreadPool.QueueUserWorkItem(_ => LoadLogForGraph(job));
        }

        private static string _lastGraphOpenedPath;

        private static LogGraphLoad _pendingHint;

        private static void LoadLogForGraph(LogGraphLoad job)
        {
            try
            {
                string error;
                if (!File.Exists(job.FilePath))
                {
                    job.Result = LogGraphLoadResult.Missing;
                    job.Error = "file not found";
                }
                else if (TimingLogReader.TryRead(job.FilePath, out TimingLogData data, out error))
                {
                    job.Data = data;
                    job.Result = LogGraphLoadResult.Success;
                }
                else
                {
                    job.Result = LogGraphLoadResult.Failed;
                    job.Error = error;
                }
            }
            catch (Exception e)
            {
                job.Result = LogGraphLoadResult.Failed;
                job.Error = e.Message;
            }

            lock (GraphLoadSync)
            {
                _graphLoadCompleted = job;
                _graphLoadPending = null;
            }
        }

        private static void ApplyLogGraphLoadResult()
        {
            LogGraphLoad job;
            lock (GraphLoadSync)
            {
                job = _graphLoadCompleted;
                _graphLoadCompleted = null;
            }
            if (job == null) return;

            if (job.Result == LogGraphLoadResult.Success && job.Data != null)
            {
                try { LogGraphWindow.Open(job.Data); }
                catch (Exception e) { ModContext.Logger.Error("Failed to open log graph window: " + e.Message); }
                return;
            }

            if (job.Result == LogGraphLoadResult.Missing)
                ModContext.Logger.Error("Log file disappeared before it could be opened: " + job.FilePath);
            else
                ModContext.Logger.Error("Failed to read log for graph: " + (job.Error ?? "unknown error"));
        }

        private static void UpdatePendingHintSnapshot()
        {
            Event current = Event.current;
            if (current != null && current.type != EventType.Layout) return;

            lock (GraphLoadSync) _pendingHint = _graphLoadPending;
        }

        private static void DrawLogGraphPendingOverlay()
        {
            LogGraphLoad job = _pendingHint;
            if (job == null) return;

            GUILayout.Space(4);
            GUILayout.Label(string.Format(i18n.T("LogGraphLoading"), job.FileName), _warningLabelStyle, GUILayout.ExpandWidth(false));
        }

        private static string SafeFileName(string path)
        {
            try { return Path.GetFileName(path); }
            catch { return path; }
        }

        private static void OpenLogFile(string filePath)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = filePath, UseShellExecute = true, Verb = "open" });
            }
            catch (Exception e)
            {
                ModContext.Logger.Error("Failed to open log file: " + e.Message);
            }
        }

        private static float MeasureWidth(string text, GUIStyle style)
        {
            _measureContent.text = text;
            return style.CalcSize(_measureContent).x;
        }

        private static float MeasureButtonWidth(string text, float minWidth)
        {
            return Mathf.Max(minWidth, MeasureWidth(text, GUI.skin.button) + 14f);
        }

        private static string TruncateToWidth(string text, GUIStyle style, float maxWidth)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (MeasureWidth(text, style) <= maxWidth) return text;

            float ellipsisWidth = MeasureWidth(Ellipsis, style);
            if (ellipsisWidth > maxWidth) return string.Empty;

            int low = 0;
            int high = text.Length;
            while (low < high)
            {
                int mid = (low + high + 1) / 2;
                if (MeasureWidth(text.Substring(0, mid), style) + ellipsisWidth <= maxWidth) low = mid;
                else high = mid - 1;
            }
            return low == 0 ? Ellipsis : text.Substring(0, low) + Ellipsis;
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
            
            for (int i = 0; i < completed.Count; i++)
                RemoveLogEntry(completed[i]);

            _logListDirty = true;
        }

        private static void RemoveLogEntry(string fullPath)
        {
            for (int i = _logEntries.Count - 1; i >= 0; i--)
            {
                if (string.Equals(_logEntries[i].FullPath, fullPath, StringComparison.OrdinalIgnoreCase))
                    _logEntries.RemoveAt(i);
            }
        }

        private static void RemoveActiveLogEntries()
        {
            for (int i = _logEntries.Count - 1; i >= 0; i--)
            {
                if (TimingLogger.IsFileBeingWritten(_logEntries[i].FullPath))
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
