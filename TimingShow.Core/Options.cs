using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Threading;
using Newtonsoft.Json;
using UnityEngine;
using UnityFileDialog;

namespace TimingShow
{
    public static class Options
    {
        private static string _bufferSizeText;
        private static string _maxPointsText;
        private static string _analyzerPortText;
        private static bool _showAdvancedSettings;

        private static bool _foldoutTitleSettings;
        private static bool _foldoutPlanetSettings;
        private static bool _foldoutReplaceSettings;
        private static bool _foldoutDeathSettings; 
        private static bool _foldoutWinSettings;
        private static bool _foldoutTimingHUD;
        private static bool _foldoutURHUD;
        private static bool _foldoutRatioHUD;
        private static bool _foldoutLogging;
        private static bool _foldoutXACCGraph;
        private static bool _showLogList;

        private static GUIStyle _activeButtonStyle;
        private static GUIStyle _richToggleStyle;
        private static GUIStyle _deleteButtonStyle;
        private static GUIStyle _deleteArmedButtonStyle;
        private static readonly List<LogListEntry> _logEntries = new List<LogListEntry>();
        private static readonly Dictionary<string, DateTime> _deleteArmedUntil = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> _deletePending = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<string> _deleteCompleted = new List<string>();
        private static readonly object DeleteSync = new object();
        private static Vector2 _logListScroll;
        private static string _logListDirectory;
        private static float _nextLogListRefresh;
        
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

        public static void OnGUI()
        {
            bool configJustOpened = !ModContext.IsConfigOpen;
            if (configJustOpened)
            {
                _logListDirectory = null;
                _nextLogListRefresh = 0f;
                try { RequestLogScan(GetLogDirectory()); }
                catch (Exception e) { ModContext.Logger.Error("Failed to refresh logs on config open: " + e.Message); }
            }

            if (_activeButtonStyle == null) _activeButtonStyle = new GUIStyle(GUI.skin.button);
            if (_richToggleStyle == null)
            {
                _richToggleStyle = new GUIStyle(GUI.skin.toggle);
                _richToggleStyle.richText = true;
            }
            if (_deleteButtonStyle == null)
            {
                _deleteButtonStyle = new GUIStyle(GUI.skin.button);
                _deleteArmedButtonStyle = new GUIStyle(_deleteButtonStyle);
                SetButtonTextColor(_deleteButtonStyle, Color.white);
                SetButtonTextColor(_deleteArmedButtonStyle, Color.red);
            }

            ProcessDeleteResults();
            ApplyLogScanResults();

            ModContext.LastConfigGuiFrame = Time.frameCount;
            ModContext.UIDirty = true;

            DrawLanguageSettings();
            DrawTitleSettings();
            DrawPlanetSettings();
            DrawDeathAndWinSettings();
            DrawTimingHUD();
            DrawURHUD();
            DrawRatioHUD();
            DrawXACCGraphSettings();
            DrawLoggingSettings();
            DrawSessionControls();
            DrawLogList();
            DrawAdvancedSettings();
        }


