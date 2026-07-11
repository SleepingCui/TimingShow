using HarmonyLib;
using System;
using UnityEngine;

namespace TimingShow
{
    // timing calc
    [HarmonyPatch(typeof(scrPlanet), "SwitchChosen")]
    public static class Patches
    {
        public static void Prefix(scrPlanet __instance)
        {
            if (!Main.IsEnabled || scrController.instance == null) return;
            if (__instance.conductor == null || __instance.conductor.song == null) return;

            double bpm = (double)__instance.conductor.bpm;
            double speed = (double)scrController.instance.planetarySystem.speed;
            double pitch = (double)__instance.conductor.song.pitch;
            bool isCW = scrController.instance.planetarySystem.isCW;

            if (bpm * speed * pitch == 0) return;
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

            if (replace && __instance.text != null)
            {
                ColourSchemeHitMargin hitMarginColours = RDConstants.data.hitMarginColours;
                Color targetColor = Color.gray;

                bool isvanilla = __instance.hitMargin == HitMargin.Perfect || __instance.hitMargin == HitMargin.EarlyPerfect || __instance.hitMargin == HitMargin.LatePerfect;

                if (isvanilla)
                {
                    var cond = scrController.instance.chosenPlanet.conductor;
                    targetColor = CalcXP.XPc(scrController.instance.chosenPlanet, Main.LastTiming, cond.bpm, scrController.instance.planetarySystem.speed, cond.song.pitch, Main.Settings.Planet_EnableXPerfect);
                }
                else
                {
                    switch (__instance.hitMargin)
                    {
                        case HitMargin.TooEarly: targetColor = hitMarginColours.colourTooEarly; break;
                        case HitMargin.VeryEarly: targetColor = hitMarginColours.colourVeryEarly; break;
                        case HitMargin.VeryLate: targetColor = hitMarginColours.colourVeryLate; break;
                        case HitMargin.TooLate: targetColor = hitMarginColours.colourTooLate; break;
                        case HitMargin.Multipress: targetColor = hitMarginColours.colourMultipress; break;
                        case HitMargin.FailMiss: targetColor = hitMarginColours.colourFail; break;
                        case HitMargin.FailOverload: targetColor = hitMarginColours.colourFail; break;
                        case HitMargin.OverPress: targetColor = hitMarginColours.colourFail; break;
                    }
                }

                __instance.text.text = Main.Format(Main.LastTiming, Main.Settings.Perc2);
                __instance.text.color = targetColor;
                __instance.text.ForceMeshUpdate(false, false);
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
            if (__instance.detailedResults != null && __instance.detailedResults.textComponent != null && __instance.detailedResults.gameObject.activeSelf)
            {
                double avgOffset = 0;
                int count = Main.SessionOffsets.Count;
                if (count > 0)
                {
                    for (int i = 0; i < count; i++) avgOffset += Main.SessionOffsets[i];
                    avgOffset /= count;
                }

                string info = "\n" + LangMan.T("Avg_Timing") + Main.Format(avgOffset, Main.Settings.Perc4);
                var resultsField = typeof(DetailedResults).GetField("results", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (resultsField != null)
                {
                    string[] resultsArray = resultsField.GetValue(__instance.detailedResults) as string[];
                    if (resultsArray != null)
                    {
                        for (int i = 0; i < resultsArray.Length; i++) resultsArray[i] += info;
                    }
                }
                __instance.detailedResults.textComponent.text += info;
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
            if (Main.IsPlaying() && Main.Settings.ShowInSongTitle && __instance.txtLevelName != null)
            {
                if (dirty)
                {
                    string timing = Main.Format(Main.LastTiming, Main.Settings.Perc1);
                    if (Main.Settings.Title_UseJudgeColor)
                    {
                        var cond = scrController.instance.chosenPlanet.conductor;
                        Color titleColor = CalcXP.XPc(scrController.instance.chosenPlanet, Main.LastTiming, cond.bpm, scrController.instance.planetarySystem.speed, cond.song.pitch, Main.Settings.Title_EnableXPerfect);
                        timing = "<color=#" + ColorUtility.ToHtmlStringRGB(titleColor) + ">" + timing + "</color>";
                    }
                    __instance.txtLevelName.supportRichText = true;
                    __instance.txtLevelName.text = timing;
                    dirty = false;
                }
            }

            if (Main.IsPlaying() && Main.Settings.ShowTimingHUD)
            {
                HUDMan.Update();
            }
        }
    }
}