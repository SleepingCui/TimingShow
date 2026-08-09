using UnityEngine;

namespace TimingShow
{
    public static class HUDMan
    {
        private static GameObject _hudObj;
        private static TextUI _hudInstance;

        private static GameObject _urhudObj;
        private static TextUI _urHudInstance;

        private static GameObject _ratiohudObj;
        private static TextUI _ratioHudInstance;

        private static GameObject _xaccGraphObject;
        private static XACCGraphDrawer _xaccGraphInstance;

        public static void Destroy()
        {
            DestroyHUD(ref _hudObj, ref _hudInstance);
            DestroyHUD(ref _urhudObj, ref _urHudInstance);
            DestroyHUD(ref _ratiohudObj, ref _ratioHudInstance);
            DestroyHUD(ref _xaccGraphObject, ref _xaccGraphInstance);
        }

        public static void Update()
        {
            bool isPlayBase = Main.IsPlaying && scrController.instance != null && scrController.instance.gameworld && !scrController.instance.paused;

            // timing hud
            bool isTimingPlay = isPlayBase && Main.Settings.ShowTimingHUD;
            EnsureUI(ref _hudObj, ref _hudInstance, "TimingShow_HUD", isTimingPlay);

            if (isTimingPlay)
            {
                string timing = Main.LastTiming.ToString("F" + Main.Settings.PercHUD);
                if (Main.Settings.HUD_UseJudgeColor)
                {
                    var cond = scrController.instance.chosenPlanet.conductor;
                    Color fColor = CalcXP.XPc(scrController.instance.chosenPlanet, Main.LastTiming, cond.bpm, scrController.instance.planetarySystem.speed, cond.song.pitch, Main.Settings.HUD_EnableXPerfect, Main.LastHitMargin);
                    timing = $"<color=#{ColorUtility.ToHtmlStringRGB(fColor)}>" + timing + "</color>";
                }

                UpdateTextHUD(_hudInstance, Main.Settings.HUD_Format, timing, Main.Settings.HUD_x, Main.Settings.HUD_y, Main.Settings.HUD_scale, Main.Settings.HUD_align, Main.Settings.HUD_bold);
            }

            // ur hud
            bool isURPlay = isPlayBase && Main.Settings.ShowURHUD;
            EnsureUI(ref _urhudObj, ref _urHudInstance, "TimingShow_URHUD", isURPlay);

            if (isURPlay)
            {
                double currentUR = CalcUR.calc(Main.SessionOffsets);
                string urStr = currentUR.ToString("F" + Main.Settings.PercURHUD);

                UpdateTextHUD(_urHudInstance, Main.Settings.URHUD_Format, urStr, Main.Settings.URHUD_x, Main.Settings.URHUD_y, Main.Settings.URHUD_scale, Main.Settings.URHUD_align, Main.Settings.URHUD_bold);
            }

            // ratio hud
            bool isRatioPlay = isPlayBase && Main.Settings.ShowRatioHUD;
            EnsureUI(ref _ratiohudObj, ref _ratioHudInstance, "TimingShow_RatioHUD", isRatioPlay);

            if (isRatioPlay)
            {
                string ratioStr = CalcRatio.GetRatioString();

                UpdateTextHUD(_ratioHudInstance, Main.Settings.RatioHUD_Format, ratioStr, Main.Settings.RatioHUD_x, Main.Settings.RatioHUD_y, Main.Settings.RatioHUD_scale, Main.Settings.RatioHUD_align, Main.Settings.RatioHUD_bold);
            }

            // xacc gr
            bool isXACCPlay = isPlayBase && Main.Settings.ShowXACCGraph;
            if (Main.Settings.XACCGraph_ShowEnd) isXACCPlay = isXACCPlay && Main.IsLevelFinished;
            if (_xaccGraphObject == null)
            {
                _xaccGraphObject = new GameObject("TimingShow_XACCCanvas");
                Canvas canvas = _xaccGraphObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;

                GameObject drawerObj = new GameObject("XACCGraphDrawer");
                drawerObj.transform.SetParent(_xaccGraphObject.transform, false);

                _xaccGraphInstance = drawerObj.AddComponent<XACCGraphDrawer>();
            }
            _xaccGraphObject.SetActive(isXACCPlay);
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