using System;
using System.Collections.Generic;

namespace TimingShow.Update
{
    internal readonly struct GameVersion : IComparable<GameVersion>
    {
        private static readonly string[] NoTokens = new string[0];

        private readonly string[] _tokens;
        private readonly string _text;

        private GameVersion(string text, string[] tokens)
        {
            _text = text;
            _tokens = tokens;
        }

        public bool IsEmpty => _tokens == null || _tokens.Length == 0;

        public static bool TryParse(string text, out GameVersion version)
        {
            version = default(GameVersion);
            if (string.IsNullOrEmpty(text)) return false;
            string trimmed = text.Trim();
            if (trimmed.Length == 0) return false;
            version = new GameVersion(trimmed, Tokenize(trimmed));
            return version._tokens.Length > 0;
        }

        public int CompareTo(GameVersion other)
        {
            string[] left = _tokens ?? NoTokens;
            string[] right = other._tokens ?? NoTokens;
            int count = Math.Max(left.Length, right.Length);
            for (int i = 0; i < count; i++)
            {
                string a = i < left.Length ? left[i] : null;
                string b = i < right.Length ? right[i] : null;
                int result = CompareToken(a, b);
                if (result != 0) return result;
            }
            return 0;
        }

        public override string ToString()
        {
            return _text ?? string.Empty;
        }

        private static int CompareToken(string left, string right)
        {
            if (left == null && right == null) return 0;
            if (left == null) return IsZero(right) ? 0 : -1;
            if (right == null) return IsZero(left) ? 0 : 1;
            if (IsNumber(left) && IsNumber(right))
            {
                if (left.Length != right.Length) return left.Length < right.Length ? -1 : 1;
                return string.CompareOrdinal(left, right);
            }
            return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsNumber(string token)
        {
            return token.Length > 0 && char.IsDigit(token[0]);
        }

        private static bool IsZero(string token)
        {
            return IsNumber(token) && token == "0";
        }

        private static string[] Tokenize(string text)
        {
            List<string> tokens = new List<string>();
            int index = 0;
            while (index < text.Length)
            {
                char current = text[index];
                if (!char.IsLetterOrDigit(current))
                {
                    index++;
                    continue;
                }
                bool digits = char.IsDigit(current);
                int start = index;
                while (index < text.Length && char.IsDigit(text[index]) == digits) index++;
                string token = text.Substring(start, index - start);
                string normalized = digits ? token : token.ToLowerInvariant();
                if (tokens.Count == 0 && !digits && (normalized == "r" || normalized == "v")) continue;
                tokens.Add(digits ? TrimZeros(normalized) : normalized);
            }
            return tokens.ToArray();
        }

        private static string TrimZeros(string digits)
        {
            int index = 0;
            while (index < digits.Length - 1 && digits[index] == '0') index++;
            return index == 0 ? digits : digits.Substring(index);
        }
    }

    internal sealed class GameVersionConstraint
    {
        private sealed class Clause
        {
            public string Operator;
            public GameVersion Version;
        }

        private static readonly string[] Operators = { ">=", "<=", "==", "!=", ">", "<", "=" };

        private static readonly GameVersionConstraint Any = new GameVersionConstraint(new Clause[0], "*");

        private readonly Clause[] _clauses;
        private readonly string _text;

        private GameVersionConstraint(Clause[] clauses, string text)
        {
            _clauses = clauses;
            _text = text;
        }

        public bool IsAny => _clauses.Length == 0;

        public static bool TryParse(string text, out GameVersionConstraint constraint)
        {
            constraint = null;
            string trimmed = text == null ? string.Empty : text.Trim();
            if (trimmed.Length == 0 || trimmed == "*")
            {
                constraint = Any;
                return true;
            }

            string[] parts = trimmed.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            List<Clause> clauses = new List<Clause>(parts.Length);
            foreach (string part in parts)
            {
                Clause clause = ParseClause(part.Trim());
                if (clause == null) return false;
                clauses.Add(clause);
            }
            if (clauses.Count == 0) return false;
            constraint = new GameVersionConstraint(clauses.ToArray(), trimmed);
            return true;
        }

        public bool IsSatisfiedBy(GameVersion version)
        {
            foreach (Clause clause in _clauses)
            {
                int comparison = version.CompareTo(clause.Version);
                switch (clause.Operator)
                {
                    case ">=":
                        if (comparison < 0) return false;
                        break;
                    case "<=":
                        if (comparison > 0) return false;
                        break;
                    case ">":
                        if (comparison <= 0) return false;
                        break;
                    case "<":
                        if (comparison >= 0) return false;
                        break;
                    case "!=":
                        if (comparison == 0) return false;
                        break;
                    default:
                        if (comparison != 0) return false;
                        break;
                }
            }
            return true;
        }

        public override string ToString()
        {
            return _text;
        }

        private static Clause ParseClause(string text)
        {
            if (text.Length == 0) return null;
            foreach (string op in Operators)
            {
                if (!text.StartsWith(op, StringComparison.Ordinal)) continue;
                GameVersion version;
                if (!GameVersion.TryParse(text.Substring(op.Length), out version)) return null;
                return new Clause { Operator = op, Version = version };
            }
            GameVersion bare;
            if (!GameVersion.TryParse(text, out bare)) return null;
            return new Clause { Operator = "=", Version = bare };
        }
    }
}
