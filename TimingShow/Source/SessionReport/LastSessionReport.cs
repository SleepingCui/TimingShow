using System;
using System.Collections.Generic;
using UnityEngine;
using TimingShow.HUD.LogGraphWindow;
using TimingShow.Logging;

namespace TimingShow.SessionReport
{
    public static class LastSessionReport
    {
        private static TimingLogData _data;
        private static bool _showingReport;
        private static string _songName;
        private static string _levelPath;
        private static double _bpm;
        private static double _speed = 1.0;
        private static double _pitch = 1.0;
        private static int _playerCount = 1;

        public static bool HasReport
        {
            get { return _data != null && _data.Samples != null && _data.Samples.Count > 0; }
        }

        public static TimingLogData Data
        {
            get { return _data; }
        }
        
        public static void BeginSession(string songName, string levelPath, double bpm, double speed, double pitch)
        {
            _songName = songName;
            _levelPath = levelPath;
            _bpm = bpm > 0.0 ? bpm : 0.0;
            _speed = speed > 0.0 ? speed : 1.0;
            _pitch = pitch > 0.0 ? pitch : 1.0;

            try
            {
                _playerCount = Mathf.Clamp(Players.Count, 1, Players.MaxPlayers);
            }
            catch
            {
                _playerCount = 1;
            }
        }
        
        public static void Capture(string reason)
        {
            try
            {
                if (ModContext.Settings == null) return;

                List<TimingScatterSample> live = ModContext.TimingScatterSamples;
                if (live == null || live.Count == 0) return;

                TimingLogData data = new TimingLogData();
                data.FilePath = null;  
                data.SongName = string.IsNullOrEmpty(_songName) ? "?" : _songName;
                data.LevelPath = _levelPath;
                data.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                data.Bpm = _bpm;
                data.Speed = _speed;
                data.Pitch = _pitch;
                data.IsAngle = false;
                data.FormatVersion = TimingLogger.FormatVersion;
                data.HitMarginVersionText = HitMarginCompat.Version.ToString();
                data.JudgeCodeVersion = HitMarginCompat.JudgeCodeVersion;
                data.Truncated = false;
                data.PlayerCount = ResolvePlayerCount(live);
                data.Samples = new List<TimingScatterSample>(live.Count);
                data.Samples.AddRange(live);

                _data = data;
                ModContext.Logger?.Log($"[SessionReport] captured {data.Samples.Count} hits ({reason})");
            }
            catch (Exception e)
            {
                ModContext.Logger?.Error("[SessionReport] capture failed: " + e.Message);
            }
        }

        public static void ToggleWindow()
        {
            if (_showingReport && LogGraphWindow.IsOpen)
            {
                CloseWindow();
                return;
            }

            OpenWindow();
        }

        public static void OpenWindow()
        {
            if (!HasReport)
            {
                ModContext.Logger?.Log("[SessionReport] no report available yet");
                return;
            }

            LogGraphWindow.Open(_data);
            _showingReport = LogGraphWindow.IsOpen;
        }

        public static void CloseWindow()
        {
            _showingReport = false;
            LogGraphWindow.Close();
        }
        
        public static void CloseOnLevelStart()
        {
            if (_showingReport) CloseWindow();
        }

        public static void Clear()
        {
            CloseWindow();
            _data = null;
            _songName = null;
            _levelPath = null;
            _playerCount = 1;
        }

        private static int ResolvePlayerCount(List<TimingScatterSample> samples)
        {
            int count = _playerCount;
            for (int i = 0; i < samples.Count; i++)
            {
                int slot = samples[i].PlayerId + 1;
                if (slot > count) count = slot;
            }

            return Mathf.Clamp(count, 1, Players.MaxPlayers);
        }
    }
}
