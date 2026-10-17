using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using TimingShow.HUD;
using TimingShow.Logging;
using TimingShow.Diagnostics;

namespace TimingShow.Patches
{
    public static class UIPatches
    {
        [HarmonyPatch(typeof(scrHitTextManager), "ShowHitText")]
        public static class HitTextManagerShowPatch
        {
            public static int CurrentHitMargin = -1;

            public static void Prefix(HitMargin hitMargin, scrPlanet planet)
            {
                Players.CurrentId = Players.ResolvePlayerId(planet);
                CurrentHitMargin = (int)hitMargin;
            }

            public static void Postfix()
            {
                Players.CurrentId = Players.Unset;
                CurrentHitMargin = -1;
            }
        }

        // jd text
        [HarmonyPatch(typeof(scrHitTextMesh), "Show")]
        [HarmonyPriority(199)]
        public static class HitTextMeshShowPatch
        {
            public static void Postfix(scrHitTextMesh __instance)
            {
                if (!ModContext.IsEnabled || !ModContext.Settings.ShowOnPlanet || !ModContext.IsPlaying) return;
                if (__instance.text == null) return;

                int rawMargin = (int)__instance.hitMargin;
                HitMan judge = HitMarginCompat.ToJudgeKind(rawMargin);

                if (!ShouldReplace(judge)) return;

                PlayerSession player = HitTextManagerShowPatch.CurrentHitMargin == rawMargin
                    ? Players.Current
                    : Players.Get(0);

                Color targetColor = JColors.GetColor(judge, player.IsXPerfect(judge), ModContext.Settings.Planet_EnableXPerfect);

                string timingText = ModContext.Settings.Planet_ShowAngle
                    ? ModContext.FormatAngle(player.LastAngle, ModContext.Settings.Perc2)
                    : ModContext.Format(player.LastTiming, ModContext.Settings.Perc2);

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
                    Players.ResetAll();
                    return;
                }

                if (ModContext.Settings.ShowOnDeath && __instance.txtTryCalibrating != null)
                {
                    string lines = Coop.JoinLines(BuildDeathLine);
                    if (!string.IsNullOrEmpty(lines))
                    {
                        int fontSize = ModContext.Settings.ShowOnDeath_FontSize;
                        __instance.txtTryCalibrating.text = $"<size={fontSize}%>{lines}</size>";
                    }
                }

                TimingLogger.CloseSession();
            }

            private static string BuildDeathLine(PlayerSession player)
            {
                Settings s = ModContext.Settings;
                List<string> items = new List<string>();

                if (s.ShowOnDeath_ShowAvgTiming)
                {
                    items.Add($"{i18n.T("Avg_Timing")}{ModContext.Format(CalcUR.Mean(player.Offsets), s.Perc3)}");
                }

                if (s.ShowOnDeath_ShowUR)
                {
                    items.Add($"{i18n.T("Label_UR")}{CalcUR.calc(player.Offsets).ToString("F" + s.Perc3)}");
                }

                if (s.ShowOnDeath_ShowXACC)
                {
                    items.Add($"XACC: {(GetPercentXAcc(player) * 100f).ToString("F" + s.Perc3)}%");
                }

                if (s.ShowOnDeath_ShowRatio)
                {
                    items.Add($"Ratio: {CalcRatio.GetRatioText(player)}");
                }

                return items.Count > 0 ? string.Join("    ", items) : null;
            }
        }

        internal static float GetPercentXAcc(PlayerSession player)
        {
            try
            {
                scrPlayerManager manager = ADOBase.playerManager;
                if (manager == null) return 0f;

                scrPlayer[] players = manager.players;
                if (players == null || player.PlayerId >= players.Length) return 0f;

                scrPlayer owner = players[player.PlayerId];
                if (owner == null || owner.marginTracker == null) return 0f;

                return owner.marginTracker.percentXAcc;
            }
            catch (Exception e)
            {
                ModContext.Logger?.Log("GetPercentXAcc failed: " + e.Message);
                return 0f;
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
                    int fontSize = ModContext.Settings.ShowInWinPage_FontSize;
                    string[] resultsArray = GetResultsArray(__instance.detailedResults);

                    if (Coop.IsCoop && !ModContext.Settings.Coop_WinPageAllPlayers)
                    {
                        if (resultsArray != null)
                        {
                            for (int i = 0; i < resultsArray.Length; i++)
                            {
                                string line = BuildWinLine(Players.Get(i));
                                if (string.IsNullOrEmpty(line)) continue;
                                resultsArray[i] += $"<size={fontSize}%>    {line}</size>";
                            }
                        }
                    }
                    else
                    {
                        string lines = Coop.JoinLines(BuildWinLine, "    ");
                        if (!string.IsNullOrEmpty(lines))
                        {
                            string info = $"<size={fontSize}%>{lines}</size>";

                            if (resultsArray != null)
                            {
                                for (int i = 0; i < resultsArray.Length; i++) resultsArray[i] += info;
                            }
                            __instance.detailedResults.textComponent.text += info;
                        }
                    }
                }
            }

