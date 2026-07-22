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
        private const byte FormatVersion = 1;

        public static void StartNewSession(string levelPath, string songName, string customDir, int bufferSizeKB)
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
                }

                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                _currentFilePath = Path.Combine(dir, $"{timestamp}_{safeSongName}.tlog.gz");

                int bufferSizeBytes = Math.Max(4, bufferSizeKB) * 1024;
                _fs = new FileStream(_currentFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, bufferSizeBytes);
                _gzStream = new GZipStream(_fs, System.IO.Compression.CompressionLevel.Fastest, leaveOpen: false);
                _writer = new BinaryWriter(_gzStream, new UTF8Encoding(false));

                _writer.Write(MagicBytes);                 
                _writer.Write(FormatVersion);      
                _writer.Write(timestamp);                 
                _writer.Write(safeSongName ?? "");          
                _writer.Write(levelPath ?? "");       

                _writer.Flush();
                Main.Logger.Log($"created: {_currentFilePath} (binary)");
            }
            catch (Exception ex)
            {
                Main.Logger.Error($"Unable to create log file: {ex.Message} (binary)");
                CloseSession();
            }
        }

        public static void LogHit(double timing, int marginCode)
        {
            if (_writer == null) return;

            try
            {
                _writer.Write(timing);
                _writer.Write(marginCode);
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
                _writer.Flush();
                _gzStream?.Flush();
                Main.Logger.Log($"Successfully closed session: {_currentFilePath} (binary)");
            }
            catch (Exception e)
            {
                Main.Logger.Error($"Err closing log session: {e.Message} (binary)");
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