        private static void DrawLanguageSettings()
        {
            GUILayout.BeginHorizontal();
            foreach (string langCode in i18n.AvailableLanguages)
            {
                _activeButtonStyle.fontStyle = (ModContext.Settings.Language == langCode) ? FontStyle.Bold : FontStyle.Normal;
                if (GUILayout.Button(langCode, _activeButtonStyle, GUILayout.Width(100)))
                    ModContext.Settings.Language = langCode;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
        }

        private static void DrawTitleSettings()
        {
            FoldoutToggle(i18n.T("Toggle_Title"), ref ModContext.Settings.ShowInSongTitle, ref _foldoutTitleSettings);
            if (ModContext.Settings.ShowInSongTitle && _foldoutTitleSettings)
            {
                SliderInt("Label_Precision", ref ModContext.Settings.Perc1, 0, 5);
                SliderInt("Label_FontSize", ref ModContext.Settings.Title_FontSize, 20, 200);
                Toggle(ref ModContext.Settings.Title_UseJudgeColor, "HUD_UseJudgeColor");
                if (ModContext.Settings.Title_UseJudgeColor)
                {
                    if (!HitMarginCompat.IsGame34)
                        Toggle(ref ModContext.Settings.Title_EnableXPerfect, "Enable_XP", 40);
                }
                Toggle(ref ModContext.Settings.Title_ShowAngle, "Toggle_ShowAngle");
            }
        }

        private static void DrawPlanetSettings()
        {
            bool oldShowOnPlanet = ModContext.Settings.ShowOnPlanet;
            FoldoutToggle(i18n.T("Toggle_Planet"), ref ModContext.Settings.ShowOnPlanet, ref _foldoutPlanetSettings);
            
            if (oldShowOnPlanet != ModContext.Settings.ShowOnPlanet && ModContext.Settings.AutoReloadInEditor)
            {
                if (!ADOBase.isLevelEditor) return;
                ADOBase.RestartScene();
            }

            if (ModContext.Settings.ShowOnPlanet && _foldoutPlanetSettings)
            {
                SliderInt("Label_Precision", ref ModContext.Settings.Perc3, 0, 5);
                SliderInt("Label_FontSize", ref ModContext.Settings.Planet_FontSize, 20, 200);
                Toggle(ref ModContext.Settings.Planet_ShowAngle, "Toggle_ShowAngle");
                if (!HitMarginCompat.IsGame34)
                    Toggle(ref ModContext.Settings.Planet_EnableXPerfect, "Enable_XP");

                string replaceArrow = _foldoutReplaceSettings ? "▲" : "▼";
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(20);
                    if (GUILayout.Button($"{i18n.T("Setting_Title")} {replaceArrow}", GUILayout.ExpandWidth(false)))
                        _foldoutReplaceSettings = !_foldoutReplaceSettings;
                }
                GUILayout.EndHorizontal();

                if (!_foldoutReplaceSettings) return;

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(20);
                    GUILayout.BeginVertical();
                    {
                        Toggle(ref ModContext.Settings.ReplaceFailOverload, "Toggle_FailOverload", 0);
                        Toggle(ref ModContext.Settings.ReplaceTooEarly, "Toggle_TooEarly", 0);
                        Toggle(ref ModContext.Settings.ReplaceVeryEarly, "Toggle_VeryEarly", 0);
                        Toggle(ref ModContext.Settings.ReplaceEarlyPerfect, "Toggle_EarlyPerfect", 0);


                        if (HitMarginCompat.IsGame34)
                            Toggle(ref ModContext.Settings.ReplacePerfectMinus, "Toggle_PerfectMinus", 0);
                        else
                            Toggle(ref ModContext.Settings.ReplacePerfect, "Toggle_Perfect", 0);
                        
                        if (HitMarginCompat.HasNativeXPerfect)
                        {
                            Toggle(ref ModContext.Settings.ReplaceXPerfect, "Toggle_XPerfect", 0);
                            Toggle(ref ModContext.Settings.ReplacePerfectPlus, "Toggle_PerfectPlus", 0);
                        }

                        Toggle(ref ModContext.Settings.ReplaceLatePerfect, "Toggle_LatePerfect", 0);
                        Toggle(ref ModContext.Settings.ReplaceVeryLate, "Toggle_VeryLate", 0);
                        Toggle(ref ModContext.Settings.ReplaceTooLate, "Toggle_TooLate", 0);
                        Toggle(ref ModContext.Settings.ReplaceFailMiss, "Toggle_FailMiss", 0);
                        Toggle(ref ModContext.Settings.ReplaceMultipress, "Toggle_Multipress", 0);
                        Toggle(ref ModContext.Settings.ReplaceOverPress, "Toggle_OverPress", 0);
                        Toggle(ref ModContext.Settings.ReplaceAuto, "Toggle_Auto", 0);
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
            }
        }

        private static void DrawDeathAndWinSettings()
        {
            FoldoutToggle(i18n.T("Toggle_Death"), ref ModContext.Settings.ShowOnDeath, ref _foldoutDeathSettings);
            if (ModContext.Settings.ShowOnDeath && _foldoutDeathSettings)
            {
                SliderInt("Label_Precision", ref ModContext.Settings.Perc3, 0, 5);
                Toggle(ref ModContext.Settings.ShowOnDeath_ShowAvgTiming, "Toggle_Death_AvgTiming");
                Toggle(ref ModContext.Settings.ShowOnDeath_ShowUR, "Toggle_Death_UR");
                Toggle(ref ModContext.Settings.ShowOnDeath_ShowXACC, "Toggle_Death_XACC");
                Toggle(ref ModContext.Settings.ShowOnDeath_ShowRatio, "Toggle_Death_Ratio");
                SliderInt("Label_FontSize", ref ModContext.Settings.ShowOnDeath_FontSize, 20, 200);
            }
            
            FoldoutToggle(i18n.T("Toggle_Win"), ref ModContext.Settings.ShowInWinPage, ref _foldoutWinSettings);
            if (ModContext.Settings.ShowInWinPage && _foldoutWinSettings)
            {
                SliderInt("Label_Precision", ref ModContext.Settings.Perc4, 0, 5);
                Toggle(ref ModContext.Settings.ShowInWinPage_ShowAvgTiming, "Toggle_Death_AvgTiming");
                Toggle(ref ModContext.Settings.ShowInWinPage_ShowUR, "Toggle_Death_UR");
                Toggle(ref ModContext.Settings.ShowInWinPage_ShowRatio, "Toggle_Death_Ratio");
                SliderInt("Label_FontSize", ref ModContext.Settings.ShowInWinPage_FontSize, 20, 200);
            }
        }

        private static void DrawTimingHUD()
        {
            ToggleFold(i18n.T("Toggle_TimingHUD"), ref ModContext.Settings.ShowTimingHUD, ref _foldoutTimingHUD);
            if (ModContext.Settings.ShowTimingHUD && _foldoutTimingHUD)
            {
                HUDBase(
                    ref ModContext.Settings.HUD_x, ref ModContext.Settings.HUD_y, ref ModContext.Settings.HUD_scale,
                    ref ModContext.Settings.HUD_bold, ref ModContext.Settings.HUD_align, ref ModContext.Settings.HUD_Format,
                    ref ModContext.Settings.PercHUD
                );
                Toggle(ref ModContext.Settings.HUD_UseJudgeColor, "HUD_UseJudgeColor");
                if (ModContext.Settings.HUD_UseJudgeColor)
                {
                    if (!HitMarginCompat.IsGame34)
                        Toggle(ref ModContext.Settings.HUD_EnableXPerfect, "Enable_XP", 40);
                }
                Toggle(ref ModContext.Settings.HUD_ShowAngle, "Toggle_ShowAngle");
            }
        }

        private static void DrawURHUD()
        {
            ToggleFold(i18n.T("Toggle_URHUD"), ref ModContext.Settings.ShowURHUD, ref _foldoutURHUD);
            if (ModContext.Settings.ShowURHUD && _foldoutURHUD)
            {
                HUDBase(
                    ref ModContext.Settings.URHUD_x, ref ModContext.Settings.URHUD_y, ref ModContext.Settings.URHUD_scale,
                    ref ModContext.Settings.URHUD_bold, ref ModContext.Settings.URHUD_align, ref ModContext.Settings.URHUD_Format,
                    ref ModContext.Settings.PercURHUD
                );
            }
        }

        private static void DrawRatioHUD()
        {
            ToggleFold(i18n.T("Toggle_RatioHUD"), ref ModContext.Settings.ShowRatioHUD, ref _foldoutRatioHUD);
            if (ModContext.Settings.ShowRatioHUD && _foldoutRatioHUD)
            {
                HUDBase(
                    ref ModContext.Settings.RatioHUD_x, ref ModContext.Settings.RatioHUD_y, ref ModContext.Settings.RatioHUD_scale,
                    ref ModContext.Settings.RatioHUD_bold, ref ModContext.Settings.RatioHUD_align, ref ModContext.Settings.RatioHUD_Format,
                    ref ModContext.Settings.PercRatioHUD
                );
                RatioModeButtons();
            }
        }

        private static void DrawXACCGraphSettings()
        {
            ToggleFold(i18n.T("Toggle_XACCGraph"), ref ModContext.Settings.ShowXACCGraph, ref _foldoutXACCGraph);
            if (ModContext.Settings.ShowXACCGraph && _foldoutXACCGraph)
            {
                Toggle(ref ModContext.Settings.XACCGraph_ShowEnd, "Toggle_ShowEnd");
                SliderFloat("Label_XOffset", ref ModContext.Settings.XACCGraph_X, 0.0f, 1.0f);
                SliderFloat("Label_YOffset", ref ModContext.Settings.XACCGraph_Y, 0.0f, 1.0f);
                SliderFloat("Label_Scale", ref ModContext.Settings.XACCGraph_Scale, 0.2f, 3.0f);
                IntField("Label_MaxPoints", ref _maxPointsText, ref ModContext.Settings.XACCGraph_MaxPoints, 20, 5000, 250);

                ColorPicker(i18n.T("Label_BgColor"), ref ModContext.Settings.XACCGraph_BgColor);
                ColorPicker(i18n.T("Label_LineColor"), ref ModContext.Settings.XACCGraph_LineColor);
                ColorPicker(i18n.T("Label_GridColor"), ref ModContext.Settings.XACCGraph_GridColor);
                ColorPicker(i18n.T("Label_AxisTextColor"), ref ModContext.Settings.XACCGraph_AxisTextColor);
                ColorPicker(i18n.T("Label_InfoTextColor"), ref ModContext.Settings.XACCGraph_ValueTextColor);
            }
        }

        private static void DrawLoggingSettings()
        {
            GUILayout.BeginVertical();
            {
                ToggleFold(i18n.T("Toggle_Logging"), ref ModContext.Settings.EnableLogging, ref _foldoutLogging);

                if (ModContext.Settings.EnableLogging && _foldoutLogging)
                {
                    SliderInt("Label_Precision", ref ModContext.Settings.PercLog, 0, 5);
                    if (!HitMarginCompat.IsGame34)
                        Toggle(ref ModContext.Settings.Logger_EnableXPerfect, "Enable_XP");
                    Toggle(ref ModContext.Settings.Logger_ShowAngle, "Toggle_ShowAngle");
                    Toggle(ref ModContext.Settings.LogAutoplay, "Toggle_LogAutoplay");
                    Toggle(ref ModContext.Settings.UseJsonWriter, "Toggle_UseJsonWriter");

                    // logdir
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    GUILayout.Label(i18n.T("Label_LogDir"), GUILayout.Width(140));
                    string absolutePath = AbsLogPath(ModContext.Settings.LogDirectory);
                    string displayPath = string.IsNullOrWhiteSpace(absolutePath) ? "None" : absolutePath;
                    GUILayout.Label(displayPath, GUILayout.MinWidth(280), GUILayout.MaxWidth(480));
                    
                    if (GUILayout.Button(i18n.T("Btn_Browse"), GUILayout.Width(70)))
                    {
                        string defaultDir = GetLogDirectory();
                        string selectedFolder = FileBrowser.PickFolder(defaultDir, "Folder", new string[0], i18n.T("Label_LogDir"));
                        if (!string.IsNullOrEmpty(selectedFolder))
                        {
                            ModContext.Settings.LogDirectory = Path.GetFullPath(selectedFolder);
                        }
                    }
                    GUILayout.EndHorizontal();
                    
                    // lbl buffersize
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    GUILayout.Label(i18n.T("Label_BufferSize"), GUILayout.Width(140));

                    if (_bufferSizeText == null) _bufferSizeText = ModContext.Settings.LogBufferSizeKB.ToString();
                    string newBufferSizeText = GUILayout.TextField(_bufferSizeText, GUILayout.Width(80));
                    if (newBufferSizeText != _bufferSizeText)
                    {
                        if (int.TryParse(newBufferSizeText, out int parsedVal) && parsedVal >= 8 && parsedVal <= 102400)
                        {
                            _bufferSizeText = newBufferSizeText;
                            ModContext.Settings.LogBufferSizeKB = parsedVal;
                        }
                        else
                        {
                            _bufferSizeText = "64";
                            ModContext.Settings.LogBufferSizeKB = 64;
                        }
                    }
                    GUILayout.EndHorizontal();
                }

                // btn openlogs
                GUILayout.Space(10);
                if (GUILayout.Button(i18n.T("Btn_OpenLogs"), GUILayout.Width(150)))
                {
                    try
                    {
                        string logDir = GetLogDirectory();
                        if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
                        Process.Start(new ProcessStartInfo() { FileName = logDir, UseShellExecute = true, Verb = "open" });
                    }
                    catch (Exception e)
                    {
                        ModContext.Logger.Error(e.Message);
                    }
                }

            }
            GUILayout.EndVertical();
        }

        private static void DrawLogList()
        {
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

        private static void DrawSessionControls()
        {
            if (GUILayout.Button(i18n.T("Btn_Reset"), GUILayout.Width(150)))
            {
                ModContext.SessionOffsets.Clear();
                ModContext.ResetJudgeState();
                ModContext.LastTiming = 0;
                ModContext.LastAngle = 0;
                Patches.TimingCalcPatches.MarginTrackerAddHitPatch.ResetCounts();
            }
        }

        private static void DrawAdvancedSettings()
        {
            string foldoutArrow = _showAdvancedSettings ? "▲" : "▼";
            if (GUILayout.Button($"{i18n.T("Btn_Advanced")} {foldoutArrow}", GUILayout.Width(150)))
                _showAdvancedSettings = !_showAdvancedSettings;

            if (!_showAdvancedSettings) return;

            GUILayout.BeginVertical();
            {
                GUILayout.Space(5);
                
                // hookmode
                XPerfectBridge.HookState currentState = XPerfectBridge.CurrentState;
                string statusDisplayText;
                switch (currentState)
                {
                    case XPerfectBridge.HookState.Success:
                        statusDisplayText = $"<color=#55FF55> ({i18n.T("Status_HookSuccess")})</color>";
                        break;
                    case XPerfectBridge.HookState.Failed:
                        statusDisplayText = $"<color=#FF5555> ({i18n.T("Status_HookFailed")}{XPerfectBridge.LastErrorMessage})</color>";
                        break;
                    case XPerfectBridge.HookState.NotApplicable:
                        statusDisplayText = $"<color=#55CCFF> ({i18n.T("Status_HookNotApplicable")})</color>";
                        break;
                    case XPerfectBridge.HookState.Disabled:
                    default:
                        statusDisplayText = string.Empty;
                        break;
                }
                
                bool hookSupported = XPerfectBridge.IsSupported;
                bool prevGuiEnabled = GUI.enabled;
                GUI.enabled = hookSupported;

                bool newHookMode = ToggleWithDescription(
                    ModContext.Settings.UseHookMode,
                    "Toggle_HookMode",
                    "Desc_HookMode",
                    extraLabelHtml: statusDisplayText
                );

                GUI.enabled = prevGuiEnabled;

                if (hookSupported && newHookMode != ModContext.Settings.UseHookMode)
                {
                    ModContext.Settings.UseHookMode = newHookMode;
                    if (newHookMode) XPerfectBridge.TryInit(force: true);
                    else XPerfectBridge.UnloadHook();
                }

                GUILayout.Space(5);

                //autoreload
                ModContext.Settings.AutoReloadInEditor = ToggleWithDescription(
                    ModContext.Settings.AutoReloadInEditor,
                    "Toggle_AutoReloadInEditor",
                    "Desc_AutoReloadInEditor"
                );

                GUILayout.Space(5);

                bool previousAnalyzerEnabled = ModContext.Settings.AnalyzerBridgeEnabled;
                bool analyzerEnabled = ToggleWithDescription(
                    ModContext.Settings.AnalyzerBridgeEnabled,
                    "Toggle_AnalyzerBridge",
                    "Desc_AnalyzerBridge"
                );
                ModContext.Settings.AnalyzerBridgeEnabled = analyzerEnabled;
                if (previousAnalyzerEnabled && !analyzerEnabled)
                    LogAnalyzerBridge.Stop();

                if (analyzerEnabled)
                {
                    IntField("Label_AnalyzerPort", ref _analyzerPortText, ref ModContext.Settings.AnalyzerBridgePort, 0, 65535, 0, 160);
                }
            }
            GUILayout.EndVertical();
        }
        

        #region UI Components

        private static void FoldoutToggle(string label, ref bool toggle, ref bool foldout)
        {
            GUILayout.BeginHorizontal();
            toggle = GUILayout.Toggle(toggle, label, GUILayout.ExpandWidth(false));

            if (toggle)
            {
                GUILayout.Space(10);
                string arrow = foldout ? "▲" : "▼";
                if (GUILayout.Button(arrow, GUILayout.Width(28), GUILayout.Height(18)))
                {
                    foldout = !foldout;
                }
            }
            GUILayout.EndHorizontal();
        }

        private static void ToggleFold(string label, ref bool toggle, ref bool foldout)
        {
            FoldoutToggle(label, ref toggle, ref foldout);
        }

        private static void SettingFold(string label, ref bool toggle, ref int precision, ref bool foldout)
        {
            FoldoutToggle(label, ref toggle, ref foldout);

            if (toggle)
            {
                SliderInt("Label_Precision", ref precision, 0, 5);
            }
        }

        private static void SettingRow(string label, ref bool toggle, ref int precision)
        {
            toggle = GUILayout.Toggle(toggle, label);
            if (toggle)
            {
                SliderInt("Label_Precision", ref precision, 0, 5);
            }
        }

        private static bool ToggleWithDescription(bool value, string labelKey, string descKey, string extraDescriptionHtml = "", float indent = 20, string extraLabelHtml = "")
        {
            bool newValue = GUILayout.Toggle(value, i18n.T(labelKey) + extraLabelHtml, _richToggleStyle);
            IndentedLabel($"<color=#888888>{i18n.T(descKey)}</color>{extraDescriptionHtml}", indent);
            return newValue;
        }

        private static void IndentedLabel(string text, float indent = 20)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(indent);
            GUILayout.Label(text);
            GUILayout.EndHorizontal();
        }

        private static void HUDBase(ref float x, ref float y, ref float scale, ref bool bold, ref int align, ref string format, ref int prec)
        {
            SliderFloat("Label_XOffset", ref x, -0.5f, 0.5f);
            SliderFloat("Label_YOffset", ref y, -0.5f, 0.5f);
            SliderFloat("Label_Scale", ref scale, 0.2f, 3.0f);
            Toggle(ref bold, "Toggle_Bold");
            AlignButtons(ref align);

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label(i18n.T("Label_Format"), GUILayout.Width(100));
            format = GUILayout.TextField(format, GUILayout.Width(200));
            GUILayout.EndHorizontal();

            SliderInt("Label_Precision", ref prec, 0, 5);
        }

        private static void SliderFloat(string labelKey, ref float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label(i18n.T(labelKey) + $"{value:F2}", GUILayout.Width(120));
            value = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(120));
            GUILayout.EndHorizontal();
        }

