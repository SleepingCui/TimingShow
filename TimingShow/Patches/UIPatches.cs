using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace TimingShow.Patches
{
    public static class UIPatches
    {
        // jd text
        [HarmonyPatch(typeof(scrHitTextMesh), "Show")]
        [HarmonyPriority(199)]
        public static class HitTextMeshShowPatch
        {
            public static void Postfix(scrHitTextMesh __instance)
            {
                if (!Main.IsEnabled || !Main.Settings.ShowOnPlanet || !Main.IsPlaying) return;

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
                        var controller = scrController.instance;
                        var conductor = scrController.conductor ?? scrConductor.instance ?? (controller != null && controller.chosenPlanet != null ? controller.chosenPlanet.conductor : null);

                        if (controller != null && conductor != null && conductor.song != null)
                        {
                            double bpm = (double)conductor.bpm;
                            double speed = controller.planetarySystem != null ? (double)controller.planetarySystem.speed : 1.0;
                            double pitch = (double)conductor.song.pitch;

                            targetColor = CalcXP.XPc(controller.chosenPlanet, Main.LastTiming, bpm, speed, pitch, Main.Settings.Planet_EnableXPerfect, __instance.hitMargin);
                        }
                        else
                        {
                            targetColor = hitMarginColours.colourPerfect;
                        }
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
                TimingLogger.CloseSession();
                if (!Main.IsEnabled) return;
                if (Main.SessionOffsets != null) Main.SessionOffsets.Clear();
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
                TimingLogger.CloseSession();
                if (!Main.IsEnabled) return;
                if (!Main.Settings.ShowInWinPage) return;

                if (__instance.detailedResults != null && __instance.detailedResults.textComponent != null && __instance.detailedResults.gameObject.activeSelf)
                {
                    double avgOffset = 0;
                    double urValue = 0;
                    int count = Main.SessionOffsets != null ? Main.SessionOffsets.Count : 0;

                    if (count > 0)
                    {
                        for (int i = 0; i < count; i++) avgOffset += Main.SessionOffsets[i];
                        avgOffset /= count;
                        double sumOfSquares = 0;
                        for (int i = 0; i < count; i++)
                        {
                            double diff = Main.SessionOffsets[i] - avgOffset;
                            sumOfSquares += diff * diff;
                        }
                        double stdDev = Math.Sqrt(sumOfSquares / count);
                        urValue = stdDev * 10.0;
                    }

                    string info = LangMan.T("Avg_Timing") + Main.Format(avgOffset, Main.Settings.Perc4)+ "    " + LangMan.T("Label_UR") + urValue.ToString("F" + Math.Max(0, Main.Settings.Perc4));
                    var resultsField = typeof(DetailedResults).GetField("results", BindingFlags.NonPublic | BindingFlags.Instance);
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
                if (Main.SessionOffsets != null) Main.SessionOffsets.Clear();
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
                if (Main.IsPlaying && Main.Settings.ShowInSongTitle && __instance.txtLevelName != null)
                {
                    if (dirty)
                    {
                        string timing = Main.Format(Main.LastTiming, Main.Settings.Perc1);
                        if (Main.Settings.Title_UseJudgeColor)
                        {
                            var cond = scrController.instance.chosenPlanet.conductor;
                            Color titleColor = CalcXP.XPc(scrController.instance.chosenPlanet, Main.LastTiming, cond.bpm, scrController.instance.planetarySystem.speed, cond.song.pitch, Main.Settings.Title_EnableXPerfect, Main.LastHitMargin);
                            timing = "<color=#" + ColorUtility.ToHtmlStringRGB(titleColor) + ">" + timing + "</color>";
                        }
                        __instance.txtLevelName.supportRichText = true;
                        __instance.txtLevelName.text = timing;
                        dirty = false;
                    }
                }

                if (Main.IsPlaying && (Main.Settings.ShowTimingHUD || Main.Settings.ShowURHUD))
                {
                    HUDMan.Update();
                }
            }
        }
    }
}
