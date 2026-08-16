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
        private static int _hitIndex;
        private static bool _isCurrentSessionBinary;

        public static void StartNewSession(string levelPath, string songName, string customDir, int bufferSize)
        {
            CloseSession();

            _isCurrentSessionBinary = !ModContext.Settings.UseJsonWriter;
            if (_isCurrentSessionBinary)
            {
                TimingLoggerBinary.StartNewSession(levelPath, songName, customDir, bufferSize);
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
                    safeSongName = safeSongName.Replace(' ', '_');
                }

                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                _currentFilePath = Path.Combine(dir, $"{timestamp}_{safeSongName}.json");
                FileStream fs = new FileStream(_currentFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                ModContext.Logger.Log($"created {_currentFilePath}");

                int bufferSizeBytes = Math.Max(4, bufferSize) * 1024;
                _writer = new StreamWriter(fs, new UTF8Encoding(false), bufferSizeBytes);

                _writer.WriteLine("{");
                _writer.WriteLine($"  \"songName\": \"{JsonEscape(safeSongName)}\",");
                _writer.WriteLine($"  \"levelPath\": \"{JsonEscape(levelPath ?? "")}\",");
                _writer.WriteLine($"  \"timestamp\": {timestamp},");

                if (ModContext.Settings.UseOldJsonFormat)
                    _writer.Write("  \"offsets\": {");
                else
                    _writer.Write("  \"offsets\": [");

                _writer.Flush();
            }
            catch (Exception ex)
            {
                ModContext.Logger.Error($"unable to create log file: {ex.Message}");
                _writer = null;
            }
        }

        public static void LogHit(double timing, HitMargin margin)
        {
            int marginCode = RDC.auto ? 10 : (ModContext.Settings.Logger_EnableXPerfect && ModContext.LastIsXP ? 12 : (int)margin);

            if (_isCurrentSessionBinary)
            {
                TimingLoggerBinary.LogHit(timing, marginCode);
                return;
            }


            if (_writer == null) return;
            try
            {
                _hitIndex++;

                string fmt = "F" + Math.Max(0, ModContext.Settings.PercLog);
                string formattedTiming = timing.ToString(fmt);

                if (ModContext.Settings.UseOldJsonFormat)
                {
                    if (!_isFirstEntry)
                        _writer.WriteLine(",");
                    else
                        _writer.WriteLine();

                    _writer.Write($"    \"{_hitIndex}\": {{\"v\": {formattedTiming}, \"j\": {marginCode}}}");
                }
                else
                {
                    string prefix = _isFirstEntry ? "" : ",";
                    _writer.Write(prefix);
                    _writer.Write("[");
                    _writer.Write(formattedTiming);
                    _writer.Write(",");
                    _writer.Write(marginCode);
                    _writer.Write("]");
                }

                _isFirstEntry = false;
            }
            catch (Exception ex)
            {
                ModContext.Logger.Error($"Failed to write log: {ex.Message}");
            }
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
                if (_hitIndex == 0)
                {
                    _writer.Dispose();
                    _writer = null;
                    if (!string.IsNullOrEmpty(_currentFilePath) && File.Exists(_currentFilePath))
                        File.Delete(_currentFilePath);
                    ModContext.Logger.Log($"Discarded empty session: {_currentFilePath}");
                    _currentFilePath = null;
                    return;
                }

                if (ModContext.Settings.UseOldJsonFormat)
                {
                    _writer.WriteLine();
                    _writer.WriteLine("  }");
                }
                else
                {
                    _writer.WriteLine("]");
                }

                _writer.Write("}");
                _writer.Flush();
                ModContext.Logger.Log($"Successfully closed session: {_currentFilePath}");
            }
            catch (Exception e)
            {
                ModContext.Logger.Log($"Err closing log session: {e.Message}");
            }
            finally
            {
                _writer?.Dispose();
                _writer = null;
                _currentFilePath = null;
            }
        }

        private static string JsonEscape(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            return str.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
