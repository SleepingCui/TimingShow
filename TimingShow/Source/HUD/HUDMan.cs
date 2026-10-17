using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TimingShow.HUD
{
    public static class HUDMan
    {
        private static readonly HashSet<string> _badFormats = new HashSet<string>();

        private static GameObject _hudObj;
        private static TextUI _hudInstance;

        private static GameObject _avghudObj;
        private static TextUI _avgHudInstance;

        private static GameObject _urhudObj;
        private static TextUI _urHudInstance;

        private static GameObject _ratiohudObj;
        private static TextUI _ratioHudInstance;

        private static GameObject _xaccGraphObject;
        private static XACCGraphDrawer _xaccGraphInstance;

        private static GameObject _timingScatterObject;
        private static TimingScatterDrawer _timingScatterInstance;

        public static void Destroy()
        {
            DestroyHUD(ref _hudObj, ref _hudInstance);
            DestroyHUD(ref _avghudObj, ref _avgHudInstance);
            DestroyHUD(ref _urhudObj, ref _urHudInstance);
            DestroyHUD(ref _ratiohudObj, ref _ratioHudInstance);
            DestroyHUD(ref _xaccGraphObject, ref _xaccGraphInstance);
            DestroyHUD(ref _timingScatterObject, ref _timingScatterInstance);
        }

        public static void Update()
        {
            bool isPlayBase = ModContext.IsPlaying && scrController.instance != null && scrController.instance.gameworld && (!scrController.instance.paused || ModContext.IsConfigOpen);
            
            bool isTimingPlay = isPlayBase && ModContext.Settings.ShowTimingHUD;
            EnsureUI(ref _hudObj, ref _hudInstance, "TimingShow_HUD", isTimingPlay);
            if (_hudInstance != null)
                _hudInstance.ApplyFont(ModContext.Settings.HUD_UseCustomFont, ModContext.Settings.HUD_FontPath);

            bool isAvgPlay = isPlayBase && ModContext.Settings.ShowAvgHUD;
            EnsureUI(ref _avghudObj, ref _avgHudInstance, "TimingShow_AvgHUD", isAvgPlay);
            if (_avgHudInstance != null)
                _avgHudInstance.ApplyFont(ModContext.Settings.AvgHUD_UseCustomFont, ModContext.Settings.AvgHUD_FontPath);

            bool isURPlay = isPlayBase && ModContext.Settings.ShowURHUD;
            EnsureUI(ref _urhudObj, ref _urHudInstance, "TimingShow_URHUD", isURPlay);
            if (_urHudInstance != null)
                _urHudInstance.ApplyFont(ModContext.Settings.URHUD_UseCustomFont, ModContext.Settings.URHUD_FontPath);

            bool isRatioPlay = isPlayBase && ModContext.Settings.ShowRatioHUD;
            EnsureUI(ref _ratiohudObj, ref _ratioHudInstance, "TimingShow_RatioHUD", isRatioPlay);
            if (_ratioHudInstance != null)
                _ratioHudInstance.ApplyFont(ModContext.Settings.RatioHUD_UseCustomFont, ModContext.Settings.RatioHUD_FontPath);

            bool isXACCPlay = isPlayBase && ModContext.Settings.ShowXACCGraph;
            if (ModContext.Settings.XACCGraph_ShowEnd) isXACCPlay = isXACCPlay && ModContext.IsLevelFinished;
            if (isXACCPlay && _xaccGraphObject == null)
            {
                _xaccGraphObject = new GameObject("TimingShow_XACCCanvas");
                Canvas canvas = _xaccGraphObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;

                GameObject drawerObj = new GameObject("XACCGraphDrawer");
                drawerObj.transform.SetParent(_xaccGraphObject.transform, false);

                _xaccGraphInstance = drawerObj.AddComponent<XACCGraphDrawer>();
            }
            if (_xaccGraphObject != null && _xaccGraphObject.activeSelf != isXACCPlay) _xaccGraphObject.SetActive(isXACCPlay);


            bool isScatterBase = ModContext.IsPlaying && scrController.instance != null && scrController.instance.gameworld;
            bool isScatterPlay = isScatterBase && ModContext.Settings.ShowTimingScatter;
            if (ModContext.Settings.TimingScatter_ShowEnd) isScatterPlay = isScatterPlay && ModContext.IsLevelFinished;
            if (isScatterPlay && _timingScatterObject == null)
            {
                _timingScatterObject = new GameObject("TimingShow_TimingScatterCanvas");
                Canvas scatterCanvas = _timingScatterObject.AddComponent<Canvas>();
                scatterCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                scatterCanvas.sortingOrder = 101;
                _timingScatterObject.AddComponent<GraphicRaycaster>();

                GameObject scatterDrawerObj = new GameObject("TimingScatterDrawer");
                scatterDrawerObj.transform.SetParent(_timingScatterObject.transform, false);

                _timingScatterInstance = scatterDrawerObj.AddComponent<TimingScatterDrawer>();
            }
            if (_timingScatterObject != null && _timingScatterObject.activeSelf != isScatterPlay) _timingScatterObject.SetActive(isScatterPlay);

            bool dirty = ModContext.UIDirty;

            // timing hud
            if (isTimingPlay && dirty)
            {
                string timing = Coop.Join(ModContext.Settings.Coop_TimingHUD, FormatTiming);
                BumpTrigger.Timing(timing);
                UpdateTextHUD(_hudInstance, ModContext.Settings.HUD_Format, timing, ModContext.Settings.HUD_x, ModContext.Settings.HUD_y, ModContext.Settings.HUD_scale, ModContext.Settings.HUD_align, ModContext.Settings.HUD_bold);
            }

            // avg offset hud
            if (isAvgPlay && dirty)
            {
                string avgStr = Coop.Join(ModContext.Settings.Coop_AvgHUD, FormatAvg);
                BumpTrigger.Avg(avgStr);
                UpdateTextHUD(_avgHudInstance, ModContext.Settings.AvgHUD_Format, avgStr, ModContext.Settings.AvgHUD_x, ModContext.Settings.AvgHUD_y, ModContext.Settings.AvgHUD_scale, ModContext.Settings.AvgHUD_align, ModContext.Settings.AvgHUD_bold);
            }

            // ur hud
            if (isURPlay && dirty)
            {
                string urStr = Coop.Join(ModContext.Settings.Coop_URHUD, FormatUr);
                BumpTrigger.UR(urStr);
                UpdateTextHUD(_urHudInstance, ModContext.Settings.URHUD_Format, urStr, ModContext.Settings.URHUD_x, ModContext.Settings.URHUD_y, ModContext.Settings.URHUD_scale, ModContext.Settings.URHUD_align, ModContext.Settings.URHUD_bold);
            }

            // ratio hud
            if (isRatioPlay && dirty)
            {
                string ratioStr = Coop.Join(ModContext.Settings.Coop_RatioHUD, FormatRatio);
                BumpTrigger.Ratio(ratioStr);
                UpdateTextHUD(_ratioHudInstance, ModContext.Settings.RatioHUD_Format, ratioStr, ModContext.Settings.RatioHUD_x, ModContext.Settings.RatioHUD_y, ModContext.Settings.RatioHUD_scale, ModContext.Settings.RatioHUD_align, ModContext.Settings.RatioHUD_bold);
            }

            if (dirty) ModContext.UIDirty = false;
            
            ApplyBumpSize(_hudInstance, isTimingPlay, ModContext.Settings.HUD_scale, ModContext.Settings.HUD_Bump, HitBump.Timing);
            ApplyBumpSize(_avgHudInstance, isAvgPlay, ModContext.Settings.AvgHUD_scale, ModContext.Settings.AvgHUD_Bump, HitBump.Avg);
            ApplyBumpSize(_urHudInstance, isURPlay, ModContext.Settings.URHUD_scale, ModContext.Settings.URHUD_Bump, HitBump.UR);
            ApplyBumpSize(_ratioHudInstance, isRatioPlay, ModContext.Settings.RatioHUD_scale, ModContext.Settings.RatioHUD_Bump, HitBump.Ratio);
        }
        
        private static string FormatTiming(PlayerSession player)
        {
            Settings s = ModContext.Settings;
            string number = s.HUD_ShowAngle
                ? player.LastAngle.ToString("F" + s.PercHUD)
                : player.LastTiming.ToString("F" + s.PercHUD);

            if (s.HUD_UseJudgeColor)
            {
                Color color = JColors.GetColor(player.LastJudge, player.IsXPerfect(player.LastJudge), s.HUD_EnableXPerfect);
                number = $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>" + number + "</color>";
            }

            return number + (s.HUD_ShowAngle ? "°" : "ms");
        }

        private static string FormatAvg(PlayerSession player)
        {
            return player.Ur.Mean().ToString("F" + ModContext.Settings.PercAvgHUD) + "ms";
        }

        private static string FormatUr(PlayerSession player)
        {
            return player.Ur.StdDev().ToString("F" + ModContext.Settings.PercURHUD);
        }

        private static string FormatRatio(PlayerSession player)
        {
            return CalcRatio.GetRatioText(player);
        }

        private static void ApplyBumpSize(TextUI instance, bool active, float scale, bool bump, BumpAnim anim)
        {
            if (!active || instance == null) return;
            int baseSize = (int)(24 * scale);
            instance.SetSize(bump ? BumpAnim.Scale(baseSize, anim.Ease) : baseSize);
        }


        private static void EnsureUI<T>(ref GameObject obj, ref T instance, string name, bool active) where T : Component
        {
            if (obj == null)
            {
                if (!active) return;
                obj = new GameObject(name);
                instance = obj.AddComponent<T>();
            }
            if (obj.activeSelf != active) obj.SetActive(active);
        }

        private static void UpdateTextHUD(TextUI instance, string format, string value, float x, float y, float scale, int align, bool bold)
        {
            instance.SetText(SafeFormat(format, value));
            instance.SetPosition(x, y);
            instance.SetSize((int)(24 * scale));
            instance.SetAlignment(align);
            instance.SetBold(bold);
        }
        
        private static string SafeFormat(string format, string value)
        {
            if (format == null || _badFormats.Contains(format)) return value;
            try
            {
                return string.Format(format, value);
            }
            catch
            {
                _badFormats.Add(format);
                return value;
            }
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
