using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using ScatterBase = TimingShow.HUD.ScatterDrawerBase.ScatterDrawerBase;

namespace TimingShow.HUD
{
    public class TimingScatterDrawer : ScatterBase
    {
        protected override List<TimingScatterSample> ScatterSamples => ModContext.TimingScatterSamples;
        protected override int ScatterVersion => ModContext.TimingScatterVersion;
        protected override bool LevelFinished => ModContext.IsLevelFinished;

        protected override int ScatterPlayerCount
        {
            get { return Coop.ActiveCount; }
        }

        protected override bool ScatterDistinguishPlayers
        {
            get { return true; }
        }

        protected override bool GamePaused
        {
            get
            {
                scrController controller = scrController.instance;
                return controller != null && controller.paused;
            }
        }
        
        private static readonly Color DefaultBorderColor = new Color(1f, 1f, 1f, 0.35f);

        protected override bool ShowBorder => ModContext.Settings != null && ModContext.Settings.TimingScatter_ShowBorder;
        protected override Color BorderColor => ModContext.Settings != null ? ModContext.Settings.TimingScatter_BorderColor : DefaultBorderColor;

        private const float BandAnimationDuration = 0.4f; 

        private bool _bandWindowValid;
        private bool _bandAnimating;
        private float _bandAnimStartedAt;
        
        private float _bandPerfectMs;
        private float _bandElPerfectMs;
        private float _bandPassMs;
        private float _bandXpMs;
        private float _bandPerfectTarget;
        private float _bandElPerfectTarget;
        private float _bandPassTarget;
        private float _bandXpTarget;
        private float _bandPerfectStart;
        private float _bandElPerfectStart;
        private float _bandPassStart;
        private float _bandXpStart;

        protected override void DrawBands(VertexHelper vh, float w, float h, Settings settings, float minY, float maxY)
        {
            float rangeY = Mathf.Max(0.01f, maxY - minY);

            if (settings.TimingScatter_ShowJudgeBands)
            {
                float perfectMs = _bandPerfectMs;
                float elPerfectMs = _bandElPerfectMs;
                float passMs = _bandPassMs;
                if (passMs > 0f)
                {
                    //45-60
                    DrawBand(vh, w, h, rangeY, minY, -passMs, -elPerfectMs, settings.TimingScatter_BandEarlyLateColor);
                    DrawBand(vh, w, h, rangeY, minY, elPerfectMs, passMs, settings.TimingScatter_BandEarlyLateColor);
                    //30-45
                    DrawBand(vh, w, h, rangeY, minY, -elPerfectMs, -perfectMs, settings.TimingScatter_BandElPerfectColor);
                    DrawBand(vh, w, h, rangeY, minY, perfectMs, elPerfectMs, settings.TimingScatter_BandElPerfectColor);
                    //0-30
                    DrawBand(vh, w, h, rangeY, minY, -perfectMs, perfectMs, settings.TimingScatter_BandPerfectColor);
                }
            }

            if (settings.TimingScatter_ShowXpBand)
            {
                float xpMs = _bandXpMs;
                if (xpMs > 0f) DrawBand(vh, w, h, rangeY, minY, -xpMs, xpMs, settings.TimingScatter_BandXpColor);
            }
        }

