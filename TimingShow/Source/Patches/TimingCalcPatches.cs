using HarmonyLib;
using System;
using UnityEngine;
using TimingShow.Logging;
using TimingShow.Diagnostics;

namespace TimingShow.Patches
{

    public static class TimingCalcPatches
    {
        private const int TimingScatterHardLimit = 100000;
        private static int TimingScatterWarningLimit
        {
            get
            {
                Settings settings = ModContext.Settings;
                return Mathf.Clamp(settings != null ? settings.Diag_ScatterWarningLimit : 5, 0, 100);
            }
        }
        private const float TimingScatterDedupeToleranceMs = 0.05f;

        private static int _pendingScatterSampleIndex = -1;
        private static int _pendingScatterSampleFrame = -1;
        private static int _scatterUnresolvedWarnings;
        private static int _scatterLimitWarnings;
        
        internal static void InvalidatePendingScatterSample()
        {
            _pendingScatterSampleIndex = -1;
            _pendingScatterSampleFrame = -1;
        }
        
        private static void AppendTimingScatterSample(double timing, int playerId)
        {
            if (_pendingScatterSampleIndex >= 0 && _pendingScatterSampleIndex < ModContext.TimingScatterSamples.Count)
            {
                TimingScatterSample pending = ModContext.TimingScatterSamples[_pendingScatterSampleIndex];
                bool sameFrame = _pendingScatterSampleFrame == Time.frameCount;
                bool sameTiming = Mathf.Abs(pending.OffsetMs - (float)timing) <= TimingScatterDedupeToleranceMs;

                if (sameFrame && sameTiming)
                {
                    return;
                }

                if (_scatterUnresolvedWarnings < TimingScatterWarningLimit)
                {
                    _scatterUnresolvedWarnings++;
                    ModContext.Logger?.Log($"[TimingShow] scatter sample superseded before resolve ({_scatterUnresolvedWarnings}/{TimingScatterWarningLimit})");
                }
            }

            if (ModContext.TimingScatterSamples.Count >= TimingScatterHardLimit)
            {
                InvalidatePendingScatterSample();
                if (_scatterLimitWarnings < TimingScatterWarningLimit)
                {
                    _scatterLimitWarnings++;
                    ModContext.Logger?.Log($"[TimingShow] scatter sample limit {TimingScatterHardLimit} reached, further hits dropped ({_scatterLimitWarnings}/{TimingScatterWarningLimit})");
                }
                return;
            }

            ModContext.TimingScatterSamples.Add(new TimingScatterSample
            {
                TimeMs = (float)PlayStatePatches.GetSessionTimeMs(),
                OffsetMs = (float)timing,
                Judge = HitMan.Unknown,
                IsXPerfect = false,
                HasJudge = false,
                PlayerId = playerId
            });

            _pendingScatterSampleIndex = ModContext.TimingScatterSamples.Count - 1;
            _pendingScatterSampleFrame = Time.frameCount;
        }
        
        private static void MarkPendingTimingScatterSample(HitMan judge, bool isXPerfect)
        {
            int index = _pendingScatterSampleIndex;
            InvalidatePendingScatterSample();
            if (index < 0 || index >= ModContext.TimingScatterSamples.Count) return;

            TimingScatterSample sample = ModContext.TimingScatterSamples[index];
            if (sample.HasJudge) return;

            sample.Judge = judge;
            sample.IsXPerfect = isXPerfect;
            sample.HasJudge = true;
            ModContext.TimingScatterSamples[index] = sample;
            ModContext.TimingScatterVersion++;
        }

