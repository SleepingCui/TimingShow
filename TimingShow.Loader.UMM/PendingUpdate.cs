using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityModManagerNet;

namespace TimingShow.UMM
{
    internal static class PendingUpdate
    {
        private const string UpdateDirectoryName = "Update";
        private const string StagingDirectoryName = "staging";
        private const string PendingFileName = "pending.txt";
        private const string ManifestFileName = "manifest.txt";

        public static void Apply(UnityModManager.ModEntry modEntry)
        {
            if (modEntry == null) return;

            UnityModManager.ModEntry.ModLogger logger = modEntry.Logger;
            string modPath = modEntry.Path;
            try
            {
                string updateDirectory = Path.Combine(modPath, UpdateDirectoryName);
                string pendingPath = Path.Combine(updateDirectory, PendingFileName);
                if (!File.Exists(pendingPath)) return;

                string version = ReadValue(pendingPath, "version") ?? "unknown";
                string backupDirectory = Path.Combine(updateDirectory, "backup-" + Sanitize(ReadValue(pendingPath, "from")));
                string stagingDirectory = Path.GetFullPath(Path.Combine(updateDirectory, StagingDirectoryName));
                string manifestPath = Path.Combine(stagingDirectory, ManifestFileName);
                if (!File.Exists(manifestPath))
                {
                    RestoreBackup(backupDirectory, modPath, logger);
                    File.Delete(pendingPath);
                    DeleteDirectory(stagingDirectory);
                    logger.Log("Pending update to " + version + " has no manifest; the previous files were restored.");
                    return;
                }

                string stagingPrefix = stagingDirectory + Path.DirectorySeparatorChar;

                bool failed = false;
                List<string> entries = new List<string>();
                foreach (string line in File.ReadAllLines(manifestPath))
                {
                    int tab = line.IndexOf('\t');
                    if (tab <= 0) continue;

                    string expectedHash = line.Substring(0, tab).Trim();
                    string relative = line.Substring(tab + 1).Trim();
                    if (relative.Length == 0) continue;

                    string source = Path.GetFullPath(Path.Combine(stagingDirectory, relative));
                    if (!source.StartsWith(stagingPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        logger.Error("Pending update entry escapes the staging directory: " + relative);
                        failed = true;
                        continue;
                    }
                    if (!File.Exists(source) || !string.Equals(HashFile(source), expectedHash, StringComparison.OrdinalIgnoreCase))
                    {
                        logger.Error("Pending update file is corrupted: " + relative);
                        failed = true;
                        continue;
                    }

                    entries.Add(relative);
                }

                if (failed)
                {
                    RestoreBackup(backupDirectory, modPath, logger);
                    File.Delete(pendingPath);
                    DeleteDirectory(stagingDirectory);
                    logger.Log("Pending update to " + version + " is corrupted; the previous files were restored.");
                    return;
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
                        logger.Error("Failed to replace " + relative + ": " + e.Message);
                        RestoreFiles(replaced, backupDirectory, modPath, logger);
                        logger.Log("Pending update to " + version + " was rolled back; it will be retried on the next launch.");
                        return;
                    }
                }

                File.Delete(pendingPath);
                DeleteDirectory(stagingDirectory);
                DeleteDirectory(Path.Combine(updateDirectory, "download"));
                PruneBackups(updateDirectory, backupDirectory);
                logger.Log("Applied pending update " + version + " (" + entries.Count + " file(s)).");
            }
            catch (Exception e)
            {
                logger.Error("Pending update failed: " + e.Message);
            }
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

        private static void RestoreFiles(List<string> relativePaths, string backupDirectory, string modPath, UnityModManager.ModEntry.ModLogger logger)
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
                    logger.Error("Failed to restore " + relative + ": " + e.Message);
                }
            }
        }

        private static void RestoreBackup(string backupDirectory, string modPath, UnityModManager.ModEntry.ModLogger logger)
        {
            if (!Directory.Exists(backupDirectory)) return;

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
                    logger.Error("Failed to restore " + relative + ": " + e.Message);
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

        private static void PruneBackups(string updateDirectory, string keep)
        {
            try
            {
                foreach (string directory in Directory.GetDirectories(updateDirectory, "backup-*"))
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
                if (Directory.Exists(path)) Directory.Delete(path, true);
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