        protected override void RefreshBandWindow()
        {
            Settings settings = ModContext.Settings;
            if (settings == null || (!settings.TimingScatter_ShowJudgeBands && !settings.TimingScatter_ShowXpBand))
            {
                _bandWindowValid = false;
                _bandAnimating = false;
                return;
            }

            float targetPerfect = 0f, targetElPerfect = 0f, targetPass = 0f;
            if (settings.TimingScatter_ShowJudgeBands)
                GetJudgeWindowMs(settings, out targetPerfect, out targetElPerfect, out targetPass);
            float targetXp = settings.TimingScatter_ShowXpBand ? GetXpWindowMs() : 0f;

            bool first = !_bandWindowValid;
            bool changed = first
                || Mathf.Abs(targetPerfect - _bandPerfectTarget) > 0.01f
                || Mathf.Abs(targetElPerfect - _bandElPerfectTarget) > 0.01f
                || Mathf.Abs(targetPass - _bandPassTarget) > 0.01f
                || Mathf.Abs(targetXp - _bandXpTarget) > 0.01f;

            if (changed)
            {
                if (first)
                {
                    _bandPerfectMs = targetPerfect;
                    _bandElPerfectMs = targetElPerfect;
                    _bandPassMs = targetPass;
                    _bandXpMs = targetXp;
                }
                else
                {
                    _bandPerfectStart = _bandPerfectMs;
                    _bandElPerfectStart = _bandElPerfectMs;
                    _bandPassStart = _bandPassMs;
                    _bandXpStart = _bandXpMs;
                    _bandAnimStartedAt = Time.unscaledTime;
                }

                _bandPerfectTarget = targetPerfect;
                _bandElPerfectTarget = targetElPerfect;
                _bandPassTarget = targetPass;
                _bandXpTarget = targetXp;
                _bandWindowValid = true;
                _bandAnimating = !first && BandAnimationDuration > 0f;

                if (!_bandAnimating)
                {
                    _bandPerfectMs = targetPerfect;
                    _bandElPerfectMs = targetElPerfect;
                    _bandPassMs = targetPass;
                    _bandXpMs = targetXp;
                }
                SetVerticesDirty();
            }

            if (!_bandAnimating) return;

            float t = Mathf.Clamp01((Time.unscaledTime - _bandAnimStartedAt) / BandAnimationDuration);
            float eased = 1f - (1f - t) * (1f - t) * (1f - t);
            _bandPerfectMs = Mathf.Lerp(_bandPerfectStart, _bandPerfectTarget, eased);
            _bandElPerfectMs = Mathf.Lerp(_bandElPerfectStart, _bandElPerfectTarget, eased);
            _bandPassMs = Mathf.Lerp(_bandPassStart, _bandPassTarget, eased);
            _bandXpMs = Mathf.Lerp(_bandXpStart, _bandXpTarget, eased);
            if (t >= 1f) _bandAnimating = false;
            SetVerticesDirty();
        }

        internal const float ThresholdLenientBpm = 220f;
        internal const float ThresholdNormalBpm = 310f;
        internal const float ThresholdStrictBpm = 500f;
        private const float PassAngleMsScale = 20000f;
        private const float MinWindowMs = 25f;
        private const float PerfectBaseMs = 20f;
        private const float ElPerfectBaseMs = 30f;
        private const float XpMinWindowMs = 16.666666f;

        private static FieldInfo _gameDifficultyField;
        private static bool _gameDifficultySearched;
        private static FieldInfo _gameSpeedTrialField;
        private static bool _gameSpeedTrialSearched;

        internal static float GetThresholdBpm(Settings settings)
        {
            if (settings != null && !settings.TimingScatter_BandAutoWindow)
                return Mathf.Clamp(settings.TimingScatter_BandThresholdBpm, 100f, 600f);

            switch (ReadGameDifficulty())
            {
                case 0: return ThresholdLenientBpm;
                case 1: return ThresholdNormalBpm;
                case 2: return ThresholdStrictBpm;
                default: return ThresholdNormalBpm;
            }
        }
        
        internal static string GetGameDifficultyLabel()
        {
            switch (ReadGameDifficulty())
            {
                case 0: return "Lenient";
                case 1: return "Normal";
                case 2: return "Strict";
                default: return "Unknown";
            }
        }
        
