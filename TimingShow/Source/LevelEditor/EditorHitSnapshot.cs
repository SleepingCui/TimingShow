using System;
using System.Collections.Generic;
using UnityEngine;
using TimingShow.Logging;

namespace TimingShow.LevelEditor
{
    internal sealed class EditorHitSnapshotData
    {
        internal string LevelPath;
        internal string SongName;
        internal int FloorCount;
        internal int PlayerCount;
        internal int SampleCount;
        internal bool Truncated;
        internal long CapturedAtUtcTicks;
        internal TimingScatterSample[] Samples;

        internal bool MatchesEditor(scnEditor editor)
        {
            if (editor == null) return false;

            int currentFloors = 0;
            try { currentFloors = editor.floors != null ? editor.floors.Count : 0; }
            catch (Exception) { }

            if (!string.IsNullOrEmpty(LevelPath))
            {
                string currentPath = null;
                try { currentPath = ADOBase.levelPath; }
                catch (Exception) { }

                if (!string.IsNullOrEmpty(currentPath))
                    return string.Equals(LevelPath, currentPath, StringComparison.OrdinalIgnoreCase);
                
            }

            return FloorCount > 0 && currentFloors == FloorCount;
        }
    }

    internal static class EditorHitSnapshot
    {
        private static EditorHitSnapshotData _pending;

        internal static EditorHitSnapshotData Pending
        {
            get { return _pending; }
        }

        internal static bool HasPending
        {
            get { return _pending != null; }
        }
        
        internal static void CaptureFromSession()
        {
            try
            {
                if (!ModContext.IsPlaying) return;

                List<TimingScatterSample> source = ModContext.TimingScatterSamples;
                if (source == null || source.Count == 0) return;

                string levelPath = "";
                string songName = "";
                int floorCount = 0;
                int playerCount;

                try
                {
                    if (scnGame.instance != null) levelPath = scnGame.instance.levelPath ?? "";
                }
                catch (Exception) { }

                try
                {
                    if (scnGame.instance != null && scnGame.instance.levelData != null)
                        songName = scnGame.instance.levelData.songFilename ?? "";
                }
                catch (Exception) { }

                try
                {
                    if (scnEditor.instance != null && scnEditor.instance.floors != null)
                        floorCount = scnEditor.instance.floors.Count;
                }
                catch (Exception) { }

                try { playerCount = Mathf.Clamp(Players.Count, 1, Players.MaxPlayers); }
                catch (Exception) { playerCount = 1; }

                TimingScatterSample[] copy = new TimingScatterSample[source.Count];
                source.CopyTo(copy, 0);

                _pending = new EditorHitSnapshotData
                {
                    LevelPath = levelPath,
                    SongName = songName,
                    FloorCount = floorCount,
                    PlayerCount = playerCount,
                    SampleCount = copy.Length,
                    Truncated = copy.Length >= Patches.TimingCalcPatches.SampleHardLimit,
                    CapturedAtUtcTicks = DateTime.UtcNow.Ticks,
                    Samples = copy
                };

                EditorHitMarkerController.EnsureHost();
            }
            catch (Exception e)
            {
                _pending = null;
                ModContext.Logger?.Error("Editor hit markers: snapshot failed: " + e.Message);
            }
        }


        internal static bool TryLoadFromLog(string logPath, out int sampleCount, out string errorKey)
        {
            sampleCount = 0;
            errorKey = null;

            try
            {
                scnEditor editor = scnEditor.instance;
                if (editor == null || editor.floors == null || editor.floors.Count == 0 || ModContext.IsPlaying)
                {
                    errorKey = "EditorHits_ImportNotEditor";
                    return false;
                }

                TimingLogData data = TimingLogReader.Read(logPath);
                if (data == null || data.Samples == null || data.Samples.Count == 0)
                {
                    errorKey = "EditorHits_ImportReadFailed";
                    return false;
                }

                int placed = 0;
                for (int i = 0; i < data.Samples.Count; i++)
                {
                    if (data.Samples[i].HasFloorId) placed++;
                }

                if (placed == 0)
                {
                    errorKey = "EditorHits_ImportNoFloor";
                    return false;
                }

                string currentPath = null;
                try { currentPath = ADOBase.levelPath; }
                catch (Exception) { }

                if (!string.IsNullOrEmpty(data.LevelPath) && !string.IsNullOrEmpty(currentPath)
                    && !string.Equals(data.LevelPath, currentPath, StringComparison.OrdinalIgnoreCase))
                {
                    errorKey = "EditorHits_ImportLevelMismatch";
                    return false;
                }

                int floorCount = editor.floors.Count;
                TimingScatterSample[] copy = new TimingScatterSample[data.Samples.Count];
                data.Samples.CopyTo(copy, 0);

                _pending = new EditorHitSnapshotData
                {
                    LevelPath = data.LevelPath ?? "",
                    SongName = data.SongName ?? "",
                    FloorCount = floorCount,
                    PlayerCount = Mathf.Clamp(data.PlayerCount, 1, Players.MaxPlayers),
                    SampleCount = copy.Length,
                    Truncated = data.Truncated,
                    CapturedAtUtcTicks = DateTime.UtcNow.Ticks,
                    Samples = copy
                };

                sampleCount = placed;
                EditorHitMarkerController.EnsureHost();
                return true;
            }
            catch (Exception e)
            {
                errorKey = "EditorHits_ImportReadFailed";
                ModContext.Logger?.Error("Editor hit markers: import failed: " + e.Message);
                return false;
            }
        }

        internal static void Clear()
        {
            _pending = null;
            EditorHitMarkerController.DestroyHost();
        }
    }
}