        // timing calc
        [HarmonyPatch(typeof(scrPlanet), "SwitchChosen")]
        public static class PlanetSwitchPatch
        {
            public static void Prefix(scrPlanet __instance)
            {
                if (!ModContext.IsEnabled || scrController.instance == null) return;

                Players.CurrentId = Players.ResolvePlayerId(__instance);

                if (__instance.conductor == null || __instance.conductor.song == null) return;

                PlanetarySystem system = __instance.planetarySystem;
                if (system == null) return;

                double bpm = __instance.conductor.bpm;
                double speed = system.speed;
                double pitch = __instance.conductor.song.pitch;
                bool isCW = system.isCW;

                if (bpm * speed * pitch == 0) return;
                double diff = (__instance.angle - __instance.targetExitAngle) * (isCW ? 1.0 : -1.0) * 60000.0 / (Math.PI * bpm * speed * pitch);

                PlayerSession player = Players.Current;
                player.HasHit = true;
                player.LastTiming = diff;
                player.LastSongTimeMs = PlayStatePatches.GetSessionTimeMs();
                player.LastAngle = (__instance.angle - __instance.targetExitAngle) * (isCW ? 1.0 : -1.0) * 180.0 / Math.PI;
                player.LastBpm = bpm;
                player.LastSpeed = speed;
                player.LastPitch = pitch;
                ModContext.UIDirty = true;


                if (!HitMarginCompat.IsGame34)
                {
                    player.LastIsXP = CalcXP.IsLegacyXPerfect(diff, bpm, speed, pitch);
                    RecordSample(diff, player.LastAngle);
                }
            }

            public static void Postfix()
            {
                Players.CurrentId = Players.Unset;
            }
        }

        // r150
        [HarmonyPatch(typeof(scrMisc), "GetHitMarginInDeg")]
        public static class ScrMiscGetHitMarginInDegPatch
        {
            public static bool Prepare() => HitMarginCompat.IsGame34;

            public static void Postfix(float hitAngle, float refAngle, bool clockwise, float floorBpm, float conductorPitch)
            {
                if (!ModContext.IsEnabled) return;
                if (floorBpm == 0f || conductorPitch == 0f) return;

                double deg = (hitAngle - refAngle) * (clockwise ? 1.0 : -1.0) * 57.29578;
                double timing = deg / 180.0 / floorBpm / conductorPitch * 60000.0;
                OnGame34Timing(timing, deg);
            }
        }
        
        [HarmonyPatch(typeof(scrMisc), "GetHitMarginInSec")]
        public static class ScrMiscGetHitMarginInSecPatch
        {
            public static bool Prepare() => HitMarginCompat.IsGame34;

            public static void Postfix(double timeDiff, float floorBpm, float conductorPitch)
            {
                if (!ModContext.IsEnabled) return;
                if (floorBpm == 0f || conductorPitch == 0f) return;

                double timing = timeDiff * 1000.0;
                double deg = timeDiff * 3.0 * floorBpm * conductorPitch;
                OnGame34Timing(timing, deg);
            }
        }


        private static void OnGame34Timing(double timing, double angle)
        {
            PlayerSession player = Players.Current;
            player.HasHit = true;
            player.LastTiming = timing;
            player.LastAngle = angle;
            ModContext.UIDirty = true;
            RecordSample(timing, angle);
        }
        
        internal static void RecordSample(double timing, double angle)
        {
            if (!ModContext.IsPlaying || ModContext.Settings == null) return;

            PerfStatsRecorder.HitBegin();
            try
            {
                RecordSampleCore(timing, angle);
            }
            finally
            {
                PerfStatsRecorder.HitEnd();
            }
        }

        private static void RecordSampleCore(double timing, double angle)
        {
            bool isAuto = RDC.auto;
            bool needRecord = ModContext.Settings.ShowInWinPage || ModContext.Settings.ShowAvgHUD || ModContext.Settings.ShowURHUD || !isAuto || ModContext.Settings.LogAutoplay || ModContext.Settings.ShowXACCGraph || ModContext.Settings.ShowTimingScatter;

            PlayerSession player = Players.Current;
            if (needRecord)
            {
                player.Offsets.Add(timing);
                player.Ur.Add(timing);

                AppendTimingScatterSample(timing, player.PlayerId);
            }

            int xaccPlayerId = player.PlayerId;
            if (TryGetPercentXAcc(xaccPlayerId, out float curXAcc))
            {
                ModContext.FullXAccHistory[xaccPlayerId].Add(curXAcc);
                ModContext.XAccVersion++;
            }

            if (isAuto)
            {
                MarginTrackerAddHitPatch.ApplyAutoHit(timing, angle, ModContext.Settings.EnableLogging);
            }
        }

