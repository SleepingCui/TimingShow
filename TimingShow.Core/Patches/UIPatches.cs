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
                if (!ModContext.IsEnabled || !ModContext.Settings.ShowOnPlanet || !ModContext.IsPlaying) return;

                bool replace;
                switch (__instance.hitMargin)
                {
                    case HitMargin.TooEarly: replace = ModContext.Settings.ReplaceTooEarly; break;
                    case HitMargin.VeryEarly: replace = ModContext.Settings.ReplaceVeryEarly; break;
                    case HitMargin.EarlyPerfect: replace = ModContext.Settings.ReplaceEarlyPerfect; break;
                    case HitMargin.Perfect: replace = ModContext.Settings.ReplacePerfect; break;
                    case HitMargin.LatePerfect: replace = ModContext.Settings.ReplaceLatePerfect; break;
                    case HitMargin.VeryLate: replace = ModContext.Settings.ReplaceVeryLate; break;
                    case HitMargin.TooLate: replace = ModContext.Settings.ReplaceTooLate; break;
                    case HitMargin.Multipress: replace = ModContext.Settings.ReplaceMultipress; break;
                    case HitMargin.FailMiss: replace = ModContext.Settings.ReplaceFailMiss; break;
                    case HitMargin.FailOverload: replace = ModContext.Settings.ReplaceFailOverload; break;
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
                            double bpm = conductor.bpm;
                            double speed = controller.planetarySystem != null ? controller.planetarySystem.speed : 1.0;
                            double pitch = conductor.song.pitch;

                            targetColor = CalcXP.XPc(controller.chosenPlanet, ModContext.LastTiming, bpm, speed, pitch, ModContext.Settings.Planet_EnableXPerfect, __instance.hitMargin, ModContext.LastIsXP);
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

                    __instance.text.text = ModContext.Settings.Planet_ShowAngle
                        ? ModContext.FormatAngle(ModContext.LastAngle, ModContext.Settings.Perc2)
                        : ModContext.Format(ModContext.LastTiming, ModContext.Settings.Perc2);
                    __instance.text.color = targetColor;
                    __instance.text.ForceMeshUpdate();
                }
            }
        }

        // fail text
        [HarmonyPatch(typeof(scrController), "Fail2Action")]
        public static class Fail2ActionPatch
        {
            public static void Postfix(scrController __instance)
            {
                if (!ModContext.IsEnabled)
                {
                    TimingLogger.CloseSession();
                    if (ModContext.SessionOffsets != null) ModContext.SessionOffsets.Clear();
                    CalcUR.Reset();
                    return;
                }

                if (ModContext.Settings.ShowOnDeath && __instance.txtTryCalibrating != null)
                {
                    double avgOffset = 0;
                    float xaccPerc = 0f;
                    int count = ModContext.SessionOffsets != null ? ModContext.SessionOffsets.Count : 0;

                    if (count > 0)
                    {
                        for (int i = 0; i < count; i++) avgOffset += ModContext.SessionOffsets[i];
                        avgOffset /= count;
                    }

                    if (__instance.playerOne != null && __instance.playerOne.marginTracker != null)
                        xaccPerc = __instance.playerOne.marginTracker.percentXAcc * 100f;

                    string info = $"<size=60%>{LangMan.T("Avg_Timing")}{ModContext.Format(avgOffset, ModContext.Settings.Perc3)}    {LangMan.T("Label_UR")}{CalcUR.calc(ModContext.SessionOffsets).ToString("F" + ModContext.Settings.Perc3)}    XACC: {xaccPerc.ToString("F" + ModContext.Settings.Perc3)}%</size>";
                    __instance.txtTryCalibrating.text = info;
                }

                TimingLogger.CloseSession();
                if (ModContext.SessionOffsets != null) ModContext.SessionOffsets.Clear();
                CalcUR.Reset();
            }
        }

        // finish text
        [HarmonyPatch(typeof(scrController), "OnLandOnPortal")]
        public static class WinPagePatch
        {
            public static void Postfix(scrController __instance)
            {
                TimingLogger.CloseSession();
                if (!ModContext.IsEnabled) return;
                if (!ModContext.Settings.ShowInWinPage) return;

                if (__instance.detailedResults != null && __instance.detailedResults.textComponent != null && __instance.detailedResults.gameObject.activeSelf)
                {
                    double avgOffset = 0;
                    int count = ModContext.SessionOffsets != null ? ModContext.SessionOffsets.Count : 0;

                    if (count > 0)
                    {
                        for (int i = 0; i < count; i++) avgOffset += ModContext.SessionOffsets[i];
                        avgOffset /= count;
                    }

                    string info = LangMan.T("Avg_Timing") + ModContext.Format(avgOffset, ModContext.Settings.Perc4) + "    " + LangMan.T("Label_UR") + CalcUR.calc(ModContext.SessionOffsets).ToString("F" + Math.Max(0, ModContext.Settings.Perc4));
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
                if (ModContext.SessionOffsets != null) ModContext.SessionOffsets.Clear();
                CalcUR.Reset();
            }
        }

        // lvl name
        [HarmonyPatch(typeof(scrUIController), "Update")]
        public static class UIReplacePatch
        {
            public static void Postfix(scrUIController __instance)
            {
                if (!ModContext.IsEnabled) return;
                if (ModContext.IsPlaying && ModContext.Settings.ShowInSongTitle && __instance.txtLevelName != null)
                {
                    if (ModContext.UIDirty)
                    {
                        string timing = ModContext.Settings.Title_ShowAngle ? ModContext.FormatAngle(ModContext.LastAngle, ModContext.Settings.Perc1) : ModContext.Format(ModContext.LastTiming, ModContext.Settings.Perc1);
                        if (ModContext.Settings.Title_UseJudgeColor)
                        {
                            var cond = scrController.instance.chosenPlanet.conductor;
                            Color titleColor = CalcXP.XPc(scrController.instance.chosenPlanet, ModContext.LastTiming, cond.bpm, scrController.instance.planetarySystem.speed, cond.song.pitch, ModContext.Settings.Title_EnableXPerfect, ModContext.LastHitMargin, ModContext.LastIsXP);
                            timing = "<color=#" + ColorUtility.ToHtmlStringRGB(titleColor) + ">" + timing + "</color>";
                        }
                        __instance.txtLevelName.supportRichText = true;
                        __instance.txtLevelName.text = timing;
                    }
                }

                if (ModContext.IsPlaying && (ModContext.Settings.ShowTimingHUD || ModContext.Settings.ShowURHUD || ModContext.Settings.ShowRatioHUD || ModContext.Settings.ShowXACCGraph))
                {
                    HUDMan.Update();
                }
                else
                {
                    ModContext.UIDirty = false;
                }
            }
        }
    }
}
