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
                plan = ParsePlan(JsonText.ParseObject(text));
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

        private static PendingUpdatePlan ParsePlan(Dictionary<string, object> root)
        {
            PendingUpdatePlan plan = new PendingUpdatePlan();
            plan.Schema = (int)ReadLong(root, "schema");
            plan.Id = ReadString(root, "id");
            plan.Version = ReadString(root, "version");
            plan.FromVersion = ReadString(root, "from");
            plan.Target = ReadString(root, "target");
            plan.ModPath = ReadString(root, "mod");
            plan.Created = ReadString(root, "created");
            plan.Files = ParseFiles(root);
            return plan;
        }

        private static List<PendingFileEntry> ParseFiles(Dictionary<string, object> root)
        {
            object value;
            if (!root.TryGetValue("files", out value) || value == null) return null;
            List<object> items = value as List<object>;
            if (items == null) throw new FormatException("'files' is not an array.");

            List<PendingFileEntry> files = new List<PendingFileEntry>(items.Count);
            foreach (object item in items)
            {
                Dictionary<string, object> values = item as Dictionary<string, object>;
                if (values == null) throw new FormatException("'files' contains a value that is not an object.");
                files.Add(ParseFile(values));
            }
            return files;
        }

        private static PendingFileEntry ParseFile(Dictionary<string, object> values)
        {
            PendingFileEntry entry = new PendingFileEntry();
            entry.RelativePath = ReadString(values, "path");
            entry.Size = ReadLong(values, "size");
            entry.Sha256 = ReadString(values, "sha256");
            return entry;
        }

        private static string ReadString(Dictionary<string, object> values, string key)
        {
            object value;
            if (!values.TryGetValue(key, out value) || value == null) return null;
            string text = value as string;
            if (text == null) throw new FormatException("'" + key + "' is not a string.");
            return text;
        }

        private static long ReadLong(Dictionary<string, object> values, string key)
        {
            object value;
            if (!values.TryGetValue(key, out value) || value == null) return 0L;
            if (value is long) return (long)value;
            throw new FormatException("'" + key + "' is not a number.");
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
    }
}
