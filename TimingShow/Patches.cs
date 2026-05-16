using HarmonyLib;
using System;
using UnityEngine;

namespace TimingShow
{
    //timing calc
    [HarmonyPatch(typeof(scrPlanet), "SwitchChosen")]
    public static class Patches
    {
        public static void Prefix(scrPlanet __instance)
        {
            if (!Main.IsEnabled || scrController.instance == null) return;
            try
            {
                double bpm = (double)__instance.conductor.bpm;
                double speed = (double)scrController.instance.speed;
                double pitch = (double)__instance.conductor.song.pitch;
                bool isCW = scrController.instance.isCW;
                double diff = (__instance.angle - __instance.targetExitAngle) * (isCW ? 1.0 : -1.0) * 60000.0 / (Math.PI * bpm * speed * pitch);
                Main.LastTiming = diff;
                if (Main.IsPlaying() && Main.Settings.ShowInWinPage && !RDC.auto)
                    Main.SessionOffsets.Add(diff);
            }
            catch { }
        }
    }
    //jd text
    [HarmonyPatch(typeof(scrHitTextMesh), "Show")]
    public static class HitTextMeshShowPatch
    {
        public static void Postfix(scrHitTextMesh __instance)
        {
            if (!Main.IsEnabled || !Main.Settings.ShowOnPlanet || !Main.IsPlaying()) return;

            bool shouldReplace = false;
            switch (__instance.hitMargin)
            {
                case HitMargin.TooEarly: shouldReplace = Main.Settings.ReplaceTooEarly; break;
                case HitMargin.VeryEarly: shouldReplace = Main.Settings.ReplaceVeryEarly; break;
                case HitMargin.EarlyPerfect: shouldReplace = Main.Settings.ReplaceEarlyPerfect; break;
                case HitMargin.Perfect: shouldReplace = Main.Settings.ReplacePerfect; break;
                case HitMargin.LatePerfect: shouldReplace = Main.Settings.ReplaceLatePerfect; break;
                case HitMargin.VeryLate: shouldReplace = Main.Settings.ReplaceVeryLate; break;
                case HitMargin.TooLate: shouldReplace = Main.Settings.ReplaceTooLate; break;
                case HitMargin.Multipress: shouldReplace = Main.Settings.ReplaceMultipress; break;
                case HitMargin.FailMiss: shouldReplace = Main.Settings.ReplaceFailMiss; break;
                case HitMargin.FailOverload: shouldReplace = Main.Settings.ReplaceFailOverload; break;
                default: shouldReplace = false; break;
            }
            if (shouldReplace)
            {
                var textField = AccessTools.Field(typeof(scrHitTextMesh),"text");
                if (textField?.GetValue(__instance) is TextMesh tm)
                {
                    Main.LastTimingColor = tm.color;
                    tm.text = Main.Format(Main.LastTiming,Main.Settings.Perc2);
                }
            }
        }
    }

    //fail text
    [HarmonyPatch(typeof(scrController), "Fail2Action")]
    public static class Fail2ActionPatch
    {
        public static void Postfix(scrController __instance)
        {
            if (!Main.IsEnabled) return;
            Main.SessionOffsets.Clear();
            if (Main.Settings.ShowOnDeath && __instance.txtTryCalibrating != null)
                __instance.txtTryCalibrating.text = Main.Format(Main.LastTiming, Main.Settings.Perc3);
        }
    }

    //finish text
    [HarmonyPatch(typeof(scrController), "OnLandOnPortal")]
    public static class WinPagePatch
    {
        public static void Postfix(scrController __instance)
        {
            if (!Main.IsEnabled || !Main.Settings.ShowInWinPage || Main.SessionOffsets.Count == 0) return;
            try 
            {
                double avgOffset = 0;
                foreach (var offset in Main.SessionOffsets) avgOffset += offset;
                avgOffset /= Main.SessionOffsets.Count;
                string info = Main.L(Locale_zh.Avg_Timing, Locale_en.Avg_Timing) + Main.Format(avgOffset, Main.Settings.Perc4);
                if (__instance.txtResults != null && __instance.txtResults.gameObject.activeSelf) {
                    __instance.txtResults.text += info;
                }
                Main.SessionOffsets.Clear();
            }
            catch {}
        }
    }

    //lvl name
    [HarmonyPatch(typeof(scrUIController), "Update")]
    public static class UIReplacePatch
    {
        public static void Postfix(scrUIController __instance)
        {
            if (Main.IsEnabled && Main.Settings.ShowInSongTitle && Main.IsPlaying() && __instance.txtLevelName != null)
            {
                string timing = Main.Format(Main.LastTiming, Main.Settings.Perc1);
                if (Main.Settings.Title_UseJudgeColor)
                {
                    timing = $"<color=#{ColorUtility.ToHtmlStringRGB(Main.LastTimingColor)}>" + timing + "</color>";
                }
                __instance.txtLevelName.supportRichText = true;
                __instance.txtLevelName.text = timing;
            }

            if (Main.IsEnabled && Main.IsPlaying() && Main.Settings.ShowTimingHUD)
            {
                Main.UpdateHUD();
            }
        }
    }
}