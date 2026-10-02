using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace TimingShow
{
    public static class JColors
    {
        public static readonly Color Gray = new Color(0.5f, 0.5f, 0.5f, 1f);
        public static readonly Color XPerfectFallback = new Color32(77, 204, 255, byte.MaxValue);

        private static readonly Dictionary<string, Color> Cache = new Dictionary<string, Color>(StringComparer.Ordinal);
        private static readonly HashSet<string> Missing = new HashSet<string>(StringComparer.Ordinal);

        private static object _colors;
        private static bool _colorsResolved;
        
        public static Color GetColor(HitMargin? judge)
        {
            if (judge == null) return Gray;

            HitMargin m = judge.Value;
            if (m == HitMargin.XPerfect)
                return GetXPerfectColor();

            if (m.IsPerfectFamily())
                return GetPerfectFamilyColor(m);

            if (m.IsFailFamily()) return GetGameColor("colourFail");

            switch (m)
            {
                case HitMargin.TooEarly: return GetGameColor("colourTooEarly");
                case HitMargin.VeryEarly: return GetGameColor("colourVeryEarly");
                case HitMargin.VeryLate: return GetGameColor("colourVeryLate");
                case HitMargin.TooLate: return GetGameColor("colourTooLate");
                case HitMargin.Multipress: return GetGameColor("colourMultipress");
                case HitMargin.Midspin: return GetGameColor("colourMidspin");
                case HitMargin.FailedFloor: return GetGameColor("colourFailedFloor");
                case HitMargin.Auto: return Gray;
                default: return Gray;
            }
        }
        
        private static Color GetXPerfectColor()
        {
            if (TryResolve("colourXPerfect", out Color color)) return color;
            return XPerfectFallback;
        }

        internal static Color GetPerfectFamilyColor(HitMargin judge)
        {
            switch (judge)
            {
                case HitMargin.PerfectMinus:
                    return GetGameColor("colourPerfectMinus", "colourPerfect");
                case HitMargin.PerfectPlus:
                    return GetGameColor("colourPerfectPlus", "colourPerfect");
                case HitMargin.EarlyPerfect:
                case HitMargin.LatePerfect:
                default:
                    return GetGameColor("colourPerfect");
            }
        }
        
        private static Color GetGameColor(string name) => GetGameColor(name, null);
        private static Color GetGameColor(string name, string fallbackName)
        {
            if (TryResolve(name, out Color color)) return color;
            if (fallbackName != null && TryResolve(fallbackName, out Color fallback)) return fallback;
            return Gray;
        }

        private static bool TryResolve(string name, out Color color)
        {
            if (Cache.TryGetValue(name, out color)) return true;
            if (Missing.Contains(name)) return false;

            Color? found = ReadColorField(name);
            if (found.HasValue)
            {
                Cache[name] = found.Value;
                color = found.Value;
                return true;
            }
            Missing.Add(name);
            return false;
        }

        private static Color? ReadColorField(string fieldName)
        {
            try
            {
                object colors = GetColorsObject();
                if (colors == null) return null;

                object v = ReadMember(colors.GetType(), colors, fieldName, BindingFlags.Public | BindingFlags.Instance);
                if (v is Color c) return c;
                if (v is Color32 c32) return c32;
            }
            catch
            {
                
            }
            return null;
        }
        
        private static object ReadMember(Type type, object target, string name, BindingFlags flags)
        {
            try
            {
                FieldInfo f = type.GetField(name, flags);
                if (f != null) return f.GetValue(target);

                PropertyInfo p = type.GetProperty(name, flags);
                if (p != null && p.CanRead && p.GetIndexParameters().Length == 0)
                    return p.GetValue(target, null);
            }
            catch
            {
            }
            return null;
        }

        private static object GetColorsObject()
        {
            if (_colorsResolved) return _colors;
            _colorsResolved = true;
            try
            {

                _colors = ReadMember(typeof(RDC), null, "hitMarginColoursBySettings", BindingFlags.Public | BindingFlags.Static);
                if (_colors != null) return _colors;
                
                object data = ReadMember(typeof(RDConstants), null, "data", BindingFlags.Public | BindingFlags.Static);
                if (data == null) return null;

                _colors = ReadMember(data.GetType(), data, "hitMarginColours", BindingFlags.Public | BindingFlags.Instance);
            }
            catch
            {
                _colors = null;
            }
            return _colors;
        }
        
        public static void ResetCache()
        {
            Cache.Clear();
            Missing.Clear();
            _colors = null;
            _colorsResolved = false;
        }
    }
}
