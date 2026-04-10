using HarmonyLib;
using System;
using System.Linq;
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
            var textField = AccessTools.Field(typeof(scrHitTextMesh), "text");
            if (textField?.GetValue(__instance) is TextMesh tm)
                tm.text = Main.Format(Main.LastTiming, Main.Settings.perc2);
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
                __instance.txtTryCalibrating.text = Main.Format(Main.LastTiming, Main.Settings.perc3);
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
                double avgError = Main.SessionOffsets.Select(Math.Abs).Average();
                double avgBias = Main.SessionOffsets.Average();
                string info = $"绝对平均误差: {Main.Format(avgError, Main.Settings.perc4)}  平均偏差: {Main.Format(avgBias, Main.Settings.perc4)}";
                if (__instance.txtResults != null && __instance.txtResults.gameObject.activeSelf)
                    __instance.txtResults.text += info;
                Main.SessionOffsets.Clear();
            }
            catch { }
        }
    }

    //lvl name
    [HarmonyPatch(typeof(scrUIController), "Update")]
    public static class UIReplacePatch
    {
        public static void Postfix(scrUIController __instance)
        {
            if (Main.IsEnabled && Main.Settings.ShowInSongTitle && Main.IsPlaying() && __instance.txtLevelName != null)
                __instance.txtLevelName.text = Main.Format(Main.LastTiming, Main.Settings.perc1);
        }
    }
}