using System;
using System.Reflection;

namespace TimingShow
{
    internal static class GameDifficulty
    {
        internal const int Unknown = -1;
        internal const int Lenient = 0;
        internal const int Normal = 1;
        internal const int Strict = 2;

        internal const float LenientThresholdBpm = 220f;
        internal const float NormalThresholdBpm = 310f;
        internal const float StrictThresholdBpm = 500f;

        private static FieldInfo _field;
        private static bool _searched;

        internal static int Read()
        {
            try
            {
                if (!_searched)
                {
                    _searched = true;
                    Type type = Type.GetType("GCS, Assembly-CSharp");
                    if (type != null)
                        _field = type.GetField("difficulty", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                }

                if (_field == null) return Unknown;
                object value = _field.GetValue(null);
                if (value == null) return Unknown;

                int code = Convert.ToInt32(value);
                return code < Lenient || code > Strict ? Unknown : code;
            }
            catch
            {
                return Unknown;
            }
        }

        internal static float ThresholdBpm(int difficulty)
        {
            switch (difficulty)
            {
                case Lenient: return LenientThresholdBpm;
                case Strict: return StrictThresholdBpm;
                default: return NormalThresholdBpm;
            }
        }

        internal static float PassBaseMs(int difficulty)
        {
            switch (difficulty)
            {
                case Lenient: return 91f;
                case Strict: return 40f;
                default: return 65f;
            }
        }

        internal static string Label(int difficulty)
        {
            switch (difficulty)
            {
                case Lenient: return "Lenient";
                case Normal: return "Normal";
                case Strict: return "Strict";
                default: return "Unknown";
            }
        }

        internal static string LocalizedLabel(int difficulty)
        {
            switch (difficulty)
            {
                case Lenient: return i18n.T("LogGraph_DifficultyLenient");
                case Normal: return i18n.T("LogGraph_DifficultyNormal");
                case Strict: return i18n.T("LogGraph_DifficultyStrict");
                default: return i18n.T("LogGraph_DifficultyUnknown");
            }
        }
    }
}
