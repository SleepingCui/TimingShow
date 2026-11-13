using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TimingShow.Update
{
    internal sealed class PendingFileEntry
    {
        public string RelativePath { get; set; }

        public long Size { get; set; }

        public string Sha256 { get; set; }
    }

    internal sealed class PendingUpdatePlan
    {
        public int Schema { get; set; }

        public string Id { get; set; }

        public string Version { get; set; }

        public string FromVersion { get; set; }

        public string Target { get; set; }

        public string ModPath { get; set; }

        public string Created { get; set; }

        public List<PendingFileEntry> Files { get; set; }
    }

    internal static class PendingUpdateFile
    {
        public const int SchemaVersion = 2;
        public const string FileName = "pending.json";

        private const string TemporarySuffix = ".tmp";
        private const string UmmTargetValue = "Umm";
        private const string MelonTargetValue = "Melon";

        public static string GetPath(string updateRoot)
        {
            return Path.Combine(updateRoot, FileName);
        }

        public static bool Exists(string updateRoot)
        {
            return !string.IsNullOrEmpty(updateRoot) && File.Exists(GetPath(updateRoot));
        }

        public static void Write(string updateRoot, PendingUpdatePlan plan)
        {
            Directory.CreateDirectory(updateRoot);
            string path = GetPath(updateRoot);
            string temporary = path + TemporarySuffix;
            File.WriteAllText(temporary, Serialize(plan), new UTF8Encoding(false));
            try
            {
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
            }
            catch (Exception)
            {
                TryDelete(temporary);
                throw;
            }
        }

        public static bool TryRead(string updateRoot, out PendingUpdatePlan plan, out string error)
        {
            if (!Exists(updateRoot))
            {
                plan = null;
                error = "No pending update was found.";
                return false;
            }
            return TryReadFile(GetPath(updateRoot), out plan, out error);
        }

        public static bool TryReadFile(string path, out PendingUpdatePlan plan, out string error)
        {
            plan = null;
            string text;
            try
            {
                text = File.ReadAllText(path, Encoding.UTF8);
            }
            catch (Exception e)
            {
                error = "The pending update description could not be read: " + e.Message;
                return false;
            }

            try
            {
                JsonCursor cursor = new JsonCursor(text);
                plan = ParsePlan(cursor);
                cursor.EnsureEnd();
                error = string.Empty;
                return true;
            }
            catch (FormatException e)
            {
                plan = null;
                error = "The pending update description is corrupted: " + e.Message;
                return false;
            }
        }

        public static bool Validate(PendingUpdatePlan plan, out string error)
        {
            if (plan == null)
            {
                error = "The pending update description is empty.";
                return false;
            }
            if (plan.Schema != SchemaVersion)
            {
                error = "The pending update uses schema " + plan.Schema + " instead of " + SchemaVersion + ".";
                return false;
            }
            if (string.IsNullOrEmpty(plan.Version))
            {
                error = "The pending update has no version.";
                return false;
            }
            if (!IsKnownTarget(plan.Target))
            {
                error = "The pending update targets an unknown loader: " + (plan.Target ?? "(none)") + ".";
                return false;
            }
            if (string.IsNullOrEmpty(plan.ModPath) || !Path.IsPathRooted(plan.ModPath))
            {
                error = "The pending update has no usable install path: " + (plan.ModPath ?? "(none)") + ".";
                return false;
            }
            if (plan.Files == null || plan.Files.Count == 0)
            {
                error = "The pending update lists no files.";
                return false;
            }

            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (PendingFileEntry entry in plan.Files)
            {
                if (entry == null)
                {
                    error = "The pending update lists an empty file entry.";
                    return false;
                }

                string relative = NormalizeRelative(entry.RelativePath);
                if (relative == null)
                {
                    error = "The pending update lists an unsafe path: " + (entry.RelativePath ?? "(none)") + ".";
                    return false;
                }
                if (entry.Size < 0 || !IsSha256(entry.Sha256))
                {
                    error = "The pending update has no valid size or hash for " + relative + ".";
                    return false;
                }
                if (!seen.Add(relative))
                {
                    error = "The pending update lists " + relative + " twice.";
                    return false;
                }

                entry.RelativePath = relative;
            }

            error = string.Empty;
            return true;
        }

        public static string ComputeHash(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            using (SHA256 sha = SHA256.Create())
            {
                return ToHex(sha.ComputeHash(stream));
            }
        }

        public static void TryDelete(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path)) File.Delete(path);
            }
            catch (Exception)
            {
            }
        }

        private static string Serialize(PendingUpdatePlan plan)
        {
            StringBuilder builder = new StringBuilder(512);
            builder.Append("{\n");
            builder.Append("  \"schema\": ").Append(plan.Schema.ToString(CultureInfo.InvariantCulture)).Append(",\n");
            builder.Append("  \"id\": \"").Append(Escape(plan.Id)).Append("\",\n");
            builder.Append("  \"version\": \"").Append(Escape(plan.Version)).Append("\",\n");
            builder.Append("  \"from\": \"").Append(Escape(plan.FromVersion)).Append("\",\n");
            builder.Append("  \"target\": \"").Append(Escape(plan.Target)).Append("\",\n");
            builder.Append("  \"mod\": \"").Append(Escape(plan.ModPath)).Append("\",\n");
            builder.Append("  \"created\": \"").Append(Escape(plan.Created)).Append("\",\n");
            builder.Append("  \"files\": [\n");
            for (int i = 0; i < plan.Files.Count; i++)
            {
                PendingFileEntry entry = plan.Files[i];
                builder.Append("    { \"path\": \"").Append(Escape(entry.RelativePath)).Append("\", \"size\": ")
                    .Append(entry.Size.ToString(CultureInfo.InvariantCulture))
                    .Append(", \"sha256\": \"").Append(Escape(entry.Sha256)).Append("\" }");
                if (i < plan.Files.Count - 1) builder.Append(',');
                builder.Append('\n');
            }
            builder.Append("  ]\n}\n");
            return builder.ToString();
        }

        private static PendingUpdatePlan ParsePlan(JsonCursor cursor)
        {
            PendingUpdatePlan plan = new PendingUpdatePlan();
            cursor.Expect('{');
            bool first = true;
            while (true)
            {
                if (cursor.TryConsume('}')) break;
                if (!first) cursor.Expect(',');
                first = false;

                string key = cursor.ReadString();
                cursor.Expect(':');
                switch (key)
                {
                    case "schema":
                        plan.Schema = (int)cursor.ReadLong();
                        break;
                    case "id":
                        plan.Id = cursor.ReadString();
                        break;
                    case "version":
                        plan.Version = cursor.ReadString();
                        break;
                    case "from":
                        plan.FromVersion = cursor.ReadString();
                        break;
                    case "target":
                        plan.Target = cursor.ReadString();
                        break;
                    case "mod":
                        plan.ModPath = cursor.ReadString();
                        break;
                    case "created":
                        plan.Created = cursor.ReadString();
                        break;
                    case "files":
                        plan.Files = ParseFiles(cursor);
                        break;
                    default:
                        cursor.SkipValue();
                        break;
                }
            }
            return plan;
        }

        private static List<PendingFileEntry> ParseFiles(JsonCursor cursor)
        {
            List<PendingFileEntry> files = new List<PendingFileEntry>();
            cursor.Expect('[');
            bool first = true;
            while (true)
            {
                if (cursor.TryConsume(']')) break;
                if (!first) cursor.Expect(',');
                first = false;
                files.Add(ParseFile(cursor));
            }
            return files;
        }

        private static PendingFileEntry ParseFile(JsonCursor cursor)
        {
            PendingFileEntry entry = new PendingFileEntry();
            cursor.Expect('{');
            bool first = true;
            while (true)
            {
                if (cursor.TryConsume('}')) break;
                if (!first) cursor.Expect(',');
                first = false;

                string key = cursor.ReadString();
                cursor.Expect(':');
                switch (key)
                {
                    case "path":
                        entry.RelativePath = cursor.ReadString();
                        break;
                    case "size":
                        entry.Size = cursor.ReadLong();
                        break;
                    case "sha256":
                        entry.Sha256 = cursor.ReadString();
                        break;
                    default:
                        cursor.SkipValue();
                        break;
                }
            }
            return entry;
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            StringBuilder builder = new StringBuilder(value.Length);
            foreach (char character in value)
            {
                switch (character)
                {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '\b':
                        builder.Append("\\b");
                        break;
                    case '\f':
                        builder.Append("\\f");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        if (character < ' ')
                            builder.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            builder.Append(character);
                        break;
                }
            }
            return builder.ToString();
        }

        private static string NormalizeRelative(string relative)
        {
            if (string.IsNullOrEmpty(relative)) return null;

            string normalized = relative.Replace('\\', '/').Trim();
            if (normalized.Length == 0 || normalized[0] == '/' || normalized.IndexOf(':') >= 0) return null;

            StringBuilder builder = new StringBuilder(normalized.Length);
            foreach (string segment in normalized.Split('/'))
            {
                if (segment.Length == 0 || segment == "." || segment == "..") return null;
                if (builder.Length > 0) builder.Append('/');
                builder.Append(segment);
            }
            return builder.Length == 0 ? null : builder.ToString();
        }

        private static bool IsKnownTarget(string target)
        {
            return string.Equals(target, UmmTargetValue, StringComparison.OrdinalIgnoreCase)
                || string.Equals(target, MelonTargetValue, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSha256(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 64) return false;

            foreach (char character in value)
            {
                bool digit = character >= '0' && character <= '9';
                bool letter = (character >= 'a' && character <= 'f') || (character >= 'A' && character <= 'F');
                if (!digit && !letter) return false;
            }
            return true;
        }

        private static string ToHex(byte[] bytes)
        {
            StringBuilder builder = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++) builder.Append(bytes[i].ToString("X2"));
            return builder.ToString();
        }

        private sealed class JsonCursor
        {
            private readonly string _text;
            private int _index;

            public JsonCursor(string text)
            {
                _text = text ?? string.Empty;
            }

            public void EnsureEnd()
            {
                SkipWhiteSpace();
                if (_index < _text.Length) throw Error("unexpected trailing content");
            }

            public void Expect(char expected)
            {
                SkipWhiteSpace();
                if (_index >= _text.Length || _text[_index] != expected) throw Error("expected '" + expected + "'");
                _index++;
            }

            public bool TryConsume(char value)
            {
                SkipWhiteSpace();
                if (_index >= _text.Length || _text[_index] != value) return false;
                _index++;
                return true;
            }

            public string ReadString()
            {
                SkipWhiteSpace();
                if (_index >= _text.Length || _text[_index] != '"') throw Error("expected a string");
                _index++;

                StringBuilder builder = new StringBuilder();
                while (true)
                {
                    if (_index >= _text.Length) throw Error("unterminated string");

                    char current = _text[_index++];
                    if (current == '"') return builder.ToString();
                    if (current != '\\')
                    {
                        if (current < ' ') throw Error("control character in a string");
                        builder.Append(current);
                        continue;
                    }

                    if (_index >= _text.Length) throw Error("unterminated escape sequence");
                    char escape = _text[_index++];
                    switch (escape)
                    {
                        case '"':
                            builder.Append('"');
                            break;
                        case '\\':
                            builder.Append('\\');
                            break;
                        case '/':
                            builder.Append('/');
                            break;
                        case 'b':
                            builder.Append('\b');
                            break;
                        case 'f':
                            builder.Append('\f');
                            break;
                        case 'n':
                            builder.Append('\n');
                            break;
                        case 'r':
                            builder.Append('\r');
                            break;
                        case 't':
                            builder.Append('\t');
                            break;
                        case 'u':
                            if (_index + 4 > _text.Length) throw Error("truncated unicode escape");
                            int code = int.Parse(_text.Substring(_index, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                            _index += 4;
                            builder.Append((char)code);
                            break;
                        default:
                            throw Error("invalid escape sequence");
                    }
                }
            }

            public long ReadLong()
            {
                SkipWhiteSpace();
                int start = _index;
                if (_index < _text.Length && _text[_index] == '-') _index++;

                int digits = 0;
                while (_index < _text.Length && _text[_index] >= '0' && _text[_index] <= '9')
                {
                    _index++;
                    digits++;
                }
                if (digits == 0) throw Error("expected a number");

                long value;
                if (!long.TryParse(_text.Substring(start, _index - start), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                    throw Error("number is out of range");
                return value;
            }

            public void SkipValue()
            {
                SkipWhiteSpace();
                if (_index >= _text.Length) throw Error("unexpected end");

                char current = _text[_index];
                if (current == '"')
                {
                    ReadString();
                    return;
                }
                if (current == '{')
                {
                    _index++;
                    bool first = true;
                    while (true)
                    {
                        if (TryConsume('}')) return;
                        if (!first) Expect(',');
                        first = false;
                        ReadString();
                        Expect(':');
                        SkipValue();
                    }
                }
                if (current == '[')
                {
                    _index++;
                    bool first = true;
                    while (true)
                    {
                        if (TryConsume(']')) return;
                        if (!first) Expect(',');
                        first = false;
                        SkipValue();
                    }
                }
                if (current == 't' || current == 'f' || current == 'n')
                {
                    SkipLiteral();
                    return;
                }

                ReadLong();
            }

            private void SkipLiteral()
            {
                foreach (string literal in Literals)
                {
                    if (_index + literal.Length > _text.Length) continue;
                    if (string.CompareOrdinal(_text, _index, literal, 0, literal.Length) != 0) continue;
                    _index += literal.Length;
                    return;
                }
                throw Error("invalid literal");
            }

            public FormatException Error(string message)
            {
                return new FormatException(message + " at offset " + _index + ".");
            }

            private void SkipWhiteSpace()
            {
                while (_index < _text.Length && char.IsWhiteSpace(_text[_index])) _index++;
            }

            private static readonly string[] Literals = { "true", "false", "null" };
        }
    }
}
