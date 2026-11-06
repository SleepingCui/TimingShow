using System.Collections.Generic;
using TMPro;
using TimingShow.HUD;
using UnityEngine;
using UnityEngine.UI;

namespace TimingShow.LevelEditor
{
    internal struct HitOverlayFilter
    {
        internal uint JudgeMask;
        internal bool XpOnly;
        internal float MinAbsOffset;
        internal float MaxAbsOffset;
        internal int PlayerMask;

        internal bool IsActive
        {
            get { return JudgeMask != 0u || XpOnly || MinAbsOffset > 0f || MaxAbsOffset < float.MaxValue || PlayerMask != 0; }
        }

        internal static HitOverlayFilter CreateDefault()
        {
            HitOverlayFilter filter = new HitOverlayFilter();
            filter.MaxAbsOffset = float.MaxValue;
            return filter;
        }

        internal static uint BitOf(HitMan judge)
        {
            int index = (int)judge + 1;
            if (index < 0 || index > 16) return 0u;
            return 1u << index;
        }

        internal bool Matches(TimingScatterSample sample)
        {
            if (JudgeMask != 0u && (JudgeMask & BitOf(sample.Judge)) == 0u) return false;
            if (XpOnly && !sample.IsXPerfect) return false;

            if (MinAbsOffset > 0f || MaxAbsOffset < float.MaxValue)
            {
                float magnitude = Mathf.Abs(sample.OffsetMs);
                if (float.IsNaN(magnitude) || float.IsInfinity(magnitude)) return false;
                if (magnitude < MinAbsOffset || magnitude > MaxAbsOffset) return false;
            }

            if (PlayerMask != 0)
            {
                if (sample.PlayerId < 0 || sample.PlayerId >= 32) return false;
                if ((PlayerMask & (1 << sample.PlayerId)) == 0) return false;
            }

            return true;
        }
    }

    internal struct HitJudgeChip
    {
        internal HitMan Judge;
        internal bool IsXp;
        internal string LabelKey;
        internal Color Color;
    }

    internal static class HitOverlayCatalog
    {
        private static readonly HitMan[] DisplayOrder =
        {
            HitMan.XPerfect,
            HitMan.PerfectPlus,
            HitMan.PerfectMinus,
            HitMan.EarlyPerfect,
            HitMan.LatePerfect,
            HitMan.VeryEarly,
            HitMan.VeryLate,
            HitMan.TooEarly,
            HitMan.TooLate,
            HitMan.Multipress,
            HitMan.FailMiss,
            HitMan.FailOverload,
            HitMan.OverPress,
            HitMan.Auto,
            HitMan.Midspin,
            HitMan.FailedFloor
        };

        internal static string LabelKey(HitMan judge, bool isXp)
        {
            if (isXp && !HitMarginCompat.IsGame34 && HitMarginCompat.IsPerfectFamily(judge))
                return "Toggle_XPerfect";

            switch (judge)
            {
                case HitMan.TooEarly: return "Toggle_TooEarly";
                case HitMan.VeryEarly: return "Toggle_VeryEarly";
                case HitMan.EarlyPerfect: return "Toggle_EarlyPerfect";
                case HitMan.PerfectMinus: return HitMarginCompat.IsNormalPerfect(judge) ? "Toggle_Perfect" : "Toggle_PerfectMinus";
                case HitMan.XPerfect: return "Toggle_XPerfect";
                case HitMan.PerfectPlus: return "Toggle_PerfectPlus";
                case HitMan.LatePerfect: return "Toggle_LatePerfect";
                case HitMan.VeryLate: return "Toggle_VeryLate";
                case HitMan.TooLate: return "Toggle_TooLate";
                case HitMan.Multipress: return "Toggle_Multipress";
                case HitMan.FailMiss: return "Toggle_FailMiss";
                case HitMan.FailOverload: return "Toggle_FailOverload";
                case HitMan.OverPress: return "Toggle_OverPress";
                case HitMan.Auto: return "Toggle_Auto";
                case HitMan.Midspin: return "Judge_Midspin";
                case HitMan.FailedFloor: return "Judge_FailedFloor";
                default: return "Judge_Unknown";
            }
        }

        internal static HitJudgeChip[] BuildChips()
        {
            HitMarginCompat.Initialize(false);

            bool[] present = new bool[DisplayOrder.Length];
            int[] rawValues;
            try { rawValues = HitMarginCompat.AllRawValues; }
            catch (System.Exception) { rawValues = null; }

            if (rawValues != null && rawValues.Length > 0)
            {
                for (int i = 0; i < rawValues.Length; i++)
                {
                    HitMan judge = HitMarginCompat.ToJudgeKind(rawValues[i]);
                    int index = IndexOf(judge);
                    if (index >= 0) present[index] = true;
                }
            }

            if (rawValues == null || rawValues.Length == 0 || !HasAny(present))
            {
                for (int i = 0; i < DisplayOrder.Length; i++)
                    present[i] = !HitMarginCompat.IsGame34 ? IsLegacyJudge(DisplayOrder[i]) : true;
            }

            List<HitJudgeChip> chips = new List<HitJudgeChip>(DisplayOrder.Length + 1);
            bool legacy = !HitMarginCompat.IsGame34;
            for (int i = 0; i < DisplayOrder.Length; i++)
            {
                if (!present[i]) continue;

                HitJudgeChip chip = new HitJudgeChip();
                chip.Judge = DisplayOrder[i];
                chip.IsXp = false;
                chip.LabelKey = LabelKey(chip.Judge, false);
                chip.Color = JudgeColor(chip.Judge, false);
                chips.Add(chip);
            }

            if (legacy)
            {
                HitJudgeChip xp = new HitJudgeChip();
                xp.Judge = HitMan.PerfectMinus;
                xp.IsXp = true;
                xp.LabelKey = "Toggle_XPerfect";
                xp.Color = JudgeColor(xp.Judge, true);
                chips.Add(xp);
            }

            return chips.ToArray();
        }

        internal static Color JudgeColor(HitMan judge, bool isXp)
        {
            try { return JColors.GetColor(judge, isXp, true); }
            catch (System.Exception) { return HitOverlaySkin.TextSecondary; }
        }

        internal static int[] CountByJudge(TimingScatterSample[] samples, bool[] valid, int count)
        {
            int[] counts = new int[17];
            if (samples == null) return counts;

            int limit = Mathf.Min(count, samples.Length);
            for (int i = 0; i < limit; i++)
            {
                if (valid == null || i >= valid.Length || !valid[i]) continue;

                int index = (int)samples[i].Judge + 1;
                if (index >= 0 && index < counts.Length) counts[index]++;
            }

            return counts;
        }

        private static bool IsLegacyJudge(HitMan judge)
        {
            return judge != HitMan.XPerfect && judge != HitMan.PerfectPlus && judge != HitMan.PerfectMinus && judge != HitMan.Midspin && judge != HitMan.FailedFloor;
        }

        private static bool HasAny(bool[] values)
        {
            for (int i = 0; i < values.Length; i++)
                if (values[i]) return true;

            return false;
        }

        private static int IndexOf(HitMan judge)
        {
            for (int i = 0; i < DisplayOrder.Length; i++)
                if (DisplayOrder[i] == judge) return i;

            return -1;
        }
    }
}
