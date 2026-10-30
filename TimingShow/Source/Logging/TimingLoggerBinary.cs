using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using TimingShow.Diagnostics;
using UnityEngine;

namespace TimingShow.Logging
{

    public static class TimingLoggerBinary
    {
        private static BinaryWriter _writer;
        private static GZipStream _gzStream;
        private static FileStream _fs;
        private static CountingStream _logicalCount;
        private static CountingStream _gzCount;
        private static string _currentFilePath;
        private static long _persistedBytes;
        private static long _logicalPersistedBytes;
        private static readonly Stopwatch FlushClock = new Stopwatch();
        private static readonly byte[] MagicBytes = Encoding.UTF8.GetBytes("TSMZ");
        private const byte FormatVersion = TimingLogger.BinaryFormatVersion;
        private static long _prevTimeBits;
        private static long _prevValueBits;
        private static int _hitCount;
        private static bool _isAngle;
        private static int _playerCount = 1;

        public static bool IsFileBeingWritten(string filePath)
        {
            return _writer != null && !string.IsNullOrWhiteSpace(filePath) &&
                string.Equals(_currentFilePath, filePath, StringComparison.OrdinalIgnoreCase);
        }

        public static void StartNewSession(string levelPath, string songName, double bpm, double speed, double pitch, string customDir, int bufferSize)
        {
            CloseSession();

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
                _currentFilePath = Path.Combine(dir, $"{timestamp}_{safeSongName}.tlog.gz");

                int bufferSizeBytes = Math.Max(4, bufferSize) * 1024;
                _fs = new FileStream(_currentFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, bufferSizeBytes);
                _gzCount = new CountingStream(_fs);
                _gzStream = new GZipStream(_gzCount, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: false);
                _logicalCount = new CountingStream(_gzStream);
                _writer = new BinaryWriter(_logicalCount, new UTF8Encoding(false));
                _persistedBytes = 0;
                _logicalPersistedBytes = 0;

                _prevTimeBits = 0;
                _prevValueBits = 0;
                _hitCount = 0;
                _isAngle = ModContext.Settings.Logger_ShowAngle;

                _playerCount = Players.Count;
                if (_playerCount < 1) _playerCount = 1;

                _writer.Write(MagicBytes);
                _writer.Write(FormatVersion);
                _writer.Write(timestamp);
                _writer.Write(safeSongName);
                _writer.Write(levelPath ?? "");
                _writer.Write(bpm);
                _writer.Write(speed);
                _writer.Write(pitch);
                _writer.Write(_isAngle);
                _writer.Write((byte)HitMarginCompat.Version);
                _writer.Write((byte)HitMarginCompat.JudgeCodeVersion);
                _writer.Write((byte)_playerCount);

                FlushWriter(LogWriteStats.FlushReason.Header);
                ModContext.Logger.Log($"created: {_currentFilePath} (binary)");
            }
            catch (Exception ex)
            {
                ModContext.Logger.Error($"Unable to create log file: {ex.Message} (binary)");
                CloseSession();
            }
        }
        
        
        public static void LogHit(double timing, double angle, int rawMarginCode, int judgeCode, bool isXP, int playerId,
            int floorId, double bpm, double speed, double pitch)
        {
            if (_writer == null) return;

            long mark = PerfPhases.Mark();
            long startBytes = _logicalCount != null ? _logicalCount.Written : 0;

            try
            {
                _hitCount++;
                if (playerId < 0 || playerId >= _playerCount) playerId = 0;
                _writer.Write((byte)playerId);
                WriteXorDouble(_writer, ModContext.LastSongTimeMs, ref _prevTimeBits);
                WriteXorDouble(_writer, _isAngle ? angle : timing, ref _prevValueBits);

                VarInt.Write(_writer, rawMarginCode);
                VarInt.Write(_writer, judgeCode);
                _writer.Write((byte)(isXP ? 1 : 0));

                bool hasHitParams = bpm > 0.0 && speed > 0.0 && pitch > 0.0;
                _writer.Write(hasHitParams ? SafeFloat(angle) : float.NaN);
                _writer.Write(hasHitParams ? SafeFloat(bpm) : float.NaN);
                _writer.Write(hasHitParams ? SafeFloat(speed) : float.NaN);
                _writer.Write(hasHitParams ? SafeFloat(pitch) : float.NaN);
                VarInt.Write(_writer, floorId >= 0 ? floorId : -1);
            }
            catch (Exception ex)
            {
                LogWriteStats.WriteErrors++;
                ModContext.Logger.Error($"Failed to write log: {ex.Message}");
            }

            if (mark >= 0L && _logicalCount != null)
            {
                long written = _logicalCount.Written - startBytes;
                if (written > int.MaxValue) written = int.MaxValue;
                LogWriteStats.RecordHit(PerfPhases.Since(mark) * 1000.0, (int)written);
            }
        }

