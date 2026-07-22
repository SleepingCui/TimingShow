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

        public static void StartNewSession(string levelPath, string songName, string customDir, int bufferSizeKB)
        {
            CloseSession();
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

                _writer.WriteLine("{");
                _writer.WriteLine($"  \"songName\": \"{JsonEscape(safeSongName)}\",");
                _writer.WriteLine($"  \"levelPath\": \"{JsonEscape(levelPath ?? "")}\",");
                _writer.WriteLine($"  \"timestamp\": {timestamp},");

                if (Main.Settings.UseOldJsonFormat)
                    _writer.Write("  \"offsets\": {");
                else
                    _writer.Write("  \"offsets\": [");

                _writer.Flush();
            }
            catch (Exception ex)
            {
                Main.Logger.Error($"unable to create log file: {ex.Message}");
                _writer = null;
            }
        }

        public static void LogHit(double timing, HitMargin margin)
        {
            if (_writer == null) return;

            try
            {
                int marginCode = RDC.auto ? 10 : (Main.Settings.Logger_EnableXPerfect && Main.LastIsXP ? 12 : (int)margin);
                _hitIndex++;

                string fmt = "F" + Math.Max(0, Main.Settings.PercLog);
                string formattedTiming = timing.ToString(fmt);

                if (Main.Settings.UseOldJsonFormat)
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
                Main.Logger.Error($"Failed to write log: {ex.Message}");
            }
        }

        public static void CloseSession()
        {
            if (_writer == null) return;

            try
            {
                if (Main.Settings.UseOldJsonFormat)
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
            }
        }

        private static string JsonEscape(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            return str.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}