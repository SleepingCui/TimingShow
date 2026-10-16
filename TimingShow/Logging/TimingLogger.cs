using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace TimingShow.Logging
{
    public static class TimingLogger
    {
        public const int FormatVersion = 7;
        public const int BinaryFormatVersion = 8;

        private static StreamWriter _writer;
        private static string _currentFilePath;
        private static int _playerCount = 1;
        private static StringBuilder[] _playerBuffers;
        private static int _hitCount;
        private static bool _isCurrentSessionBinary;
        private static bool _isCurrentSessionAngle;

        public static bool IsFileBeingWritten(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return false;
            if (_isCurrentSessionBinary) return TimingLoggerBinary.IsFileBeingWritten(filePath);
            return _writer != null && string.Equals(_currentFilePath, filePath, StringComparison.OrdinalIgnoreCase);
        }

        public static void StartNewSession(string levelPath, string songName, double bpm, double speed, double pitch, string customDir, int bufferSize)
        {
            CloseSession();

            _isCurrentSessionBinary = !ModContext.Settings.UseJsonWriter;
            if (_isCurrentSessionBinary)
            {
                TimingLoggerBinary.StartNewSession(levelPath, songName, bpm, speed, pitch, customDir, bufferSize);
                return;
            }

            _hitCount = 0;
            _isCurrentSessionAngle = ModContext.Settings.Logger_ShowAngle;

            _playerCount = Players.Count;
            if (_playerCount < 1) _playerCount = 1;
            _playerBuffers = new StringBuilder[_playerCount];
            for (int p = 0; p < _playerCount; p++) _playerBuffers[p] = new StringBuilder(4096);

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
                _writer.WriteLine($"  \"bpm\": {bpm.ToString("R", CultureInfo.InvariantCulture)},");
                _writer.WriteLine($"  \"speed\": {speed.ToString("R", CultureInfo.InvariantCulture)},");
                _writer.WriteLine($"  \"pitch\": {pitch.ToString("R", CultureInfo.InvariantCulture)},");
                _writer.WriteLine($"  \"isAngle\": {(_isCurrentSessionAngle ? "true" : "false")},");
                _writer.WriteLine($"  \"formatVersion\": {FormatVersion},");
                _writer.WriteLine($"  \"hitMarginVersion\": \"{HitMarginCompat.Version}\",");
                _writer.WriteLine($"  \"judgeCodeVersion\": {HitMarginCompat.JudgeCodeVersion},");

                _writer.Flush();
            }
            catch (Exception ex)
            {
                ModContext.Logger.Error($"unable to create log file: {ex.Message}");
                _writer = null;
                _playerBuffers = null;
            }
        }
        
        public static void LogHit(double timing, double angle, int rawMarginCode, HitMan judge, bool isXP)
        {
            int judgeCode = (int)judge;
            
            bool logXP = isXP && (HitMarginCompat.IsGame34 || ModContext.Settings.Logger_EnableXPerfect);

            int playerId = Players.Current.PlayerId;
            if (playerId < 0) playerId = 0;

            if (_isCurrentSessionBinary)
            {
                TimingLoggerBinary.LogHit(timing, angle, rawMarginCode, judgeCode, logXP, playerId);
                return;
            }

            if (_writer == null || _playerBuffers == null) return;
            try
            {
                _hitCount++;

                if (playerId >= _playerCount) playerId = 0;

                string fmt = "F" + Math.Max(0, ModContext.Settings.PercLog);
                string formattedTiming = (_isCurrentSessionAngle ? angle : timing).ToString(fmt);
                string formattedSongTime = double.IsNaN(ModContext.LastSongTimeMs) || double.IsInfinity(ModContext.LastSongTimeMs) || ModContext.LastSongTimeMs < 0
                    ? "null"
                    : ModContext.LastSongTimeMs.ToString("F3", CultureInfo.InvariantCulture);

                StringBuilder buffer = _playerBuffers[playerId];
                if (buffer.Length > 0) buffer.Append(',');
                buffer.Append('[');
                buffer.Append(formattedSongTime);
                buffer.Append(',');
                buffer.Append(formattedTiming);
                buffer.Append(',');
                buffer.Append(rawMarginCode);
                buffer.Append(',');
                buffer.Append(judgeCode);
                buffer.Append(',');
                buffer.Append(logXP ? 1 : 0);
                buffer.Append(']');
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
                if (_hitCount == 0)
                {
                    _writer.Dispose();
                    _writer = null;
                    if (!string.IsNullOrEmpty(_currentFilePath) && File.Exists(_currentFilePath))
                        File.Delete(_currentFilePath);
                    ModContext.Logger.Log($"Discarded empty session: {_currentFilePath}");
                    _currentFilePath = null;
                    return;
                }

                for (int i = 0; i < _playerCount; i++)
                {
                    _writer.Write($"  \"offsets_{i + 1}\": [");
                    if (_playerBuffers != null && i < _playerBuffers.Length && _playerBuffers[i] != null)
                        _writer.Write(_playerBuffers[i].ToString());
                    _writer.Write("]");
                    if (i < _playerCount - 1) _writer.Write(",");
                    _writer.WriteLine();
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
                _playerBuffers = null;
                _isCurrentSessionAngle = false;
            }
        }

        private static string JsonEscape(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            return str.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
