using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TimingShow.Update
{
    internal static class PendingUpdateApplier
    {
        public const string UpdateDirectoryName = "Update";
        public const string StagingDirectoryName = "staging";
        public const string PendingFileName = "pending.txt";
        public const string ManifestFileName = "manifest.txt";
        public const string DownloadDirectoryName = "download";
        public const string BackupDirectoryPrefix = "backup-";

        private const string MelonTargetValue = "Melon";
        private const string MelonLoaderFileName = "TimingShow.Loader.Melon.dll";

        public static bool Apply(string updateRoot, string fallbackModPath, Action<string> log, Action<string> error)
        {
            if (log == null) log = delegate { };
            if (error == null) error = delegate { };
            if (string.IsNullOrEmpty(updateRoot)) return false;

            try
            {
                string pendingPath = Path.Combine(updateRoot, PendingFileName);
                if (!File.Exists(pendingPath)) return false;

                string version = ReadValue(pendingPath, "version") ?? "unknown";
                string backupDirectory = Path.Combine(updateRoot, BackupDirectoryPrefix + Sanitize(ReadValue(pendingPath, "from")));
                string stagingDirectory = Path.GetFullPath(Path.Combine(updateRoot, StagingDirectoryName));
                bool melonTarget = string.Equals(ReadValue(pendingPath, "target"), MelonTargetValue, StringComparison.OrdinalIgnoreCase);

                string modPath = ResolveModPath(pendingPath, fallbackModPath);
                if (!IsValidModPath(modPath, melonTarget))
                {
                    RestoreBackup(backupDirectory, modPath, error);
                    File.Delete(pendingPath);
                    DeleteDirectory(stagingDirectory);
                    error("Pending update to " + version + " points at an unusable install path " + (modPath ?? "(none)") + "; it was discarded.");
                    return false;
                }

                string manifestPath = Path.Combine(stagingDirectory, ManifestFileName);
                if (!File.Exists(manifestPath))
                {
                    RestoreBackup(backupDirectory, modPath, error);
                    File.Delete(pendingPath);
                    DeleteDirectory(stagingDirectory);
                    log("Pending update to " + version + " has no manifest; the previous files were restored.");
                    return false;
                }

                string stagingPrefix = stagingDirectory + Path.DirectorySeparatorChar;
                string modPrefix = modPath + Path.DirectorySeparatorChar;

                bool failed = false;
                List<string> entries = new List<string>();
                foreach (string line in File.ReadAllLines(manifestPath))
                {
                    int tab = line.IndexOf('\t');
                    if (tab <= 0) continue;

                    string expectedHash = line.Substring(0, tab).Trim();
                    string relative = line.Substring(tab + 1).Trim().Replace('\\', '/').TrimStart('/', '\\');
                    if (relative.Length == 0) continue;

                    string source = Path.GetFullPath(Path.Combine(stagingDirectory, relative));
                    string target = Path.GetFullPath(Path.Combine(modPath, relative));
                    if (!source.StartsWith(stagingPrefix, StringComparison.OrdinalIgnoreCase)
                        || !target.StartsWith(modPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        error("Pending update entry escapes the install directory: " + relative);
                        failed = true;
                        continue;
                    }
                    if (!File.Exists(source) || !string.Equals(HashFile(source), expectedHash, StringComparison.OrdinalIgnoreCase))
                    {
                        error("Pending update file is corrupted: " + relative);
                        failed = true;
                        continue;
                    }

                    entries.Add(relative);
                }

                if (failed)
                {
                    RestoreBackup(backupDirectory, modPath, error);
                    File.Delete(pendingPath);
                    DeleteDirectory(stagingDirectory);
                    log("Pending update to " + version + " is corrupted; the previous files were restored.");
                    return false;
                }

                List<string> replaced = new List<string>();
                foreach (string relative in entries)
                {
                    try
                    {
                        string target = Path.Combine(modPath, relative);
                        BackupFile(target, backupDirectory, relative);
                        ReplaceFile(Path.Combine(stagingDirectory, relative), target);
                        replaced.Add(relative);
                    }
                    catch (Exception e)
                    {
                        error("Failed to replace " + relative + ": " + e.Message);
                        RestoreFiles(replaced, backupDirectory, modPath, error);
                        log("Pending update to " + version + " was rolled back; it will be retried on the next launch.");
                        return false;
                    }
                }

                File.Delete(pendingPath);
                DeleteDirectory(stagingDirectory);
                DeleteDirectory(Path.Combine(updateRoot, DownloadDirectoryName));
                PruneBackups(updateRoot, backupDirectory);
                log("Applied pending update " + version + " (" + entries.Count + " file(s)).");
                return true;
            }
            catch (Exception e)
            {
                error("Pending update failed: " + e.Message);
                return false;
            }
        }

        private static string ResolveModPath(string pendingPath, string fallbackModPath)
        {
            string stored = ReadValue(pendingPath, "path");
            string candidate = string.IsNullOrEmpty(stored) ? fallbackModPath : stored;
            return string.IsNullOrEmpty(candidate) ? null : Path.GetFullPath(candidate);
        }

        private static bool IsValidModPath(string modPath, bool melonTarget)
        {
            if (string.IsNullOrEmpty(modPath) || !Directory.Exists(modPath)) return false;
            return !melonTarget || File.Exists(Path.Combine(modPath, MelonLoaderFileName));
        }

        private static void ReplaceFile(string source, string target)
        {
            string directory = Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            string incoming = target + ".incoming";
            File.Copy(source, incoming, true);
            if (File.Exists(target))
            {
                File.Replace(incoming, target, null);
            }
            else
            {
                File.Move(incoming, target);
            }
        }

        private static void RestoreFiles(List<string> relativePaths, string backupDirectory, string modPath, Action<string> error)
        {
            foreach (string relative in relativePaths)
            {
                string backup = Path.Combine(backupDirectory, relative);
                string target = Path.Combine(modPath, relative);
                try
                {
                    if (File.Exists(backup))
                    {
                        File.Copy(backup, target, true);
                    }
                    else if (File.Exists(target))
                    {
                        File.Delete(target);
                    }
                }
                catch (Exception e)
                {
                    error("Failed to restore " + relative + ": " + e.Message);
                }
            }
        }

        private static void RestoreBackup(string backupDirectory, string modPath, Action<string> error)
        {
            if (string.IsNullOrEmpty(modPath) || !Directory.Exists(backupDirectory)) return;

            string prefix = Path.GetFullPath(backupDirectory) + Path.DirectorySeparatorChar;
            foreach (string file in Directory.GetFiles(backupDirectory, "*", SearchOption.AllDirectories))
            {
                string relative = file.Substring(prefix.Length);
                string target = Path.Combine(modPath, relative);
                try
                {
                    string directory = Path.GetDirectoryName(target);
                    if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                    File.Copy(file, target, true);
                }
                catch (Exception e)
                {
                    error("Failed to restore " + relative + ": " + e.Message);
                }
            }
        }

        private static void BackupFile(string target, string backupDirectory, string relative)
        {
            if (!File.Exists(target)) return;

            string backup = Path.Combine(backupDirectory, relative);
            string directory = Path.GetDirectoryName(backup);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            if (!File.Exists(backup)) File.Copy(target, backup, true);
        }

        private static void PruneBackups(string updateRoot, string keep)
        {
            try
            {
                foreach (string directory in Directory.GetDirectories(updateRoot, BackupDirectoryPrefix + "*"))
                {
                    if (string.Equals(directory, keep, StringComparison.OrdinalIgnoreCase)) continue;
                    Directory.Delete(directory, true);
                }
            }
            catch (Exception)
            {
            }
        }

        private static void DeleteDirectory(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && Directory.Exists(path)) Directory.Delete(path, true);
            }
            catch (Exception)
            {
            }
        }

        private static string ReadValue(string path, string key)
        {
            foreach (string line in File.ReadAllLines(path))
            {
                int separator = line.IndexOf('=');
                if (separator <= 0) continue;
                if (string.Equals(line.Substring(0, separator).Trim(), key, StringComparison.OrdinalIgnoreCase))
                    return line.Substring(separator + 1).Trim();
            }
            return null;
        }

        private static string HashFile(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(stream);
                StringBuilder builder = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++) builder.Append(hash[i].ToString("X2"));
                return builder.ToString();
            }
        }

        private static string Sanitize(string text)
        {
            if (string.IsNullOrEmpty(text)) return "update";

            StringBuilder builder = new StringBuilder(text.Length);
            foreach (char character in text)
            {
                if (char.IsLetterOrDigit(character) || character == '.' || character == '-' || character == '_')
                    builder.Append(character);
            }
            return builder.Length == 0 ? "update" : builder.ToString();
        }
    }
}