        private static bool TryGetPercentXAcc(int playerId, out float percentXAcc)
        {
            percentXAcc = 0f;
            try
            {
                if (scrController.instance == null) return false;

                scrMarginTracker[] trackers = scrMistakesManager.marginTrackers;
                if (trackers == null || trackers.Length == 0) return false;
                if (playerId < 0 || playerId >= trackers.Length) playerId = 0;

                scrMarginTracker tracker = trackers[playerId];
                if (tracker == null) return false;

                percentXAcc = tracker.percentXAcc * 100f;
                return true;
            }
            catch
            {
                return false;
            }
        }

        // hit
        [HarmonyPatch(typeof(scrMarginTracker), "AddHit")]
        public static class MarginTrackerAddHitPatch
        {
            public static void Prefix(HitMargin hit, scrMarginTracker __instance)
            {
                if (!ModContext.IsEnabled || !ModContext.IsPlaying) return;

                Players.CurrentId = Players.ResolvePlayerId(__instance);

                int raw = HitMarginCompat.ToRawCode(hit);

                PerfStatsRecorder.HitBegin();
                try
                {
                    ApplyHit(raw, allowLog: !RDC.auto);
                }
                finally
                {
                    PerfStatsRecorder.HitEnd();
                }
            }


            public static void ApplyAutoHit(double timing, double angle, bool allowLog)
            {
                int raw = HitMarginCompat.RawAutoCode;
                ApplyHit(raw, allowLog: allowLog, countHit: false, timingOverride: timing, angleOverride: angle);
            }

            private static void ApplyHit(int rawMargin, bool allowLog, bool countHit = true, double? timingOverride = null, double? angleOverride = null)
            {
                HitMan judge = HitMarginCompat.ToJudgeKind(rawMargin);

                PlayerSession player = Players.Current;
                player.HasHit = true;
                player.LastRawMargin = rawMargin;
                player.LastJudge = judge;
                player.LastIsXP = DetermineIsXPerfect(judge, player);

                MarkPendingTimingScatterSample(judge, player.LastIsXP);

                if (countHit)
                {
                    player.TotalHitsCount++;
                    if (HitMarginCompat.IsPerfectFamily(judge)) player.PerfectFamilyCount++;
                    if (HitMarginCompat.IsNormalPerfect(judge)) player.NormalPerfectCount++;
                    if (player.LastIsXP) player.XPerfectCount++;
                }

                if (allowLog)
                {
                    TimingLogger.LogHit(timingOverride ?? player.LastTiming, angleOverride ?? player.LastAngle, rawMargin, judge, player.LastIsXP);
                }
            }

            private static bool DetermineIsXPerfect(HitMan judge, PlayerSession player)
            {

                if (!HitMarginCompat.IsPerfectFamily(judge)) return false;

                if (HitMarginCompat.IsGame34) return judge == HitMan.XPerfect;
                if (HitMarginCompat.IsLegacy) return CalcXP.IsLegacyXPerfect(player.LastTiming, player.LastBpm, player.LastSpeed, player.LastPitch);
                return false;
            }


            public static void SyncFromTracker(scrMarginTracker tracker)
            {
                if (tracker == null || !HitMarginCompat.IsInitialized) return;

                PlayerSession player = Players.Get(Players.ResolvePlayerId(tracker));

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

                    player.NormalPerfectCount = normalPerfect;
                    player.PerfectFamilyCount = perfectFamily;
                    player.TotalHitsCount = (int)tracker.GetTotalHits();

                    if (HitMarginCompat.IsGame34 && HitMarginCompat.RawXPerfectCode >= 0)
                        player.XPerfectCount = tracker.GetHits(HitMarginCompat.FromRawCode(HitMarginCompat.RawXPerfectCode));
                }
                catch (Exception e)
                {
                    ModContext.Logger?.Log($"SyncFromTracker: {e.Message}");
                }
            }
        }
    }
}
