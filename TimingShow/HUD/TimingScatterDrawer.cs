using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace TimingShow
{
    public class TimingScatterDrawer : ScatterDrawerBase
    {
        protected override List<TimingScatterSample> ScatterSamples => ModContext.TimingScatterSamples;
        protected override int ScatterVersion => ModContext.TimingScatterVersion;
        protected override bool LevelFinished => ModContext.IsLevelFinished;

        protected override bool GamePaused
        {
            get
            {
                scrController controller = scrController.instance;
                return controller != null && controller.paused;
            }
        }
        
        private bool _bandWindowValid;
        private float _bandPerfectMs;
        private float _bandElPerfectMs;
        private float _bandPassMs;
        private float _bandXpMs;
        
        protected override void DrawBands(VertexHelper vh, float w, float h, Settings settings, float minY, float maxY)
        {
            float rangeY = Mathf.Max(0.01f, maxY - minY);

            if (settings.TimingScatter_ShowJudgeBands)
            {
                float perfectMs, elPerfectMs, passMs;
                GetJudgeWindowMs(settings, out perfectMs, out elPerfectMs, out passMs);
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
                float xpMs = GetXpWindowMs();
                if (xpMs > 0f) DrawBand(vh, w, h, rangeY, minY, -xpMs, xpMs, settings.TimingScatter_BandXpColor);
            }
        }
        
        protected override void RefreshBandWindow()
        {
            Settings settings = ModContext.Settings;
            if (settings == null || (!settings.TimingScatter_ShowJudgeBands && !settings.TimingScatter_ShowXpBand))
            {
                _bandWindowValid = false;
                return;
            }

            float perfectMs = 0f, elPerfectMs = 0f, passMs = 0f;
            if (settings.TimingScatter_ShowJudgeBands) GetJudgeWindowMs(settings, out perfectMs, out elPerfectMs, out passMs);
            float xpMs = settings.TimingScatter_ShowXpBand ? GetXpWindowMs() : 0f;

            bool changed = !_bandWindowValid
                || Mathf.Abs(perfectMs - _bandPerfectMs) > 0.01f
                || Mathf.Abs(elPerfectMs - _bandElPerfectMs) > 0.01f
                || Mathf.Abs(passMs - _bandPassMs) > 0.01f
                || Mathf.Abs(xpMs - _bandXpMs) > 0.01f;
            if (!changed) return;

            _bandPerfectMs = perfectMs;
            _bandElPerfectMs = elPerfectMs;
            _bandPassMs = passMs;
            _bandXpMs = xpMs;
            _bandWindowValid = true;
            SetVerticesDirty();
        }

        private void DrawBand(VertexHelper vh, float w, float h, float rangeY, float minY, float fromMs, float toMs, Color color)
        {
            float y0 = Mathf.Clamp01((fromMs - minY) / rangeY) * h;
            float y1 = Mathf.Clamp01((toMs - minY) / rangeY) * h;
            if (y1 - y0 < 0.5f) return;

            DrawQuad(vh, new Vector2(0f, y0), new Vector2(w, y1), color);
        }
        

        internal const float ThresholdLenientBpm = 220f;
        internal const float ThresholdNormalBpm = 310f;
        internal const float ThresholdStrictBpm = 500f;
        private const float PerfectAngleMsScale = 10000f;
        private const float ElPerfectAngleMsScale = 15000f;
        private const float PassAngleMsScale = 20000f;

        private static FieldInfo _gameDifficultyField;
        private static bool _gameDifficultySearched;

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
        
        internal static float GetEffectiveTempoBpm()
        {
            try
            {
                scrController controller = scrController.instance;
                scrConductor conductor = scrController.conductor ?? scrConductor.instance ?? (controller != null && controller.chosenPlanet != null ? controller.chosenPlanet.conductor : null);
                double bpm = conductor != null ? conductor.bpm : 0.0;
                double speed = controller != null && controller.planetarySystem != null ? controller.planetarySystem.speed : 1.0;
                double pitch = conductor != null && conductor.song != null ? conductor.song.pitch : 1.0;
                double tempo = bpm * speed * pitch;
                if (!double.IsNaN(tempo) && tempo > 0.0) return (float)tempo;
            }
            catch
            {
            }

            double fallback = ModContext.LastBpm * ModContext.LastSpeed * ModContext.LastPitch;
            if (double.IsNaN(fallback) || fallback <= 0.0) return 0f;
            return (float)fallback;
        }
        
        internal static void GetJudgeWindowMs(Settings settings, out float perfectMs, out float elPerfectMs, out float passMs)
        {
            float threshold = GetThresholdBpm(settings);
            if (threshold <= 0f) threshold = ThresholdNormalBpm;

            float tempo = GetEffectiveTempoBpm();
            if (tempo <= 0f) tempo = threshold;

            float bpm = Mathf.Min(tempo, threshold);
            if (bpm <= 0f) bpm = ThresholdNormalBpm;

            perfectMs = PerfectAngleMsScale / bpm;
            elPerfectMs = ElPerfectAngleMsScale / bpm;
            passMs = PassAngleMsScale / bpm;
        }
        
        internal static float GetXpWindowMs()
        {
            float tempo = GetEffectiveTempoBpm();
            if (tempo <= 0f) return 0f;

            double xpMs = CalcXP.GetBoundaryMs(tempo);
            if (double.IsNaN(xpMs) || xpMs <= 0.0) return 0f;
            return (float)xpMs;
        }
    }
}