        private static float SafeFloat(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return float.NaN;
            if (value > float.MaxValue) return float.MaxValue;
            if (value < float.MinValue) return float.MinValue;
            return (float)value;
        }

        private static void WriteXorDouble(BinaryWriter writer, double value, ref long previousBits)
        {
            long currentBits = BitConverter.DoubleToInt64Bits(value);
            ulong xor = (ulong)(currentBits ^ previousBits);
            if (xor == 0)
            {
                writer.Write((byte)0);
                return;
            }

            int leadingBytes = CountLeadingZeroBytes(xor);
            int trailingBytes = CountTrailingZeroBytes(xor);
            int significantBytes = 8 - leadingBytes - trailingBytes;

            // Bit 7 marks a non-zero XOR. Bits 4-6 store leading zero bytes,
            // and bits 0-3 store the number of significant bytes.
            byte control = (byte)(0x80 | (leadingBytes << 4) | significantBytes);
            writer.Write(control);

            for (int i = 0; i < significantBytes; i++)
                writer.Write((byte)(xor >> (8 * (trailingBytes + i))));

            previousBits = currentBits;
        }

        private static int CountLeadingZeroBytes(ulong value)
        {
            int count = 0;
            while ((value & 0xFF00000000000000UL) == 0 && count < 8)
            {
                value <<= 8;
                count++;
            }
            return count;
        }

        private static int CountTrailingZeroBytes(ulong value)
        {
            int count = 0;
            while ((value & 0xFFUL) == 0 && count < 8)
            {
                value >>= 8;
                count++;
            }
            return count;
        }

        public static void FlushForReport()
        {
            if (_writer == null) return;
            FlushWriter(LogWriteStats.FlushReason.Report);
        }

        public static void GetGauges(out long logicalBytes, out long compressedBytes, out long bufferedBytes, out double persistAgeSec)
        {
            logicalBytes = _logicalCount != null ? _logicalCount.Written : 0;
            compressedBytes = _gzCount != null ? _gzCount.Written : 0;
            bufferedBytes = logicalBytes - _logicalPersistedBytes;
            if (bufferedBytes < 0) bufferedBytes = 0;
            persistAgeSec = LogWriteStats.LastFlushAgeSec;
        }

        private static void FlushWriter(LogWriteStats.FlushReason reason)
        {
            if (_writer == null) return;

            FlushClock.Restart();
            _writer.Flush();
            _gzStream?.Flush();
            _fs?.Flush();
            FlushClock.Stop();

            _persistedBytes = _gzCount != null ? _gzCount.Written : 0;
            _logicalPersistedBytes = _logicalCount != null ? _logicalCount.Written : 0;
            LogWriteStats.RecordFlush(reason, FlushClock.Elapsed.TotalMilliseconds, _persistedBytes);
        }

        private static void DisposeWriter()
        {
            if (_writer != null)
            {
                _writer.Dispose();
            }
            else
            {
                _gzStream?.Dispose();
                _fs?.Dispose();
            }

            _writer = null;
            _gzStream = null;
            _logicalCount = null;
            _gzCount = null;
            _fs = null;
        }

        public static void CloseSession()
        {
            if (_writer == null) return;

            try
            {
                if (_hitCount == 0)
                {
                    DisposeWriter();
                    if (!string.IsNullOrEmpty(_currentFilePath) && File.Exists(_currentFilePath))
                        File.Delete(_currentFilePath);
                    ModContext.Logger.Log($"Discarded empty session: {_currentFilePath} (binary)");
                    _currentFilePath = null;
                    return;
                }

                FlushWriter(LogWriteStats.FlushReason.Close);
                double flushMs = LogWriteStats.LastFlushMs;

                FlushClock.Restart();
                DisposeWriter();
                FlushClock.Stop();
                LogWriteStats.RecordClose(flushMs, FlushClock.Elapsed.TotalMilliseconds);

                ModContext.Logger.Log($"Successfully closed session: {_currentFilePath} (binary)");
            }
            catch (Exception e)
            {
                ModContext.Logger.Error($"Err closing log session: {e.Message} (binary)");
            }
            finally
            {
                DisposeWriter();
                _currentFilePath = null;
            }
        }
    }
}
