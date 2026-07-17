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
        private static int _hitIndex = 0;
        private static bool _isFirstEntry = true;

        public static void StartNewSession(string levelPath, string songName)
        {
            CloseSession();
            _hitIndex = 0;
            _isFirstEntry = true;
            try
            {
                string dir = Path.Combine(Application.dataPath, "../Mods/TimingShow/Logs");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                string safeSongName = "Unknown";
                if (!string.IsNullOrEmpty(songName))
                {
                    safeSongName = Path.GetFileNameWithoutExtension(songName);
                    foreach (char c in Path.GetInvalidFileNameChars()) safeSongName = safeSongName.Replace(c, '_');
                }
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                _currentFilePath = Path.Combine(dir, $"{timestamp}_{safeSongName}.json");
                FileStream fs = new FileStream(_currentFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                _writer = new StreamWriter(fs, new UTF8Encoding(false), 65536);
                _writer.WriteLine("{");
                _writer.WriteLine($"  \"songName\": \"{JsonEscape(safeSongName)}\",");
                _writer.WriteLine($"  \"levelPath\": \"{JsonEscape(levelPath ?? "")}\",");
                _writer.WriteLine($"  \"timestamp\": {timestamp},");
                _writer.WriteLine("  \"offsets\": {");
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
            if (_writer == null)
                return;
            try
            {
                _hitIndex++;
                int marginCode = (int)margin;
                string prefix = _isFirstEntry ? "" : ",";
                _isFirstEntry = false;
                _writer.Write(prefix);
                _writer.Write("\n    \"");
                _writer.Write(_hitIndex);
                _writer.Write("\": {\"v\": ");
                _writer.Write(timing.ToString("F4"));
                _writer.Write(", \"j\": ");
                _writer.Write(marginCode);
                _writer.Write("}");
            }
            catch (Exception ex)
            {
                Main.Logger.Error($"Failed to write log: {ex.Message}");
            }
        }

        public static void CloseSession()
        {
            if (_writer == null)
                return;
            try
            {
                _writer.WriteLine();
                _writer.WriteLine("  }");
                _writer.Write("}");
                _writer.Flush();
            }
            catch { }
            finally
            {
                _writer.Dispose();
                _writer = null;
                _currentFilePath = null;
            }
        }

        private static string JsonEscape(string str)
        {
            if (string.IsNullOrEmpty(str))
                return "";
            return str.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}