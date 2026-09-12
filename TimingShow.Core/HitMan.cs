using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace TimingShow
{
    public enum HitMan
    {
        Unknown = -1,
        TooEarly = 0,
        VeryEarly = 1,
        EarlyPerfect = 2,
        PerfectMinus = 3,
        LegacyPerfect = 3,
        XPerfect = 4,
        PerfectPlus = 5,
        LatePerfect = 6,
        VeryLate = 7,
        TooLate = 8,
        Multipress = 9,
        FailMiss = 10,
        FailOverload = 11,
        Auto = 12,
        OverPress = 13,
        Midspin = 14,
        FailedFloor = 15
    }

    public enum HitMarginVersion
    {
        Unknown = 0,
        Legacy = 1,
        Game34 = 2
    }

    
    public static class HitMarginCompat
    {
        public const int JudgeCodeVersion = 1;

        private static readonly HitMan[] EmptyMap = new HitMan[0];
        private static readonly int[] EmptyInts = new int[0];

        private static HitMan[] _rawToJudge = EmptyMap;
        private static int[] _perfectFamilyRaw = EmptyInts;
        private static int[] _normalPerfectRaw = EmptyInts;
        private static string _signature = string.Empty;

        public static HitMarginVersion Version { get; private set; } = HitMarginVersion.Unknown;
        public static bool IsInitialized { get; private set; }

        public static bool IsGame34 => Version == HitMarginVersion.Game34;
        public static bool IsLegacy => Version == HitMarginVersion.Legacy;
        public static int RawAutoCode { get; private set; } = -1;
        public static int RawXPerfectCode { get; private set; } = -1;
        public static int[] AllRawValues { get; private set; } = EmptyInts;
        public static int[] PerfectFamilyRawValues => _perfectFamilyRaw;
        public static bool HasNativeXPerfect => IsGame34;


        
        public static void Initialize(bool force = false)
        {
            string signature;
            Dictionary<string, int> namesToRaw;
            FieldInfo[] fields;
            try
            {
                fields = typeof(HitMargin).GetFields(BindingFlags.Public | BindingFlags.Static);
                namesToRaw = new Dictionary<string, int>(fields.Length, StringComparer.Ordinal);
                var sb = new StringBuilder();
                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo f = fields[i];
                    if (!f.IsLiteral) continue;
                    int raw;
                    try { raw = Convert.ToInt32(f.GetRawConstantValue()); }
                    catch { continue; }

                    namesToRaw[f.Name] = raw;
                    sb.Append(f.Name).Append('=').Append(raw).Append(';');
                }
                signature = sb.ToString();
            }
            catch (Exception e)
            {
                Version = HitMarginVersion.Unknown;
                _rawToJudge = EmptyMap;
                _perfectFamilyRaw = EmptyInts;
                _normalPerfectRaw = EmptyInts;
                AllRawValues = EmptyInts;
                RawAutoCode = -1;
                RawXPerfectCode = -1;
                _signature = string.Empty;
                IsInitialized = true;
                return;
            }

            if (IsInitialized && !force && signature == _signature) return;

            _signature = signature;
            Version = DetectVersion(namesToRaw);
            RawAutoCode = namesToRaw.TryGetValue("Auto", out int autoRaw) ? autoRaw : -1;
            RawXPerfectCode = namesToRaw.TryGetValue("XPerfect", out int xpRaw) ? xpRaw : -1;

            int maxRaw = -1;
            foreach (var kv in namesToRaw) if (kv.Value > maxRaw) maxRaw = kv.Value;

            _rawToJudge = maxRaw < 0 ? EmptyMap : new HitMan[maxRaw + 1];
            for (int i = 0; i < _rawToJudge.Length; i++) _rawToJudge[i] = HitMan.Unknown;

            var rawValues = new List<int>(namesToRaw.Count);
            var perfectFamily = new List<int>();
            var normalPerfect = new List<int>();

            foreach (var kv in namesToRaw)
            {
                int raw = kv.Value;
                if (raw < 0) continue;

                HitMan kind = FromName(kv.Key);
                _rawToJudge[raw] = kind;
                rawValues.Add(raw);

                if (IsPerfectFamily(kind)) perfectFamily.Add(raw);
                if (IsNormalPerfect(kind)) normalPerfect.Add(raw);
            }

            rawValues.Sort();
            perfectFamily.Sort();
            normalPerfect.Sort();

            AllRawValues = rawValues.ToArray();
            _perfectFamilyRaw = perfectFamily.ToArray();
            _normalPerfectRaw = normalPerfect.ToArray();
            IsInitialized = true;
        }

        private static HitMarginVersion DetectVersion(Dictionary<string, int> namesToRaw)
        {
            // 3.4
            if (namesToRaw.ContainsKey("XPerfect") ||
                namesToRaw.ContainsKey("PerfectMinus") ||
                namesToRaw.ContainsKey("PerfectPlus") ||
                namesToRaw.ContainsKey("Midspin") ||
                namesToRaw.ContainsKey("FailedFloor"))
                return HitMarginVersion.Game34;
            
            if (namesToRaw.ContainsKey("Perfect") &&
                namesToRaw.ContainsKey("LatePerfect") &&
                namesToRaw.ContainsKey("Auto") &&
                namesToRaw.ContainsKey("OverPress"))
                return HitMarginVersion.Legacy;

            return HitMarginVersion.Unknown;
        }

        private static HitMan FromName(string name)
        {
            switch (name)
            {
                case "TooEarly": return HitMan.TooEarly;
                case "VeryEarly": return HitMan.VeryEarly;
                case "EarlyPerfect": return HitMan.EarlyPerfect;
                case "Perfect": return HitMan.LegacyPerfect;
                case "PerfectMinus": return HitMan.PerfectMinus;
                case "XPerfect": return HitMan.XPerfect;
                case "PerfectPlus": return HitMan.PerfectPlus;
                case "LatePerfect": return HitMan.LatePerfect;
                case "VeryLate": return HitMan.VeryLate;
                case "TooLate": return HitMan.TooLate;
                case "Multipress": return HitMan.Multipress;
                case "FailMiss": return HitMan.FailMiss;
                case "FailOverload": return HitMan.FailOverload;
                case "Auto": return HitMan.Auto;
                case "OverPress": return HitMan.OverPress;
                case "Midspin": return HitMan.Midspin;
                case "FailedFloor": return HitMan.FailedFloor;
                default: return HitMan.Unknown;
            }
        }
        
        
        
        public static HitMan ToJudgeKind(int raw)
        {
            if (!IsInitialized) Initialize();
            HitMan[] map = _rawToJudge;
            if (raw < 0 || raw >= map.Length) return HitMan.Unknown;
            return map[raw];
        }
        
        
        public static int ToRawCode(HitMargin hit) => (int)hit;
        
        public static HitMargin FromRawCode(int raw) => (HitMargin)raw;
        
        
        
        
        
        public static bool IsPerfectFamily(HitMan kind)
        {
            switch (kind)
            {
                case HitMan.EarlyPerfect:
                case HitMan.PerfectMinus: 
                case HitMan.XPerfect:
                case HitMan.PerfectPlus:
                case HitMan.LatePerfect:
                    return true;
                default:
                    return false;
            }
        }


        public static bool IsFailFamily(HitMan kind)
        {
            switch (kind)
            {
                case HitMan.FailMiss:
                case HitMan.FailOverload:
                case HitMan.OverPress:
                case HitMan.FailedFloor:
                    return true;
                default:
                    return false;
            }
        }


        public static bool IsNormalPerfect(HitMan kind)
        {
            if (kind == HitMan.LegacyPerfect) return true;
            return IsGame34 && kind == HitMan.PerfectPlus;
        }

    }
}
