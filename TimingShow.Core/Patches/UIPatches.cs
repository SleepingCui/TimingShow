using HarmonyLib;
using System;
using System.Collections.Generic;
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
                if (__instance.text == null) return;
                
                HitMan judge = HitMarginCompat.ToJudgeKind((int)__instance.hitMargin);

                if (!ShouldReplace(judge)) return;

                Color targetColor = JColors.GetColor(judge, ResolveIsXP(judge), ModContext.Settings.Planet_EnableXPerfect);

                string timingText = ModContext.Settings.Planet_ShowAngle
                    ? ModContext.FormatAngle(ModContext.LastAngle, ModContext.Settings.Perc2)
                    : ModContext.Format(ModContext.LastTiming, ModContext.Settings.Perc2);

                int fontSize = ModContext.Settings.Planet_FontSize;
                __instance.text.richText = true;
                __instance.text.text = fontSize == 100 ? timingText : $"<size={fontSize}%>{timingText}</size>";
                __instance.text.color = targetColor;
                __instance.text.ForceMeshUpdate();
                
                if (judge == HitMan.XPerfect)
                    HideXPerfectBorder(__instance);
            }

            private static FieldInfo _xPerfectBorderField;
            private static bool _xPerfectBorderResolved;
            
            private static void HideXPerfectBorder(scrHitTextMesh instance)
            {
                if (!_xPerfectBorderResolved)
                {
                    _xPerfectBorderResolved = true;
                    try
                    {
                        _xPerfectBorderField = typeof(scrHitTextMesh).GetField(
                            "xPerfectBorder",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    }
                    catch (Exception e)
                    {
                        ModContext.Logger.Log($"xPerfectBorder not available, skipping native border handling: {e.Message}");
                    }
                }
                if (_xPerfectBorderField == null) return;

                try
                {
                    object border = _xPerfectBorderField.GetValue(instance);
                    if (border is Component component)
                    {
                        if (component != null) component.gameObject.SetActive(false);
                    }
                    else if (border is GameObject go)
                    {
                        if (go != null) go.SetActive(false);
                    }
                }
                catch (Exception e)
                {
                    ModContext.Logger?.Log($"Failed to disable xPerfectBorder: {e.Message}");
                }
            }

            private static bool ShouldReplace(HitMan judge)
            {
                Settings s = ModContext.Settings;
                switch (judge)
                {
                    case HitMan.TooEarly: return s.ReplaceTooEarly;
                    case HitMan.VeryEarly: return s.ReplaceVeryEarly;
                    case HitMan.EarlyPerfect: return s.ReplaceEarlyPerfect;
                    case HitMan.PerfectMinus: return HitMarginCompat.IsGame34 ? s.ReplacePerfectMinus : s.ReplacePerfect;
                    case HitMan.XPerfect: return s.ReplaceXPerfect;
                    case HitMan.PerfectPlus: return s.ReplacePerfectPlus;
                    case HitMan.LatePerfect: return s.ReplaceLatePerfect;
                    case HitMan.VeryLate: return s.ReplaceVeryLate;
                    case HitMan.TooLate: return s.ReplaceTooLate;
                    case HitMan.Multipress: return s.ReplaceMultipress;
                    case HitMan.FailMiss: return s.ReplaceFailMiss;
                    case HitMan.FailOverload: return s.ReplaceFailOverload;
                    case HitMan.OverPress: return s.ReplaceOverPress;
                    case HitMan.Auto: return s.ReplaceAuto;
                    default: return false;
                }
            }
            
            private static bool ResolveIsXP(HitMan judge)
            {
                if (HitMarginCompat.IsGame34) return judge == HitMan.XPerfect;
                return ModContext.LastIsXP;
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
                    List<string> items = new List<string>();

                    if (ModContext.Settings.ShowOnDeath_ShowAvgTiming)
                    {
                        double avgOffset = 0;
                        int count = ModContext.SessionOffsets != null ? ModContext.SessionOffsets.Count : 0;
                        if (count > 0)
                        {
                            for (int i = 0; i < count; i++) avgOffset += ModContext.SessionOffsets[i];
                            avgOffset /= count;
                        }
                        items.Add($"{i18n.T("Avg_Timing")}{ModContext.Format(avgOffset, ModContext.Settings.Perc3)}");
                    }

                    if (ModContext.Settings.ShowOnDeath_ShowUR)
                    {
                        items.Add($"{i18n.T("Label_UR")}{CalcUR.calc(ModContext.SessionOffsets).ToString("F" + ModContext.Settings.Perc3)}");
                    }

                    if (ModContext.Settings.ShowOnDeath_ShowXACC)
                    {
                        float xaccPerc = 0f;
                        if (__instance.playerOne != null && __instance.playerOne.marginTracker != null)
                            xaccPerc = __instance.playerOne.marginTracker.percentXAcc * 100f;
                        items.Add($"XACC: {xaccPerc.ToString("F" + ModContext.Settings.Perc3)}%");
                    }

                    if (ModContext.Settings.ShowOnDeath_ShowRatio)
                    {
                        string ratioStr = CalcRatio.GetRatioString();
                        items.Add($"Ratio: {ratioStr}:1");
                    }

                    if (items.Count > 0)
                    {
                        int fontSize = ModContext.Settings.ShowOnDeath_FontSize;
                        string info = $"<size={fontSize}%>{string.Join("    ", items)}</size>";
                        __instance.txtTryCalibrating.text = info;
                    }
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
                    List<string> items = new List<string>();

                    if (ModContext.Settings.ShowInWinPage_ShowAvgTiming)
                    {
                        double avgOffset = 0;
                        int count = ModContext.SessionOffsets != null ? ModContext.SessionOffsets.Count : 0;

                        if (count > 0)
                        {
                            for (int i = 0; i < count; i++) avgOffset += ModContext.SessionOffsets[i];
                            avgOffset /= count;
                        }

                        items.Add(i18n.T("Avg_Timing") + ModContext.Format(avgOffset, ModContext.Settings.Perc4));
                    }

                    if (ModContext.Settings.ShowInWinPage_ShowUR)
                    {
                        items.Add(i18n.T("Label_UR") + CalcUR.calc(ModContext.SessionOffsets).ToString("F" + Math.Max(0, ModContext.Settings.Perc4)));
                    }

                    if (ModContext.Settings.ShowInWinPage_ShowRatio)
                    {
                        string ratioStr = CalcRatio.GetRatioString();
                        items.Add($"Ratio: {ratioStr}:1");
                    }

                    if (items.Count > 0)
                    {
                        int fontSize = ModContext.Settings.ShowInWinPage_FontSize;
                        string info = $"<size={fontSize}%>    {string.Join("    ", items)}</size>";

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
                            Color titleColor = JColors.GetColor(ModContext.LastJudge, ModContext.LastIsXP, ModContext.Settings.Title_EnableXPerfect);
                            timing = "<color=#" + ColorUtility.ToHtmlStringRGB(titleColor) + ">" + timing + "</color>";
                        }
                        int fontSize = ModContext.Settings.Title_FontSize;
                        if (fontSize != 100) timing = $"<size={fontSize}%>{timing}</size>";

                        __instance.txtLevelName.supportRichText = true;
                        __instance.txtLevelName.text = timing;
                    }
                }

                if (ModContext.IsPlaying)
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
