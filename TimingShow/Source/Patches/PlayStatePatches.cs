using HarmonyLib;
using System;
using System.Diagnostics;
using UnityEngine.SceneManagement;
using static TimingShow.Patches.TimingCalcPatches;
using TimingShow.HUD;
using TimingShow.Logging;
using TimingShow.Diagnostics;

namespace TimingShow.Patches
{
    public static class PlayStatePatches
    {
        private static readonly Stopwatch SessionTimer = new Stopwatch();
        private static bool _isGamePaused;

        public static double GetSessionTimeMs()
        {
            return SessionTimer.IsRunning ? SessionTimer.Elapsed.TotalMilliseconds : -1.0;
        }

        public static void SyncPauseState()
        {
            if (!ModContext.IsPlaying || scrController.instance == null)
                return;

            bool isPaused = scrController.instance.paused;
            if (isPaused == _isGamePaused)
                return;

            _isGamePaused = isPaused;
            if (isPaused)
            {
                SessionTimer.Stop();
                ModContext.Logger.Log("session paused");
            }
            else
            {
                SessionTimer.Start();
                ModContext.Logger.Log("session resumed");
            }
        }

        
        private const string GameplaySceneName = "scnGame";
        
        public static void EndSession()
        {
            PerfStatsRecorder.EndSession("quit");
            SessionTimer.Stop();
            _isGamePaused = false;
            ModContext.IsPlaying = false;
            Players.ResetAll();
            ModContext.ClearTimingScatterSamples();
            TimingLogger.CloseSession();
            HUDMan.Destroy();
        }

        public static void RegisterSceneHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!ModContext.IsEnabled)
                return;
            if (scene.name == GameplaySceneName)
                return;

            LevelEditor.EditorHitSnapshot.Clear();
            EndSession();
        }

        // start playing
        [HarmonyPatch(typeof(scrController), "Start_Rewind")]
        public static class LevelStartPatch
        {
            public static void Postfix()
            {
                SessionTimer.Restart();
                _isGamePaused = false;
                ModContext.IsPlaying = true;
                ModContext.IsLevelFinished = false;

                Players.ResetAll();
                ModContext.Logger?.Log($"coop players = {Players.Count}");

                ModContext.ClearXAccHistory();
                ModContext.XAccVersion++;
                ModContext.ClearTimingScatterSamples();
                LevelEditor.EditorHitSnapshot.Clear();
                ModContext.UIDirty = true;
                JColors.ResetCache();
                HitBump.StopAll();
                BumpTrigger.Reset();

                if (ModContext.Settings != null && ModContext.Settings.Diag_PerfLogEnabled)
                {
                    try
                    {
                        string perfLevelPath = scnGame.instance != null ? scnGame.instance.levelPath : null;
                        string perfSongName = scnGame.instance != null && scnGame.instance.levelData != null ? scnGame.instance.levelData.songFilename : null;
                        PerfStatsRecorder.StartSession(perfLevelPath, perfSongName);
                    }
                    catch (Exception perfEx)
                    {
                        ModContext.Logger.Error("Failed to start perf session: " + perfEx.Message);
                    }
                }

                bool isAuto = RDC.auto;
                bool shouldLogAuto = isAuto && ModContext.Settings.LogAutoplay;
                bool shouldLogPlayer = !isAuto;

                if (!ModContext.IsEnabled || scrController.instance == null || !ModContext.Settings.EnableLogging || (!shouldLogAuto && !shouldLogPlayer))
                {
                    TimingLogger.CloseSession();
                    return;
                }

                try
                {
                    if (scnGame.instance == null || scnGame.instance.levelData == null) return;

                    var controller = scrController.instance;
                    var conductor = scrController.conductor ?? scrConductor.instance ?? (controller != null && controller.chosenPlanet != null ? controller.chosenPlanet.conductor : null);
                    double bpm = conductor != null ? conductor.bpm : 0;
                    double speed = controller != null && controller.planetarySystem != null ? controller.planetarySystem.speed : 1.0;
                    double pitch = conductor != null && conductor.song != null ? conductor.song.pitch : 1.0;

                    TimingLogger.StartNewSession(scnGame.instance.levelPath, scnGame.instance.levelData.songFilename, bpm, speed, pitch, ModContext.Settings.LogDirectory, ModContext.Settings.LogBufferSizeKB);
                }
                catch (Exception e)
                {
                    ModContext.Logger.Error($"Failed to start timing session: {e.Message}");
                }
            }
        }
        
        //pause
        [HarmonyPatch(typeof(scrController), "TogglePauseGame")]
        public static class TogglePauseGamePatch
        {
            public static void Postfix()
            {
                SyncPauseState();
            }
        }

        // quit (editor)
        [HarmonyPatch(typeof(scnEditor), "SwitchToEditMode")]
        public static class scnEditor_SwitchToEditModePatch
        {
            public static void Prefix()
            {
                // Snapshot before EndSession() resets players and clears the samples.
                LevelEditor.EditorHitSnapshot.CaptureFromSession();
                EndSession();
            }
        }

        // quit (general)
        [HarmonyPatch(typeof(scrController), "QuitToMainMenu")]
        public static class QuitToMainMenu_Patch
        {
            public static void Prefix()
            {
                LevelEditor.EditorHitSnapshot.Clear();
                EndSession();
            }
        }

        // ckpoint
        [HarmonyPatch(typeof(scrMarginTracker), "RevertToLastCheckpoint")]
        public static class MarginTrackerRevertPatch
        {
            public static void Postfix(scrMarginTracker __instance)
            {
                ModContext.ClearTimingScatterSamples();
                MarginTrackerAddHitPatch.SyncFromTracker(__instance);
            }
        }

        // fail
        [HarmonyPatch(typeof(scrController), "Fail2Action")]
        public static class FailPatch
        {
            public static void Postfix()
            {
                ModContext.IsLevelFinished = true;
                ModContext.XAccVersion++;
                ModContext.TimingScatterVersion++;
                ModContext.UIDirty = true;
                PerfStatsRecorder.EndSession("fail");
            }
        }

        // land
        [HarmonyPatch(typeof(scrController), "OnLandOnPortal")]
        public static class OnLandOnPortalPatch
        {
            public static void Postfix()
            {
                ModContext.IsLevelFinished = true;
                ModContext.XAccVersion++;
                ModContext.TimingScatterVersion++;
                ModContext.UIDirty = true;
                PerfStatsRecorder.EndSession("complete");
            }
        }
    }
}