            private static string BuildWinLine(PlayerSession player)
            {
                Settings s = ModContext.Settings;
                List<string> items = new List<string>();

                if (s.ShowInWinPage_ShowAvgTiming)
                {
                    items.Add(i18n.T("Avg_Timing") + ModContext.Format(CalcUR.Mean(player.Offsets), s.Perc4));
                }

                if (s.ShowInWinPage_ShowUR)
                {
                    items.Add(i18n.T("Label_UR") + CalcUR.calc(player.Offsets).ToString("F" + Math.Max(0, s.Perc4)));
                }

                if (s.ShowInWinPage_ShowRatio)
                {
                    items.Add($"Ratio: {CalcRatio.GetRatioText(player)}");
                }

                return items.Count > 0 ? string.Join("    ", items) : null;
            }

            private static FieldInfo _resultsField;
            private static bool _resultsFieldResolved;

            private static string[] GetResultsArray(DetailedResults detailedResults)
            {
                if (!_resultsFieldResolved)
                {
                    _resultsFieldResolved = true;
                    try
                    {
                        _resultsField = typeof(DetailedResults).GetField("results", BindingFlags.NonPublic | BindingFlags.Instance);
                    }
                    catch (Exception e)
                    {
                        ModContext.Logger?.Log($"DetailedResults.results not available: {e.Message}");
                    }
                }
                if (_resultsField == null) return null;

                try
                {
                    return _resultsField.GetValue(detailedResults) as string[];
                }
                catch (Exception e)
                {
                    ModContext.Logger?.Log($"Failed to read DetailedResults.results: {e.Message}");
                    return null;
                }
            }
        }

        // lvl name
        [HarmonyPatch(typeof(scrUIController), "Update")]
        public static class UIReplacePatch
        {
            private static readonly System.Diagnostics.Stopwatch _perfSw = new System.Diagnostics.Stopwatch();
            private static string _titleBase;
            private static int _titleBaseSize = 100;
            private static int _titleAppliedSize = -1;

            public static void Postfix(scrUIController __instance)
            {
                if (!ModContext.IsEnabled) return;

                if (ModContext.IsPlaying)
                {
                    int perfFrameId = PerfStatsRecorder.BeginFrame();

                    HitBump.Tick(Time.deltaTime * 1000.0);

                    if (ModContext.Settings.ShowInSongTitle && __instance.txtLevelName != null)
                        ApplyTitleText(__instance);

                    PlayStatePatches.SyncPauseState();
                    _perfSw.Restart();
                    HUDMan.Update();
                    _perfSw.Stop();

                    PerfStatsRecorder.EndFrame(perfFrameId, Time.unscaledDeltaTime * 1000f, (float)_perfSw.Elapsed.TotalMilliseconds);
                }
                else
                {
                    ModContext.UIDirty = false;
                    HitBump.StopAll();
                    BumpTrigger.Reset();
                    _titleBase = null;
                    _titleAppliedSize = -1;
                }
            }

            private static void ApplyTitleText(scrUIController ui)
            {
                if (ModContext.UIDirty)
                {
                    BumpTrigger.Title(Coop.Join(BuildTitleValue));
                    _titleBase = Coop.Join(BuildTitlePart);
                    _titleBaseSize = ModContext.Settings.Title_FontSize;
                    _titleAppliedSize = -1;
                }

                if (_titleBase == null) return;

                int size = ModContext.Settings.Title_Bump
                    ? BumpAnim.Scale(_titleBaseSize, HitBump.Title.Ease)
                    : _titleBaseSize;
                if (size == _titleAppliedSize) return;

                ui.txtLevelName.supportRichText = true;
                ui.txtLevelName.text = size == 100 ? _titleBase : $"<size={size}%>{_titleBase}</size>";
                _titleAppliedSize = size;
            }

            private static string BuildTitleValue(PlayerSession player)
            {
                Settings s = ModContext.Settings;
                return s.Title_ShowAngle
                    ? ModContext.FormatAngle(player.LastAngle, s.Perc1)
                    : ModContext.Format(player.LastTiming, s.Perc1);
            }

            private static string BuildTitlePart(PlayerSession player)
            {
                string value = BuildTitleValue(player);

                Settings s = ModContext.Settings;
                if (s.Title_UseJudgeColor)
                {
                    Color color = JColors.GetColor(player.LastJudge, player.IsXPerfect(player.LastJudge), s.Title_EnableXPerfect);
                    value = "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + value + "</color>";
                }

                return value;
            }
        }
    }
}
