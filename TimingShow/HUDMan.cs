using UnityEngine;

namespace TimingShow
{
    public static class HUDMan
    {
        public static GameObject hudObject;
        public static TextUI hudInstance;

        public static GameObject urHudObject;
        public static TextUI urHudInstance;

        public static GameObject urGraphObject;
        public static URGraphDrawer urGraphInstance;

        public static GameObject xaccGraphObject;
        public static XACCGraphDrawer xaccGraphInstance;

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

            if (urGraphObject != null)
            {
                Object.Destroy(urGraphObject);
                urGraphObject = null;
                urGraphInstance = null;
            }

            if (xaccGraphObject != null)
            {
                Object.Destroy(xaccGraphObject);
                xaccGraphObject = null;
                xaccGraphInstance = null;
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
                double currentUR = CalcUR.calc(Main.SessionOffsets);
                string urStr = currentUR.ToString("F" + Main.Settings.PercURHUD);

                urHudInstance.SetText(string.Format(Main.Settings.URHUD_Format, urStr));
                urHudInstance.SetPosition(Main.Settings.URHUD_x, Main.Settings.URHUD_y);
                urHudInstance.SetSize((int)(24 * Main.Settings.URHUD_scale));
                urHudInstance.text.alignment = urHudInstance.ToAlign(Main.Settings.URHUD_align);
                urHudInstance.text.fontStyle = Main.Settings.URHUD_bold ? FontStyle.Bold : FontStyle.Normal;
            }

            // ur gr
            bool isGraphPlay = isPlayBase && Main.Settings.ShowURGraph;
            if (urGraphObject == null)
            {
                urGraphObject = new GameObject("TimingShow_URGraph");
                urGraphInstance = urGraphObject.AddComponent<URGraphDrawer>();
            }
            urGraphObject.SetActive(isGraphPlay);

            // xacc gr
            bool isXACCPlay = isPlayBase && Main.Settings.ShowXACCGraph;
            if (xaccGraphObject == null)
            {
                xaccGraphObject = new GameObject("TimingShow_XACCGraph");
                xaccGraphInstance = xaccGraphObject.AddComponent<XACCGraphDrawer>();
            }
            xaccGraphObject.SetActive(isXACCPlay);
        }
    }
}