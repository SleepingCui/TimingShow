using UnityEngine;

namespace TimingShow
{
    public static class HUDMan
    {
        public static GameObject hudObject;
        public static TextUI hudInstance;

        public static void Destroy()
        {
            if (hudObject != null)
            {
                Object.Destroy(hudObject);
                hudObject = null;
                hudInstance = null;
            }
        }

        public static void Update()
        {
            bool isplay = Main.IsPlaying && scrController.instance != null && scrController.instance.gameworld && !scrController.instance.paused && Main.Settings.ShowTimingHUD;

            if (hudObject == null)
            {
                hudObject = new GameObject("TimingShow_HUD");
                //Object.DontDestroyOnLoad(hudObject);
                hudInstance = hudObject.AddComponent<TextUI>();
            }
            hudObject.SetActive(isplay);

            if (!isplay) return;

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
    }
}