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
            bool isPlayBase = ModContext.IsPlaying && scrController.instance != null && scrController.instance.gameworld && !scrController.instance.paused;
            
            bool isTimingPlay = isPlayBase && ModContext.Settings.ShowTimingHUD;
            EnsureUI(ref _hudObj, ref _hudInstance, "TimingShow_HUD", isTimingPlay);

            bool isURPlay = isPlayBase && ModContext.Settings.ShowURHUD;
            EnsureUI(ref _urhudObj, ref _urHudInstance, "TimingShow_URHUD", isURPlay);

            bool isRatioPlay = isPlayBase && ModContext.Settings.ShowRatioHUD;
            EnsureUI(ref _ratiohudObj, ref _ratioHudInstance, "TimingShow_RatioHUD", isRatioPlay);

            bool isXACCPlay = isPlayBase && ModContext.Settings.ShowXACCGraph;
            if (ModContext.Settings.XACCGraph_ShowEnd) isXACCPlay = isXACCPlay && ModContext.IsLevelFinished;
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

            if (!ModContext.UIDirty) return; 

            // timing hud
            if (isTimingPlay)
            {
                string timing = ModContext.Settings.HUD_ShowAngle
                    ? ModContext.LastAngle.ToString("F" + ModContext.Settings.PercHUD)
                    : ModContext.LastTiming.ToString("F" + ModContext.Settings.PercHUD);
                if (ModContext.Settings.HUD_UseJudgeColor)
                {
                    var cond = scrController.instance.chosenPlanet.conductor;
                    Color fColor = CalcXP.XPc(scrController.instance.chosenPlanet, ModContext.LastTiming, cond.bpm, scrController.instance.planetarySystem.speed, cond.song.pitch, ModContext.Settings.HUD_EnableXPerfect, ModContext.LastHitMargin, ModContext.LastIsXP);
                    timing = $"<color=#{ColorUtility.ToHtmlStringRGB(fColor)}>" + timing + "</color>";
                }
                string format = ModContext.Settings.HUD_Format;
                if (ModContext.Settings.HUD_ShowAngle) format = format.Replace("ms", "°");
                UpdateTextHUD(_hudInstance, format, timing, ModContext.Settings.HUD_x, ModContext.Settings.HUD_y, ModContext.Settings.HUD_scale, ModContext.Settings.HUD_align, ModContext.Settings.HUD_bold);
            }

            // ur hud
            if (isURPlay)
            {
                string urStr = CalcUR.Calc().ToString("F" + ModContext.Settings.PercURHUD);
                UpdateTextHUD(_urHudInstance, ModContext.Settings.URHUD_Format, urStr, ModContext.Settings.URHUD_x, ModContext.Settings.URHUD_y, ModContext.Settings.URHUD_scale, ModContext.Settings.URHUD_align, ModContext.Settings.URHUD_bold);
            }

            // ratio hud
            if (isRatioPlay)
            {
                string ratioStr = CalcRatio.GetRatioString();
                UpdateTextHUD(_ratioHudInstance, ModContext.Settings.RatioHUD_Format, ratioStr, ModContext.Settings.RatioHUD_x, ModContext.Settings.RatioHUD_y, ModContext.Settings.RatioHUD_scale, ModContext.Settings.RatioHUD_align, ModContext.Settings.RatioHUD_bold);
            }

            ModContext.UIDirty = false;
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
