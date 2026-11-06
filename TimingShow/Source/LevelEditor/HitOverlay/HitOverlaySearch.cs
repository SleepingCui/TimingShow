using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace TimingShow.LevelEditor
{
    internal struct HitOverlayQuery
    {
        internal bool HasFloor;
        internal int Floor;
        internal bool HasTime;
        internal float TimeMs;
        internal float TimeToleranceMs;
        internal bool HasPlayer;
        internal int Player;
        internal bool HasOffsetMin;
        internal float OffsetMin;
        internal bool HasOffsetMax;
        internal float OffsetMax;
        internal uint JudgeMask;

        internal bool IsEmpty
        {
            get { return !HasFloor && !HasTime && !HasPlayer && !HasOffsetMin && !HasOffsetMax && JudgeMask == 0u; }
        }

        internal static HitOverlayQuery Parse(string text)
        {
            HitOverlayQuery query = new HitOverlayQuery();
            query.TimeToleranceMs = 50f;
            if (string.IsNullOrWhiteSpace(text)) return query;

            string[] tokens = text.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < tokens.Length; i++)
                ApplyToken(ref query, tokens[i]);

            return query;
        }

        internal bool Matches(TimingScatterSample sample)
        {
            if (HasFloor)
            {
                if (!sample.HasFloorId) return false;
                if (sample.FloorId != Floor) return false;
            }

            if (HasTime)
            {
                if (Mathf.Abs(sample.TimeMs - TimeMs) > TimeToleranceMs) return false;
            }

            if (HasPlayer && sample.PlayerId != Player) return false;

            if (HasOffsetMin || HasOffsetMax)
            {
                float magnitude = Mathf.Abs(sample.OffsetMs);
                if (float.IsNaN(magnitude) || float.IsInfinity(magnitude)) return false;
                if (HasOffsetMin && magnitude < OffsetMin) return false;
                if (HasOffsetMax && magnitude > OffsetMax) return false;
            }

            if (JudgeMask != 0u && (JudgeMask & HitOverlayFilter.BitOf(sample.Judge)) == 0u && !(sample.IsXPerfect && JudgeMask == HitOverlayFilter.BitOf(HitMan.PerfectMinus)))
                return false;

            return true;
        }

        private static void ApplyToken(ref HitOverlayQuery query, string token)
        {
            if (string.IsNullOrEmpty(token)) return;

            string value = token.ToLowerInvariant();
            if (value.StartsWith("#")) { value = value.Substring(1); }
            else if (value.StartsWith("f:")) { value = value.Substring(2); }
            else if (value.StartsWith("t:"))
            {
                float time;
                if (TryParseFloat(value.Substring(2), out time))
                {
                    query.HasTime = true;
                    query.TimeMs = time;
                }
                return;
            }
            else if (value.StartsWith("p:"))
            {
                int player;
                if (int.TryParse(value.Substring(2), NumberStyles.Integer, CultureInfo.InvariantCulture, out player))
                {
                    query.HasPlayer = true;
                    query.Player = player;
                }
                return;
            }
            else if (value.StartsWith("o:"))
            {
                ApplyOffset(ref query, value.Substring(2));
                return;
            }
            else if (value.StartsWith(">=") || value.StartsWith("<=") || value.StartsWith(">") || value.StartsWith("<"))
            {
                ApplyOffset(ref query, value);
                return;
            }

            uint judgeBit = JudgeBitOf(value);
            if (judgeBit != 0u)
            {
                query.JudgeMask |= judgeBit;
                return;
            }

            if (value.Contains("-") && !value.StartsWith("-"))
            {
                string[] range = value.Split('-');
                if (range.Length == 2)
                {
                    float min, max;
                    if (TryParseFloat(range[0], out min) && TryParseFloat(range[1], out max))
                    {
                        query.HasOffsetMin = true;
                        query.OffsetMin = Mathf.Min(min, max);
                        query.HasOffsetMax = true;
                        query.OffsetMax = Mathf.Max(min, max);
                        return;
                    }
                }
            }

            float number;
            if (TryParseFloat(value, out number))
            {
                if (value.Contains(".") || value.StartsWith("-") || value.StartsWith("+"))
                {
                    query.HasOffsetMin = true;
                    query.HasOffsetMax = true;
                    query.OffsetMin = Mathf.Abs(number) - 2f;
                    query.OffsetMax = Mathf.Abs(number) + 2f;
                }
                else
                {
                    query.HasFloor = true;
                    query.Floor = Mathf.RoundToInt(number);
                }
            }
        }

        private static void ApplyOffset(ref HitOverlayQuery query, string value)
        {
            if (value.StartsWith(">="))
            {
                float number;
                if (TryParseFloat(value.Substring(2), out number))
                {
                    query.HasOffsetMin = true;
                    query.OffsetMin = number;
                }
                return;
            }

            if (value.StartsWith("<="))
            {
                float number;
                if (TryParseFloat(value.Substring(2), out number))
                {
                    query.HasOffsetMax = true;
                    query.OffsetMax = number;
                }
                return;
            }

            if (value.StartsWith(">"))
            {
                float number;
                if (TryParseFloat(value.Substring(1), out number))
                {
                    query.HasOffsetMin = true;
                    query.OffsetMin = number + 0.01f;
                }
                return;
            }

            if (value.StartsWith("<"))
            {
                float number;
                if (TryParseFloat(value.Substring(1), out number))
                {
                    query.HasOffsetMax = true;
                    query.OffsetMax = number - 0.01f;
                }
                return;
            }

            float single;
            if (TryParseFloat(value, out single))
            {
                query.HasOffsetMin = true;
                query.HasOffsetMax = true;
                query.OffsetMin = Mathf.Abs(single) - 2f;
                query.OffsetMax = Mathf.Abs(single) + 2f;
            }
        }

        private static uint JudgeBitOf(string value)
        {
            switch (value)
            {
                case "xp": return HitOverlayFilter.BitOf(HitMan.XPerfect);
                case "perfect":
                    return HitOverlayFilter.BitOf(HitMan.EarlyPerfect) | HitOverlayFilter.BitOf(HitMan.PerfectMinus) |
                           HitOverlayFilter.BitOf(HitMan.XPerfect) | HitOverlayFilter.BitOf(HitMan.PerfectPlus) |
                           HitOverlayFilter.BitOf(HitMan.LatePerfect);
                case "perfectminus": return HitOverlayFilter.BitOf(HitMan.PerfectMinus);
                case "perfectplus": return HitOverlayFilter.BitOf(HitMan.PerfectPlus);
                case "early": return HitOverlayFilter.BitOf(HitMan.EarlyPerfect);
                case "late": return HitOverlayFilter.BitOf(HitMan.LatePerfect);
                case "veryearly": return HitOverlayFilter.BitOf(HitMan.VeryEarly);
                case "verylate": return HitOverlayFilter.BitOf(HitMan.VeryLate);
                case "toearly": return HitOverlayFilter.BitOf(HitMan.TooEarly);
                case "toolate": return HitOverlayFilter.BitOf(HitMan.TooLate);
                case "multipress": return HitOverlayFilter.BitOf(HitMan.Multipress);
                case "fail": return HitOverlayFilter.BitOf(HitMan.FailMiss) | HitOverlayFilter.BitOf(HitMan.FailOverload) | HitOverlayFilter.BitOf(HitMan.OverPress);
                case "auto": return HitOverlayFilter.BitOf(HitMan.Auto);
                case "midspin": return HitOverlayFilter.BitOf(HitMan.Midspin);
                default: return 0u;
            }
        }

        private static bool TryParseFloat(string text, out float value)
        {
            return float.TryParse(text.TrimEnd('m', 's'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }

    internal static class HitOverlaySearch
    {
        internal static int Collect(TimingScatterSample[] samples, bool[] valid, HitOverlayQuery query, List<int> results, int max)
        {
            results.Clear();
            if (samples == null || query.IsEmpty) return 0;

            int total = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                if (valid != null && (i >= valid.Length || !valid[i])) continue;
                if (!query.Matches(samples[i])) continue;

                total++;
                if (results.Count < max) results.Add(i);
            }

            return total;
        }

        internal static bool[] BuildMask(TimingScatterSample[] samples, bool[] valid, HitOverlayQuery query)
        {
            if (samples == null) return null;

            bool[] mask = new bool[samples.Length];
            if (query.IsEmpty) return null;

            for (int i = 0; i < samples.Length; i++)
            {
                if (valid != null && (i >= valid.Length || !valid[i])) continue;
                mask[i] = query.Matches(samples[i]);
            }

            return mask;
        }
    }
}