        private static int ReadGameDifficulty()
        {
            try
            {
                if (!_gameDifficultySearched)
                {
                    _gameDifficultySearched = true;
                    System.Type type = System.Type.GetType("GCS, Assembly-CSharp");
                    if (type != null)
                        _gameDifficultyField = type.GetField("difficulty", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                }

                if (_gameDifficultyField == null) return -1;
                object value = _gameDifficultyField.GetValue(null);
                return value == null ? -1 : System.Convert.ToInt32(value);
            }
            catch
            {
                return -1;
            }
        }
        
        internal static float GetWindowBpm()
        {
            try
            {
                scrController controller = scrController.instance;
                scrConductor conductor = scrController.conductor ?? scrConductor.instance ?? (controller != null && controller.chosenPlanet != null ? controller.chosenPlanet.conductor : null);
                double bpm = conductor != null ? conductor.bpm : 0.0;
                double speed = controller != null && controller.planetarySystem != null ? controller.planetarySystem.speed : 1.0;
                double pitch = conductor != null && conductor.song != null ? conductor.song.pitch : 1.0;
                double marginScale = 1.0;
                if (controller != null && controller.currFloor != null)
                {
                    double scale = controller.currFloor.marginScale;
                    if (!double.IsNaN(scale) && scale > 0.0) marginScale = scale;
                }
                double speedTrial = ReadGameSpeedTrial();

                double windowBpm = bpm * pitch * speed / marginScale / speedTrial;
                if (!double.IsNaN(windowBpm) && windowBpm > 0.0) return (float)windowBpm;
            }
            catch
            {
            }

            double fallback = ModContext.LastBpm * ModContext.LastSpeed * ModContext.LastPitch;
            if (double.IsNaN(fallback) || fallback <= 0.0) return 0f;
            return (float)fallback;
        }

        private static float ReadGameSpeedTrial()
        {
            try
            {
                if (!_gameSpeedTrialSearched)
                {
                    _gameSpeedTrialSearched = true;
                    System.Type type = System.Type.GetType("GCS, Assembly-CSharp");
                    if (type != null)
                        _gameSpeedTrialField = type.GetField("currentSpeedTrial", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                }

                if (_gameSpeedTrialField == null) return 1f;
                object value = _gameSpeedTrialField.GetValue(null);
                if (value == null) return 1f;

                float speedTrial = System.Convert.ToSingle(value);
                return !float.IsNaN(speedTrial) && !float.IsInfinity(speedTrial) && speedTrial > 0f
                    ? speedTrial
                    : 1f;
            }
            catch
            {
                return 1f;
            }
        }

        private static float GetPassBaseMs(Settings settings)
        {
            if (settings != null && !settings.TimingScatter_BandAutoWindow)
            {
                float manualBpm = Mathf.Clamp(settings.TimingScatter_BandThresholdBpm, 100f, 600f);
                return PassAngleMsScale / manualBpm;
            }

            switch (ReadGameDifficulty())
            {
                case 0: return 91f; // Lenient
                case 1: return 65f; // Normal
                case 2: return 40f; // Strict
                default: return 65f;
            }
        }

        private static float CalcWindow(float windowBpm, float speedTrial, float angle, float baseWindowMs)
        {
            float safeBpm = Mathf.Max(0.001f, windowBpm);
            float safeSpeedTrial = Mathf.Max(0.001f, speedTrial);
            float angleTerm = 60000f / safeBpm * angle / 180f;
            return Mathf.Max(Mathf.Max(angleTerm, baseWindowMs) / safeSpeedTrial, MinWindowMs);
        }

        internal static void GetJudgeWindowMs(Settings settings, out float perfectMs, out float elPerfectMs, out float passMs)
        {
            float windowBpm = GetWindowBpm();
            if (!(windowBpm > 0f))
            {
                windowBpm = GetThresholdBpm(settings);
                if (!(windowBpm > 0f)) windowBpm = ThresholdNormalBpm;
            }

            float speedTrial = ReadGameSpeedTrial();
            float passBase = GetPassBaseMs(settings);

            perfectMs = CalcWindow(windowBpm, speedTrial, 30f, PerfectBaseMs);
            elPerfectMs = CalcWindow(windowBpm, speedTrial, 45f, ElPerfectBaseMs);
            passMs = CalcWindow(windowBpm, speedTrial, 60f, passBase);
        }

        internal static float GetXpWindowMs()
        {
            float windowBpm = GetWindowBpm();
            if (!(windowBpm > 0f)) return 0f;

            float speedTrial = Mathf.Max(0.001f, ReadGameSpeedTrial());
            float xpMs = 60000f / windowBpm * 12.5f / 180f / speedTrial;
            return Mathf.Max(xpMs, XpMinWindowMs);
        }
        
        internal static void GetLogWindowMs(float windowBpm, float passThresholdBpm,
            out float perfectMs, out float elPerfectMs, out float passMs)
        {
            float threshold = Mathf.Clamp(passThresholdBpm, 100f, 600f);
            float safeBpm = windowBpm > 0f ? windowBpm : threshold;
            float passBase = PassAngleMsScale / threshold;

            perfectMs = CalcWindow(safeBpm, 1f, 30f, PerfectBaseMs);
            elPerfectMs = CalcWindow(safeBpm, 1f, 45f, ElPerfectBaseMs);
            passMs = CalcWindow(safeBpm, 1f, 60f, passBase);
        }

        internal static float GetLogXpWindowMs(float windowBpm)
        {
            if (!(windowBpm > 0f)) return 0f;
            return Mathf.Max(60000f / windowBpm * 12.5f / 180f, XpMinWindowMs);
        }
    }
}
