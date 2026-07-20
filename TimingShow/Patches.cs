using HarmonyLib;
using System;
using UnityEngine;

namespace TimingShow
{
    // timing logger
    [HarmonyPatch(typeof(scrController), "Start_Rewind")]
    public static class LevelStartPatch
    {
        public static void Postfix()
        {
            bool isAuto = RDC.auto;
            bool shouldLogAuto = isAuto && Main.Settings.LogAutoplay;
            bool shouldLogPlayer = !isAuto;

            if (!Main.IsEnabled || scrController.instance == null || !Main.Settings.EnableLogging || (!shouldLogAuto && !shouldLogPlayer))
            {
                TimingLogger.CloseSession();
                return;
            }

            try
            {
                if (scnGame.instance == null || scnGame.instance.levelData == null) return;
                if (Main.SessionOffsets != null) Main.SessionOffsets.Clear();

                TimingLogger.StartNewSession(scnGame.instance.levelPath, scnGame.instance.levelData.songFilename, Main.Settings.LogDirectory, Main.Settings.LogBufferSizeKB);
            }
            catch (Exception e)
            {
                Main.Logger.Error($"Failed to start timing session: {e.Message}");
            }
        }
    }

    // timing calc
    [HarmonyPatch(typeof(scrPlanet), "SwitchChosen")]
    public static class PlanetSwitchPatch
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

            bool isAuto = RDC.auto;
            bool canRecord = !isAuto || Main.Settings.LogAutoplay;

            if (Main.IsPlaying() && canRecord)
            {
                if (Main.Settings.ShowInWinPage && Main.SessionOffsets != null)
                {
                    Main.SessionOffsets.Add(diff);
                }

                if (isAuto && Main.Settings.EnableLogging)
                {
                    Main.LastHitMargin = HitMargin.Perfect;
                    TimingLogger.LogHit(diff, HitMargin.Perfect);
                }
            }
        }
    }

    // hit
    [HarmonyPatch(typeof(scrMarginTracker), "AddHit")]
    public static class MarginTrackerAddHitPatch
    {
        public static void Prefix(HitMargin hit)
        {
            if (!Main.IsEnabled || !Main.IsPlaying() || RDC.auto) return;
            Main.LastHitMargin = hit;
            TimingLogger.LogHit(Main.LastTiming, hit);
        }
    }

    // jd text
    [HarmonyPatch(typeof(scrHitTextMesh), "Show")]
    public static class HitTextMeshShowPatch
    {
        public static void Postfix(scrHitTextMesh __instance)
        {
            if (!Main.IsEnabled || !Main.Settings.ShowOnPlanet || !Main.IsPlaying()) return;
            //Main.LastHitMargin = __instance.hitMargin;
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
                    targetColor = CalcXP.XPc(scrController.instance.chosenPlanet, Main.LastTiming, cond.bpm, scrController.instance.planetarySystem.speed, cond.song.pitch, Main.Settings.Planet_EnableXPerfect, __instance.hitMargin);
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
            if (Main.IsPlaying() && Main.Settings.ShowInSongTitle && __instance.txtLevelName != null)
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

            if (Main.IsPlaying() && Main.Settings.ShowTimingHUD)
            {
                HUDMan.Update();
            }
        }
    }
}