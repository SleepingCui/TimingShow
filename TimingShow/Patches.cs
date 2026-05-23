using HarmonyLib;
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace TimingShow
{
    public static class ReflectCache
    {
        public static readonly FieldInfo HitTextMeshField = AccessTools.Field(typeof(scrHitTextMesh), "text");
    }

    // timing calc
    [HarmonyPatch(typeof(scrPlanet), "SwitchChosen")]
    public static class Patches
    {
        public static void Prefix(scrPlanet __instance)
        {
            if (!Main.IsEnabled || scrController.instance == null) return;
            if (__instance.conductor == null || __instance.conductor.song == null) return;

            double bpm = (double)__instance.conductor.bpm;
            double speed = (double)scrController.instance.speed;
            double pitch = (double)__instance.conductor.song.pitch;
            bool isCW = scrController.instance.isCW;

            if (bpm * speed *pitch == 0) return;
            double diff = (__instance.angle - __instance.targetExitAngle) * (isCW ? 1.0 : -1.0) * 60000.0 / (Math.PI * bpm * speed * pitch);

            Main.LastTiming = diff;
            UIReplacePatch.dirty = true; 

            if (Main.IsPlaying() && Main.Settings.ShowInWinPage && !RDC.auto)
            {
                Main.SessionOffsets.Add(diff);
            }
        }
    }

    // jd text
    [HarmonyPatch(typeof(scrHitTextMesh), "Show")]
    public static class HitTextMeshShowPatch
    {
        public static void Postfix(scrHitTextMesh __instance)
        {
            if (!Main.IsEnabled || !Main.Settings.ShowOnPlanet || !Main.IsPlaying()) return;
            bool replace = false;
            switch (__instance.hitMargin)
            {
                case HitMargin.TooEarly: replace = Main.Settings.ReplaceTooEarly; break;
                case HitMargin.VeryEarly: replace = Main.Settings.ReplaceVeryEarly; break;
                case HitMargin.EarlyPerfect: replace = Main.Settings.ReplaceEarlyPerfect; break;
                case HitMargin.Perfect: replace = Main.Settings.ReplacePerfect; break;
                case HitMargin.LatePerfect: replace = Main.Settings.ReplaceLatePerfect; break;
                case HitMargin.VeryLate: replace = Main.Settings.ReplaceVeryLate; break;
                case HitMargin.TooLate: replace = Main.Settings.ReplaceTooLate; break;
                case HitMargin.Multipress: replace = Main.Settings.ReplaceMultipress; break;
                case HitMargin.FailMiss: replace = Main.Settings.ReplaceFailMiss; break;
                case HitMargin.FailOverload: replace = Main.Settings.ReplaceFailOverload; break;
                default: replace = false; break;
            }

            if (replace)
            {
                if (ReflectCache.HitTextMeshField != null && ReflectCache.HitTextMeshField.GetValue(__instance) is TextMesh tm)
                {
                    Main.LastTimingColor = tm.color;
                    tm.text = Main.Format(Main.LastTiming, Main.Settings.Perc2);
                }
            }
        }
    }

    // fail text
    [HarmonyPatch(typeof(scrController), "Fail2Action")]
    public static class Fail2ActionPatch
    {
        public static void Postfix(scrController __instance)
        {
            if (!Main.IsEnabled) return;
            Main.SessionOffsets.Clear();
            if (Main.Settings.ShowOnDeath && __instance.txtTryCalibrating != null)
            {
                __instance.txtTryCalibrating.text = Main.Format(Main.LastTiming, Main.Settings.Perc3);
            }
        }
    }

    // finish text
    [HarmonyPatch(typeof(scrController), "OnLandOnPortal")]
    public static class WinPagePatch
    {
        public static void Postfix(scrController __instance)
        {
            if (!Main.IsEnabled || !Main.Settings.ShowInWinPage) return;

            if (__instance.txtResults != null && __instance.txtResults.gameObject.activeSelf)
            {
                double avgOffset = 0;
                int count = Main.SessionOffsets.Count;
                for (int i = 0; i < count; i++) { 
                    avgOffset += Main.SessionOffsets[i];
                }
                avgOffset /= count;

                string info = Main.L(Locale_zh.Avg_Timing, Locale_en.Avg_Timing) + Main.Format(avgOffset, Main.Settings.Perc4);
                __instance.txtResults.text += info;
            }
            Main.SessionOffsets.Clear();
        }
    }

    // lvl name
    [HarmonyPatch(typeof(scrUIController), "Update")]
    public static class UIReplacePatch
    {
        public static bool dirty = true;
        public static void Postfix(scrUIController __instance)
        {
            if (!Main.IsEnabled) return;
            if (Main.Settings.ShowInSongTitle && Main.IsPlaying() && __instance.txtLevelName != null)
            {
                if (dirty)
                {
                    string timing = Main.Format(Main.LastTiming, Main.Settings.Perc1);
                    if (Main.Settings.Title_UseJudgeColor)
                    {
                        timing = "<color=#" + ColorUtility.ToHtmlStringRGB(Main.LastTimingColor) + ">" + timing + "</color>";
                    }
                    __instance.txtLevelName.supportRichText = true;
                    __instance.txtLevelName.text = timing;
                    dirty = false;
                }
            }

            if (Main.IsPlaying() && Main.Settings.ShowTimingHUD)
            {
                Main.UpdateHUD();
            }
        }
    }
}