using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

namespace TimingShow
{

    public static class TimingLoggerBinary
    {
        private static BinaryWriter _writer;
        private static GZipStream _gzStream;
        private static FileStream _fs;
        private static string _currentFilePath;
        private static readonly byte[] MagicBytes = Encoding.UTF8.GetBytes("TSMZ");
        private const byte FormatVersion = TimingLogger.BinaryFormatVersion;
        private static long _prevTimeBits;
        private static long _prevValueBits;
        private static int _hitCount;
        private static bool _isAngle;

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
                _gzStream = new GZipStream(_fs, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: false);
                _writer = new BinaryWriter(_gzStream, new UTF8Encoding(false));

                _prevTimeBits = 0;
                _prevValueBits = 0;
                _hitCount = 0;
                _isAngle = ModContext.Settings.Logger_ShowAngle;

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

                _writer.Flush();
                ModContext.Logger.Log($"created: {_currentFilePath} (binary)");
            }
            catch (Exception ex)
            {
                ModContext.Logger.Error($"Unable to create log file: {ex.Message} (binary)");
                CloseSession();
            }
        }
        
        
        public static void LogHit(double timing, double angle, int rawMarginCode, int judgeCode, bool isXP)
        {
            if (_writer == null) return;

            try
            {
                _hitCount++;
                WriteXorDouble(_writer, ModContext.LastSongTimeMs, ref _prevTimeBits);
                WriteXorDouble(_writer, _isAngle ? angle : timing, ref _prevValueBits);

                VarInt.Write(_writer, rawMarginCode);
                VarInt.Write(_writer, judgeCode);
                _writer.Write((byte)(isXP ? 1 : 0));
            }
            catch (Exception ex)
            {
                ModContext.Logger.Error($"Failed to write log: {ex.Message}");
            }
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

        public static void CloseSession()
        {
            if (_writer == null) return;

            try
            {
                if (_hitCount == 0)
                {
                    _writer.Dispose();
                    _writer = null;
                    _gzStream = null;
                    _fs = null;
                    if (!string.IsNullOrEmpty(_currentFilePath) && File.Exists(_currentFilePath))
                        File.Delete(_currentFilePath);
                    ModContext.Logger.Log($"Discarded empty session: {_currentFilePath} (binary)");
                    _currentFilePath = null;
                    return;
                }

                _writer.Flush();
                _gzStream?.Flush();
                ModContext.Logger.Log($"Successfully closed session: {_currentFilePath} (binary)");
            }
            catch (Exception e)
            {
                ModContext.Logger.Error($"Err closing log session: {e.Message} (binary)");
            }
            finally
            {
                _writer?.Dispose();
                _gzStream?.Dispose();
                _fs?.Dispose();

                _writer = null;
                _gzStream = null;
                _fs = null;
                _currentFilePath = null;
            }
        }
    }
}
