using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TimingShow.Update
{
    internal static class PendingUpdateApplier
    {
        public const string UpdateDirectoryName = "Update";
        public const string StagingDirectoryName = "staging";
        public const string DownloadDirectoryName = "download";
        public const string BackupDirectoryPrefix = "backup-";

        private const string LegacyPendingFileName = "pending.txt";
        private const string LegacyManifestFileName = "manifest.txt";
        private const string MelonTargetValue = "Melon";
        private const string MelonLoaderFileName = "TimingShow.Loader.Melon.dll";

        public static bool HasPending(string updateRoot)
        {
            return PendingUpdateFile.Exists(updateRoot);
        }

        public static bool Apply(string updateRoot, string fallbackModPath, Action<string> log, Action<string> error)
        {
            if (log == null) log = delegate { };
            if (error == null) error = delegate { };
            if (string.IsNullOrEmpty(updateRoot)) return false;

            try
            {
                DiscardLegacy(updateRoot, log);
                if (!PendingUpdateFile.Exists(updateRoot)) return false;

                PendingUpdatePlan plan;
                string message;
                if (!PendingUpdateFile.TryRead(updateRoot, out plan, out message))
                {
                    DiscardPending(updateRoot, error, message);
                    return false;
                }
                if (plan.Schema > PendingUpdateFile.SchemaVersion)
                {
                    log("A pending update was written by a newer version of the updater; it was left in place.");
                    return false;
                }
                if (!PendingUpdateFile.Validate(plan, out message))
                {
                    DiscardPending(updateRoot, error, message);
                    return false;
                }

                string version = plan.Version;
                string stagingDirectory = NormalizeDirectory(Path.Combine(updateRoot, StagingDirectoryName));
                string backupDirectory = Path.Combine(updateRoot, BackupDirectoryPrefix + Sanitize(plan.FromVersion));
                bool melonTarget = string.Equals(plan.Target, MelonTargetValue, StringComparison.OrdinalIgnoreCase);

                string modPath = ResolveModPath(plan, fallbackModPath, log);
                if (!IsValidModPath(modPath, melonTarget))
                {
                    RestoreBackup(backupDirectory, modPath, error);
                    DiscardPending(updateRoot, error, "Pending update to " + version + " points at an unusable install path " + (modPath ?? "(none)") + "; it was discarded.");
                    return false;
                }

                string stagingPrefix = stagingDirectory + Path.DirectorySeparatorChar;
                string modPrefix = modPath + Path.DirectorySeparatorChar;

                bool failed = false;
                List<PendingFileEntry> verified = new List<PendingFileEntry>(plan.Files.Count);
                foreach (PendingFileEntry entry in plan.Files)
                {
                    string source = Path.GetFullPath(Path.Combine(stagingDirectory, entry.RelativePath));
                    string target = Path.GetFullPath(Path.Combine(modPath, entry.RelativePath));
                    if (!source.StartsWith(stagingPrefix, StringComparison.OrdinalIgnoreCase)
                        || !target.StartsWith(modPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        error("Pending update entry escapes the install directory: " + entry.RelativePath + " (install " + modPath + ", staging " + stagingDirectory + ")");
                        failed = true;
                        continue;
                    }

                    FileInfo staged = new FileInfo(source);
                    if (!staged.Exists || staged.Length != entry.Size
                        || !string.Equals(ComputeHash(source), entry.Sha256, StringComparison.OrdinalIgnoreCase))
                    {
                        error("Pending update file is corrupted: " + entry.RelativePath);
                        failed = true;
                        continue;
                    }

                    verified.Add(entry);
                }

                if (failed)
                {
                    RestoreBackup(backupDirectory, modPath, error);
                    DiscardPending(updateRoot, error, "Pending update to " + version + " is corrupted; the previous files were restored.");
                    return false;
                }

                List<string> replaced = new List<string>();
                foreach (PendingFileEntry entry in verified)
                {
                    try
                    {
                        string target = Path.Combine(modPath, entry.RelativePath);
                        BackupFile(target, backupDirectory, entry.RelativePath);
                        InstallFile(Path.Combine(stagingDirectory, entry.RelativePath), target, entry);
                        replaced.Add(entry.RelativePath);
                    }
                    catch (Exception e)
                    {
                        error("Failed to replace " + entry.RelativePath + ": " + e.Message);
                        RestoreFiles(replaced, backupDirectory, modPath, error);
                        log("Pending update to " + version + " was rolled back; it will be retried on the next launch.");
                        return false;
                    }
                }

                PendingUpdateFile.TryDelete(PendingUpdateFile.GetPath(updateRoot));
                DeleteDirectory(stagingDirectory);
                DeleteDirectory(Path.Combine(updateRoot, DownloadDirectoryName));
                PruneBackups(updateRoot, backupDirectory);
                log("Applied pending update " + version + " (" + verified.Count + " file(s)).");
                return true;
            }
            catch (Exception e)
            {
                error("Pending update failed: " + e.Message);
                return false;
            }
        }

        private static string ComputeHash(string path)
        {
            return PendingUpdateFile.ComputeHash(path);
        }

        private static void DiscardLegacy(string updateRoot, Action<string> log)
        {
            string legacyPending = Path.Combine(updateRoot, LegacyPendingFileName);
            string legacyManifest = Path.Combine(updateRoot, StagingDirectoryName, LegacyManifestFileName);
            if (!File.Exists(legacyPending) && !File.Exists(legacyManifest)) return;

            PendingUpdateFile.TryDelete(legacyPending);
            PendingUpdateFile.TryDelete(legacyManifest);
            DeleteDirectory(Path.Combine(updateRoot, StagingDirectoryName));
            DeleteDirectory(Path.Combine(updateRoot, DownloadDirectoryName));
            log("Discarded a pending update left behind by an older version of the updater; it will be downloaded again if it is still available.");
        }

        private static void DiscardPending(string updateRoot, Action<string> error, string message)
        {
            error(message);
            PendingUpdateFile.TryDelete(PendingUpdateFile.GetPath(updateRoot));
            DeleteDirectory(Path.Combine(updateRoot, StagingDirectoryName));
            DeleteDirectory(Path.Combine(updateRoot, DownloadDirectoryName));
        }

        private static string ResolveModPath(PendingUpdatePlan plan, string fallbackModPath, Action<string> log)
        {
            string candidate = string.IsNullOrEmpty(fallbackModPath) ? plan.ModPath : fallbackModPath;
            if (string.IsNullOrEmpty(candidate)) return null;

            string resolved = NormalizeDirectory(candidate);
            if (!string.IsNullOrEmpty(plan.ModPath) && !string.Equals(resolved, NormalizeDirectory(plan.ModPath), StringComparison.OrdinalIgnoreCase))
                log("The pending update was prepared for " + plan.ModPath + " but is applied to " + resolved + ".");
            return resolved;
        }

        internal static string NormalizeDirectory(string path)
        {
            string full = Path.GetFullPath(path);
            string root = Path.GetPathRoot(full);
            return full.Length > root.Length ? full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) : full;
        }

        private static bool IsValidModPath(string modPath, bool melonTarget)
        {
            if (string.IsNullOrEmpty(modPath) || !Directory.Exists(modPath)) return false;
            return !melonTarget || File.Exists(Path.Combine(modPath, MelonLoaderFileName));
        }

        private static void InstallFile(string source, string target, PendingFileEntry entry)
        {
            string directory = Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            string incoming = target + ".incoming";
            try
            {
                File.Copy(source, incoming, true);
                FileInfo copied = new FileInfo(incoming);
                if (copied.Length != entry.Size || !string.Equals(ComputeHash(incoming), entry.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("the copy prepared for " + entry.RelativePath + " did not match its hash");

                if (File.Exists(target)) File.Replace(incoming, target, null);
                else File.Move(incoming, target);
            }
            finally
            {
                PendingUpdateFile.TryDelete(incoming);
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
