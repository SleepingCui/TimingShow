using UnityEngine;

namespace TimingShow.HUD
{
    internal static class LogScatterJudgeLegend
    {
        internal const int Count = 6;
        internal const int AllMask = (1 << Count) - 1;

        internal const int XPerfect = 0;
        internal const int Perfect = 1;
        internal const int Early = 2;
        internal const int Late = 3;
        internal const int Miss = 4;
        internal const int Other = 5;

        private static readonly string[] LabelKeys =
        {
            "LogGraph_LegendXPerfect",
            "LogGraph_LegendPerfect",
            "LogGraph_LegendEarly",
            "LogGraph_LegendLate",
            "LogGraph_LegendMiss",
            "LogGraph_LegendOther"
        };

        internal static string LabelKey(int index)
        {
            return index >= 0 && index < LabelKeys.Length ? LabelKeys[index] : string.Empty;
        }

        internal static Color SwatchColor(int index)
        {
            return JColors.GetColor(RepresentativeJudge(index), false, true);
        }

        internal static int Classify(HitMan judge, bool isXPerfect)
        {
            if (judge == HitMan.XPerfect) return XPerfect;

            if (HitMarginCompat.IsPerfectFamily(judge))
                return !HitMarginCompat.IsGame34 && isXPerfect ? XPerfect : Perfect;

            switch (judge)
            {
                case HitMan.TooEarly:
                case HitMan.VeryEarly:
                    return Early;
                case HitMan.VeryLate:
                case HitMan.TooLate:
                    return Late;
            }

            return HitMarginCompat.IsFailFamily(judge) ? Miss : Other;
        }

        internal static bool IsVisible(int mask, HitMan judge, bool isXPerfect)
        {
            return (mask & (1 << Classify(judge, isXPerfect))) != 0;
        }

        private static HitMan RepresentativeJudge(int index)
        {
            switch (index)
            {
                case XPerfect: return HitMan.XPerfect;
                case Perfect: return HitMan.EarlyPerfect;
                case Early: return HitMan.TooEarly;
                case Late: return HitMan.TooLate;
                case Miss: return HitMan.FailMiss;
                default: return HitMan.Multipress;
            }
        }
    }
}
