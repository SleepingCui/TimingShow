using UnityEngine;

namespace TimingShow
{
    public static class HUDMan
    {
        public static GameObject hudObj;
        public static TextUI hudInstance;

        public static GameObject urhudObj;
        public static TextUI urHudInstance;

        public static GameObject ratiohudObj;
        public static TextUI ratioHudInstance;

        public static GameObject xaccGraphObject;
        public static XACCGraphDrawer xaccGraphInstance;

        public static void Destroy()
        {
            DestroyHUD(ref hudObj, ref hudInstance);
            DestroyHUD(ref urhudObj, ref urHudInstance);
            DestroyHUD(ref ratiohudObj, ref ratioHudInstance);
            DestroyHUD(ref xaccGraphObject, ref xaccGraphInstance);
        }

        public static void Update()
        {
            bool isPlayBase = Main.IsPlaying && scrController.instance != null && scrController.instance.gameworld && !scrController.instance.paused;

            // timing hud
            bool isTimingPlay = isPlayBase && Main.Settings.ShowTimingHUD;
            EnsureUI(ref hudObj, ref hudInstance, "TimingShow_HUD", isTimingPlay);

            if (isTimingPlay)
            {
                string timing = Main.LastTiming.ToString("F" + Main.Settings.PercHUD);
                if (Main.Settings.HUD_UseJudgeColor)
                {
                    var cond = scrController.instance.chosenPlanet.conductor;
                    Color fColor = CalcXP.XPc(scrController.instance.chosenPlanet, Main.LastTiming, cond.bpm, scrController.instance.planetarySystem.speed, cond.song.pitch, Main.Settings.HUD_EnableXPerfect, Main.LastHitMargin);
                    timing = $"<color=#{ColorUtility.ToHtmlStringRGB(fColor)}>" + timing + "</color>";
                }

                UpdateTextHUD(hudInstance, Main.Settings.HUD_Format, timing, Main.Settings.HUD_x, Main.Settings.HUD_y, Main.Settings.HUD_scale, Main.Settings.HUD_align, Main.Settings.HUD_bold);
            }

            // ur hud
            bool isURPlay = isPlayBase && Main.Settings.ShowURHUD;
            EnsureUI(ref urhudObj, ref urHudInstance, "TimingShow_URHUD", isURPlay);

            if (isURPlay)
            {
                double currentUR = CalcUR.calc(Main.SessionOffsets);
                string urStr = currentUR.ToString("F" + Main.Settings.PercURHUD);

                UpdateTextHUD(urHudInstance, Main.Settings.URHUD_Format, urStr, Main.Settings.URHUD_x, Main.Settings.URHUD_y, Main.Settings.URHUD_scale, Main.Settings.URHUD_align, Main.Settings.URHUD_bold);
            }

            // ratio hud
            bool isRatioPlay = isPlayBase && Main.Settings.ShowRatioHUD;
            EnsureUI(ref ratiohudObj, ref ratioHudInstance, "TimingShow_RatioHUD", isRatioPlay);

            if (isRatioPlay)
            {
                string ratioStr = CalcRatio.GetRatioString();

                UpdateTextHUD(ratioHudInstance, Main.Settings.RatioHUD_Format, ratioStr, Main.Settings.RatioHUD_x, Main.Settings.RatioHUD_y, Main.Settings.RatioHUD_scale, Main.Settings.RatioHUD_align, Main.Settings.RatioHUD_bold);
            }

            // xacc gr
            bool isXACCPlay = isPlayBase && Main.Settings.ShowXACCGraph;
            if (xaccGraphObject == null)
            {
                xaccGraphObject = new GameObject("TimingShow_XACCCanvas");
                Canvas canvas = xaccGraphObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;

                GameObject drawerObj = new GameObject("XACCGraphDrawer");
                drawerObj.transform.SetParent(xaccGraphObject.transform, false);

                xaccGraphInstance = drawerObj.AddComponent<XACCGraphDrawer>();
            }
            xaccGraphObject.SetActive(isXACCPlay);
        }


        private static void EnsureUI<T>(ref GameObject obj, ref T instance, string name, bool active) where T : Component
        {
            if (obj == null)
            {
                obj = new GameObject(name);
                instance = obj.AddComponent<T>();
            }
            obj.SetActive(active);
        }

        private static void UpdateTextHUD(TextUI instance, string format, string value, float x, float y, float scale, int align, bool bold)
        {
            instance.SetText(string.Format(format, value));
            instance.SetPosition(x, y);
            instance.SetSize((int)(24 * scale));
            instance.text.alignment = instance.ToAlign(align);
            instance.text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        }

        private static void DestroyHUD<T>(ref GameObject obj, ref T instance) where T : class
        {
            if (obj != null)
            {
                Object.Destroy(obj);
                obj = null;
                instance = null;
            }
        }
    }
}