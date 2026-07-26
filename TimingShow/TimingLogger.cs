using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace TimingShow
{
    public static class TimingLogger
    {
        private static StreamWriter _writer;
        private static string _currentFilePath;
        private static bool _isFirstEntry = true;
        private static int _hitIndex = 0;
        private static bool _isCurrentSessionBinary = false;

        public static bool IsBinarySession => _isCurrentSessionBinary;
        public static long CurrentBufferBytes { get; private set; } = 0;
        public static long TotalBufferBytes => Main.Settings.LogBufferSizeKB * 1024L;
        public static long FlushCount { get; private set; } = 0;

        public static void StartNewSession(string levelPath, string songName, string customDir, int bufferSizeKB)
        {
            CloseSession();

            CurrentBufferBytes = 0;
            FlushCount = 0;

            _isCurrentSessionBinary = Main.Settings.UseBinaryWriter;
            if (_isCurrentSessionBinary)
            {
                TimingLoggerBinary.StartNewSession(levelPath, songName, customDir, bufferSizeKB);
                return;
            }

            _isFirstEntry = true;
            _hitIndex = 0;

            try
            {
                string dir = string.IsNullOrWhiteSpace(customDir) ? Path.Combine(Application.dataPath, "../Mods/TimingShow/Logs") : Path.GetFullPath(Path.Combine(Application.dataPath, "..", customDir));

                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                string safeSongName = "Unknown";
                if (!string.IsNullOrEmpty(songName))
                {
                    safeSongName = Path.GetFileNameWithoutExtension(songName);
                    foreach (char c in Path.GetInvalidFileNameChars()) safeSongName = safeSongName.Replace(c, '_');
                }

                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                _currentFilePath = Path.Combine(dir, $"{timestamp}_{safeSongName}.json");
                FileStream fs = new FileStream(_currentFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                Main.Logger.Log($"created {_currentFilePath}");

                int bufferSizeBytes = Math.Max(4, bufferSizeKB) * 1024;
                _writer = new StreamWriter(fs, new UTF8Encoding(false), bufferSizeBytes);

                string header = "{\n" +
                                $"  \"songName\": \"{JsonEscape(safeSongName)}\",\n" +
                                $"  \"levelPath\": \"{JsonEscape(levelPath ?? "")}\",\n" +
                                $"  \"timestamp\": {timestamp},\n" +
                                (Main.Settings.UseOldJsonFormat ? "  \"offsets\": {" : "  \"offsets\": [");

                _writer.Write(header);
                TrackWrittenBytes(Encoding.UTF8.GetByteCount(header));

                _writer.Flush();
                OnBufferFlushed();
            }
            catch (Exception ex)
            {
                Main.Logger.Error($"unable to create log file: {ex.Message}");
                _writer = null;
            }
        }

        public static void LogHit(double timing, HitMargin margin)
        {
            int marginCode = RDC.auto ? 10 : (Main.Settings.Logger_EnableXPerfect && Main.LastIsXP ? 12 : (int)margin);

            if (_isCurrentSessionBinary)
            {
                TimingLoggerBinary.LogHit(timing, marginCode);
                return;
            }

            if (_writer == null) return;
            try
            {
                _hitIndex++;

                string fmt = "F" + Math.Max(0, Main.Settings.PercLog);
                string formattedTiming = timing.ToString(fmt);
                string contentToWrite = string.Empty;

                if (Main.Settings.UseOldJsonFormat)
                {
                    string lineBreak = !_isFirstEntry ? ",\n" : "\n";
                    contentToWrite = $"{lineBreak}    \"{_hitIndex}\": {{\"v\": {formattedTiming}, \"j\": {marginCode}}}";
                }
                else
                {
                    string prefix = _isFirstEntry ? "" : ",";
                    contentToWrite = $"{prefix}[{formattedTiming},{marginCode}]";
                }

                _writer.Write(contentToWrite);
                _isFirstEntry = false;
                int bytesWritten = Encoding.UTF8.GetByteCount(contentToWrite);
                TrackWrittenBytes(bytesWritten);
            }
            catch (Exception ex)
            {
                Main.Logger.Error($"Failed to write log: {ex.Message}");
            }
        }

        private static void TrackWrittenBytes(int bytes)
        {
            CurrentBufferBytes += bytes;
            if (CurrentBufferBytes >= TotalBufferBytes && TotalBufferBytes > 0)
                OnBufferFlushed();
        }

        private static void OnBufferFlushed()
        {
            FlushCount++;
            CurrentBufferBytes %= TotalBufferBytes; 
        }

        public static void CloseSession()
        {
            if (_isCurrentSessionBinary)
            {
                TimingLoggerBinary.CloseSession();
                _isCurrentSessionBinary = false;
                return;
            }

            if (_writer == null) return;

            try
            {
                string footer = Main.Settings.UseOldJsonFormat ? "\n  }\n}" : "]\n}";
                _writer.Write(footer);
                _writer.Flush();
                OnBufferFlushed();

                Main.Logger.Log($"Successfully closed session: {_currentFilePath}");
            }
            catch (Exception e)
            {
                Main.Logger.Log($"Err closing log session: {e.Message}");
            }
            finally
            {
                _writer.Dispose();
                _writer = null;
                _currentFilePath = null;
                CurrentBufferBytes = 0;
            }
        }

        private static string JsonEscape(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            return str.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}