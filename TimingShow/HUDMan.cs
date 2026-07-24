using UnityEngine;

namespace TimingShow
{
    public static class HUDMan
    {
        public static GameObject hudObject;
        public static TextUI hudInstance;

        public static GameObject urHudObject;
        public static TextUI urHudInstance;

        public static void Destroy()
        {
            if (hudObject != null)
            {
                Object.Destroy(hudObject);
                hudObject = null;
                hudInstance = null;
            }

            if (urHudObject != null)
            {
                Object.Destroy(urHudObject);
                urHudObject = null;
                urHudInstance = null;
            }
        }

        public static void Update()
        {
            bool isPlayBase = Main.IsPlaying && scrController.instance != null && scrController.instance.gameworld && !scrController.instance.paused;

            // timing hud
            bool isTimingPlay = isPlayBase && Main.Settings.ShowTimingHUD;
            if (hudObject == null)
            {
                hudObject = new GameObject("TimingShow_HUD");
                hudInstance = hudObject.AddComponent<TextUI>();
            }
            hudObject.SetActive(isTimingPlay);

            if (isTimingPlay)
            {
                string timing = Main.LastTiming.ToString("F" + Main.Settings.PercHUD);
                if (Main.Settings.HUD_UseJudgeColor)
                {
                    var cond = scrController.instance.chosenPlanet.conductor;
                    Color fColor = CalcXP.XPc(scrController.instance.chosenPlanet, Main.LastTiming, cond.bpm, scrController.instance.planetarySystem.speed, cond.song.pitch, Main.Settings.HUD_EnableXPerfect, Main.LastHitMargin);
                    timing = $"<color=#{ColorUtility.ToHtmlStringRGB(fColor)}>" + timing + "</color>";
                }

                hudInstance.SetText(string.Format(Main.Settings.HUD_Format, timing));
                hudInstance.SetPosition(Main.Settings.HUD_x, Main.Settings.HUD_y);
                hudInstance.SetSize((int)(24 * Main.Settings.HUD_scale));
                hudInstance.text.alignment = hudInstance.ToAlign(Main.Settings.HUD_align);
                hudInstance.text.fontStyle = Main.Settings.HUD_bold ? FontStyle.Bold : FontStyle.Normal;
            }

            // ur hud
            bool isURPlay = isPlayBase && Main.Settings.ShowURHUD;
            if (urHudObject == null)
            {
                urHudObject = new GameObject("TimingShow_URHUD");
                urHudInstance = urHudObject.AddComponent<TextUI>();
            }
            urHudObject.SetActive(isURPlay);

            if (isURPlay)
            {
                double currentUR = CalculateUR(Main.SessionOffsets);
                string urStr = currentUR.ToString("F" + Main.Settings.PercURHUD);

                urHudInstance.SetText(string.Format(Main.Settings.URHUD_Format, urStr));
                urHudInstance.SetPosition(Main.Settings.URHUD_x, Main.Settings.URHUD_y);
                urHudInstance.SetSize((int)(24 * Main.Settings.URHUD_scale));
                urHudInstance.text.alignment = urHudInstance.ToAlign(Main.Settings.URHUD_align);
                urHudInstance.text.fontStyle = Main.Settings.URHUD_bold ? FontStyle.Bold : FontStyle.Normal;
            }
        }

        private static double CalculateUR(System.Collections.Generic.List<double> offsets)
        {
            if (offsets == null || offsets.Count == 0) return 0.0;

            double avg = 0.0;
            int count = offsets.Count;
            for (int i = 0; i < count; i++) avg += offsets[i];
            avg /= count;

            double sumOfSquares = 0.0;
            for (int i = 0; i < count; i++)
            {
                double diff = offsets[i] - avg;
                sumOfSquares += diff * diff;
            }

            double stdDev = System.Math.Sqrt(sumOfSquares / count);
            return stdDev * 10.0;
        }
    }
}