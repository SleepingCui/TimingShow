using HarmonyLib;
using System;
using System.Diagnostics;
using UnityEngine.SceneManagement;
using static TimingShow.Patches.TimingCalcPatches;
using TimingShow.HUD;
using TimingShow.Logging;

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
            SessionTimer.Stop();
            _isGamePaused = false;
            ModContext.IsPlaying = false;
            MarginTrackerAddHitPatch.ResetCounts();
            ModContext.SessionOffsets.Clear();
            CalcUR.Reset();
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
                ModContext.LastTiming = 0;
                ModContext.LastAngle = 0;
                ModContext.ResetJudgeState();
                ModContext.SessionOffsets.Clear();
                CalcUR.Reset();
                ModContext.FullXAccHistory.Clear();
                ModContext.XAccVersion++;
                ModContext.ClearTimingScatterSamples();
                ModContext.UIDirty = true;
                JColors.ResetCache();
                MarginTrackerAddHitPatch.ResetCounts();
                HitBump.StopAll();
                BumpTrigger.Reset();

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
                    if (ModContext.SessionOffsets != null) ModContext.SessionOffsets.Clear();

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
                EndSession();
            }
        }

        // quit (general)
        [HarmonyPatch(typeof(scrController), "QuitToMainMenu")]
        public static class QuitToMainMenu_Patch
        {
            public static void Prefix()
            {
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
            }
        }
    }
}
