using System;
using System.Text;
using UnityEngine;

namespace TimingShow
{
    public static class Coop
    {
        public const string Placeholder = "--";

        public const string Separator = ",";

        private static readonly Color[] PlayerColors =
        {
            new Color(1.00f, 0.35f, 0.35f, 1f),
            new Color(0.31f, 0.76f, 0.97f, 1f),
            new Color(1.00f, 0.84f, 0.31f, 1f),
            new Color(0.70f, 0.53f, 1.00f, 1f),
        };

        public static Color PlayerColor(int playerId)
        {
            if (playerId < 0) playerId = 0;
            return PlayerColors[playerId % PlayerColors.Length];
        }

        private static readonly StringBuilder _sb = new StringBuilder(160);

        public static bool IsCoop
        {
            get { return Players.IsCoop; }
        }

        public static int ActiveCount
        {
            get { return IsCoop ? Players.Count : 1; }
        }

        public static string Label(int playerId)
        {
            return "P" + (playerId + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public static string Join(Func<PlayerSession, string> format)
        {
            return Join(true, format);
        }

        public static string Join(bool perPlayer, Func<PlayerSession, string> format)
        {
            if (format == null) return string.Empty;
            if (!perPlayer || !IsCoop) return Text(format, Players.Get(0));

            int count = Players.Count;
            _sb.Length = 0;
            for (int i = 0; i < count; i++)
            {
                if (i > 0) _sb.Append(Separator);
                _sb.Append(Text(format, Players.Get(i)));
            }
            return _sb.ToString();
        }

        public static string JoinLines(Func<PlayerSession, string> format, string lineIndent = "")
        {
            return JoinLines(true, format, lineIndent);
        }

        public static string JoinLines(bool perPlayer, Func<PlayerSession, string> format, string lineIndent = "")
        {
            if (format == null) return string.Empty;

            string indent = lineIndent ?? string.Empty;
            int count = (perPlayer && IsCoop) ? Players.Count : 1;

            _sb.Length = 0;
            for (int i = 0; i < count; i++)
            {
                string line = format(Players.Get(i));
                if (string.IsNullOrEmpty(line)) continue;

                if (_sb.Length > 0) _sb.Append('\n');
                _sb.Append(indent);
                if (count > 1)
                {
                    _sb.Append(Label(i));
                    _sb.Append("  ");
                }
                _sb.Append(line);
            }
            return _sb.ToString();
        }

        private static string Text(Func<PlayerSession, string> format, PlayerSession player)
        {
            if (IsCoop && !player.HasHit) return Placeholder;
            return format(player) ?? string.Empty;
        }
    }
}
