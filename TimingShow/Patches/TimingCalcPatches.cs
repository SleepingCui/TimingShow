using HarmonyLib;
using System;

namespace TimingShow.Patches
{
    public static class TimingCalcPatches
    {
        // timing calc
        [HarmonyPatch(typeof(scrPlanet), "SwitchChosen")]
        public static class PlanetSwitchPatch
        {
            public static void Prefix(scrPlanet __instance)
            {
                if (!ModContext.IsEnabled || scrController.instance == null) return;
                if (__instance.conductor == null || __instance.conductor.song == null) return;

                double bpm = __instance.conductor.bpm;
                double speed = scrController.instance.planetarySystem.speed;
                double pitch = __instance.conductor.song.pitch;
                bool isCW = scrController.instance.planetarySystem.isCW;

                if (bpm * speed * pitch == 0) return;
                double diff = (__instance.angle - __instance.targetExitAngle) * (isCW ? 1.0 : -1.0) * 60000.0 / (Math.PI * bpm * speed * pitch);

                ModContext.LastTiming = diff;
                ModContext.LastSongTimeMs = PlayStatePatches.GetSessionTimeMs();
                ModContext.LastAngle = (__instance.angle - __instance.targetExitAngle) * (isCW ? 1.0 : -1.0) * 180.0 / Math.PI;
                ModContext.UIDirty = true;

                bool isAuto = RDC.auto;

                if (ModContext.IsPlaying)
                {
                    bool needRecord = ModContext.Settings.ShowInWinPage || ModContext.Settings.ShowURHUD || !isAuto || ModContext.Settings.LogAutoplay || ModContext.Settings.ShowXACCGraph;
                    if (needRecord && ModContext.SessionOffsets != null)
                    {
                        ModContext.SessionOffsets.Add(diff);
                        CalcUR.AddSample(diff);
                    }

                    if (ModContext.FullXAccHistory != null && scrController.instance?.playerOne?.marginTracker != null)
                    {
                        float curXAcc = scrController.instance.playerOne.marginTracker.percentXAcc * 100f;
                        ModContext.FullXAccHistory.Add(curXAcc);
                        ModContext.XAccVersion++;
                    }
                    
                    if (isAuto)
                    {
                        MarginTrackerAddHitPatch.ApplyAutoHit(diff, ModContext.LastAngle, ModContext.Settings.EnableLogging);
                    }
                }
            }

        }

        // hit
        [HarmonyPatch(typeof(scrMarginTracker), "AddHit")]
        public static class MarginTrackerAddHitPatch
        {
            public static int NormalPerfectCount;
            public static int PerfectFamilyCount;
            public static int XPerfectCount;
            public static int TotalHitsCount;

            public static void Prefix(HitMargin hit)
            {
                if (!ModContext.IsEnabled || !ModContext.IsPlaying) return;
                int raw = HitMarginCompat.ToRawCode(hit);
                
                ApplyHit(raw, allowLog: !RDC.auto);
            }


            public static void ApplyAutoHit(double timing, double angle, bool allowLog)
            {
                int raw = HitMarginCompat.RawAutoCode;
                ApplyHit(raw, allowLog: allowLog, countHit: false, timingOverride: timing, angleOverride: angle);
            }

            private static void ApplyHit(int rawMargin, bool allowLog, bool countHit = true, double? timingOverride = null, double? angleOverride = null)
            {
                HitMan judge = HitMarginCompat.ToJudgeKind(rawMargin);
                bool isXPerfect = judge == HitMan.XPerfect;

                ModContext.LastRawMargin = rawMargin;
                ModContext.LastJudge = judge;

                if (countHit)
                {
                    TotalHitsCount++;
                    if (HitMarginCompat.IsPerfectFamily(judge)) PerfectFamilyCount++;
                    if (HitMarginCompat.IsNormalPerfect(judge)) NormalPerfectCount++;
                    if (isXPerfect) XPerfectCount++;
                }

                if (allowLog)
                {
                    TimingLogger.LogHit(timingOverride ?? ModContext.LastTiming, angleOverride ?? ModContext.LastAngle, rawMargin, judge);
                }
            }

            public static void ResetCounts()
            {
                NormalPerfectCount = 0;
                PerfectFamilyCount = 0;
                XPerfectCount = 0;
                TotalHitsCount = 0;
            }


            public static void SyncFromTracker(scrMarginTracker tracker)
            {
                if (tracker == null || !HitMarginCompat.IsInitialized) return;

                try
                {
                    int[] rawValues = HitMarginCompat.AllRawValues;
                    int normalPerfect = 0;
                    int perfectFamily = 0;

                    for (int i = 0; i < rawValues.Length; i++)
                    {
                        int raw = rawValues[i];
                        int count = tracker.GetHits(HitMarginCompat.FromRawCode(raw));
                        if (count <= 0) continue;

                        HitMan judge = HitMarginCompat.ToJudgeKind(raw);
                        if (HitMarginCompat.IsPerfectFamily(judge)) perfectFamily += count;
                        if (HitMarginCompat.IsNormalPerfect(judge)) normalPerfect += count;
                    }

                    NormalPerfectCount = normalPerfect;
                    PerfectFamilyCount = perfectFamily;
                    TotalHitsCount = (int)tracker.GetTotalHits();

                    if (HitMarginCompat.RawXPerfectCode >= 0)
                        XPerfectCount = tracker.GetHits(HitMarginCompat.FromRawCode(HitMarginCompat.RawXPerfectCode));
                }
                catch (Exception e)
                {
                    ModContext.Logger?.Log($"SyncFromTracker: {e.Message}");
                }
            }
        }
    }
}