        private static void SliderInt(string labelKey, ref int value, int min, int max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label(i18n.T(labelKey) + $"{value}", GUILayout.Width(120));
            value = Mathf.RoundToInt(GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(100)));
            GUILayout.EndHorizontal();
        }

        private static void Toggle(ref bool value, string labelKey, float indent = 20)
        {
            GUILayout.BeginHorizontal();
            if (indent > 0) GUILayout.Space(indent);
            value = GUILayout.Toggle(value, i18n.T(labelKey));
            GUILayout.EndHorizontal();
        }

        private static void IntField(string labelKey, ref string text, ref int value, int min, int max, int fallback, float labelWidth = 120)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label(i18n.T(labelKey), GUILayout.Width(labelWidth));
            if (text == null) text = value.ToString();
            string newText = GUILayout.TextField(text, GUILayout.Width(80));
            if (newText != text)
            {
                if (int.TryParse(newText, out int parsed) && parsed >= min && parsed <= max)
                {
                    text = newText;
                    value = parsed;
                }
                else
                {
                    text = fallback.ToString();
                    value = fallback;
                }
            }
            GUILayout.EndHorizontal();
        }
        
        private static void RatioModeButtons()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label(i18n.T("Label_RatioMode"), GUILayout.Width(120));

            string[] labels =
            {
                i18n.T("Btn_RatioNormalPerfect"),
                i18n.T("Btn_RatioPerfectFamily"),
                i18n.T("Btn_RatioXPerfect")
            };
            int[] modes =
            {
                Settings.RatioMode_NormalPerfect,
                Settings.RatioMode_PerfectFamily,
                Settings.RatioMode_XPerfect
            };

            for (int i = 0; i < labels.Length; i++)
            {
                _activeButtonStyle.fontStyle = (ModContext.Settings.Ratio_Mode == modes[i]) ? FontStyle.Bold : FontStyle.Normal;
                if (GUILayout.Button(labels[i], _activeButtonStyle, GUILayout.Width(110)))
                    ModContext.Settings.Ratio_Mode = modes[i];
            }
            GUILayout.EndHorizontal();
        }

        private static void AlignButtons(ref int align)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label(i18n.T("Label_Align"), GUILayout.Width(100));

            string[] labels = { i18n.T("Btn_Left"), i18n.T("Btn_Center"), i18n.T("Btn_Right") };
            for (int i = 0; i < 3; i++)
            {
                _activeButtonStyle.fontStyle = (align == i) ? FontStyle.Bold : FontStyle.Normal;
                if (GUILayout.Button(labels[i], _activeButtonStyle, GUILayout.Width(60)))
                    align = i;
            }
            GUILayout.EndHorizontal();
        }

        private static void ColorPicker(string label, ref Color color)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label(label, GUILayout.Width(100));
            GUILayout.Label("R", GUILayout.Width(15));
            color.r = GUILayout.HorizontalSlider(color.r, 0f, 1f, GUILayout.Width(50));
            GUILayout.Label("G", GUILayout.Width(15));
            color.g = GUILayout.HorizontalSlider(color.g, 0f, 1f, GUILayout.Width(50));
            GUILayout.Label("B", GUILayout.Width(15));
            color.b = GUILayout.HorizontalSlider(color.b, 0f, 1f, GUILayout.Width(50));
            GUILayout.Label("A", GUILayout.Width(15));
            color.a = GUILayout.HorizontalSlider(color.a, 0f, 1f, GUILayout.Width(50));
            GUILayout.EndHorizontal();
        }

        #endregion

        #region Utils

        private static string GetDefaultLogDirectory()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "../Mods/TimingShow/Logs"));
        }

        private static string GetLogDirectory()
        {
            string abs = AbsLogPath(ModContext.Settings.LogDirectory);
            return string.IsNullOrWhiteSpace(abs) ? GetDefaultLogDirectory() : abs;
        }

        private static string AbsLogPath(string logDir)
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

        #endregion
    }
}
