using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;

namespace TimingShow.Update
{
    internal static class UpdatePackage
    {
        public const string UpdateDirectoryName = "Update";
        public const string StagingDirectoryName = "staging";
        public const string PendingFileName = "pending.txt";
        public const string ManifestFileName = "manifest.txt";
        public const string DownloadDirectoryName = "download";
        public const string BackupDirectoryPrefix = "backup-";

        private const string CoreAssemblyName = "TimingShow.dll";
        private const string LoaderAssemblyName = "TimingShow.Loader.UMM.dll";

        private static readonly string[] RequiredFiles = { CoreAssemblyName, LoaderAssemblyName, "Info.json", "lang.json" };

        public static void Extract(string packagePath, string stagingDirectory)
        {
            string root = Path.GetFullPath(stagingDirectory);
            string prefix = root + Path.DirectorySeparatorChar;
            if (Directory.Exists(root)) Directory.Delete(root, true);
            Directory.CreateDirectory(root);

            using (FileStream stream = File.OpenRead(packagePath))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string relative = entry.FullName.Replace('\\', '/');
                    if (relative.Length == 0 || relative.EndsWith("/", StringComparison.Ordinal)) continue;

                    string target = Path.GetFullPath(Path.Combine(root, relative));
                    if (!target.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Package entry escapes the staging directory: " + relative);

                    string parent = Path.GetDirectoryName(target);
                    if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
                    using (Stream input = entry.Open())
                    using (FileStream output = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        input.CopyTo(output);
                    }
                }
            }
        }

        public static void Verify(string stagingDirectory, string modPath, UpdateRelease release)
        {
            foreach (string required in RequiredFiles)
            {
                string path = Path.Combine(stagingDirectory, required);
                if (!File.Exists(path) || new FileInfo(path).Length == 0)
                    throw new InvalidDataException("Update package is missing " + required + ".");
            }

            JObject packaged = JObject.Parse(File.ReadAllText(Path.Combine(stagingDirectory, "Info.json"), Encoding.UTF8));
            JObject installed = JObject.Parse(File.ReadAllText(Path.Combine(modPath, "Info.json"), Encoding.UTF8));

            if (!VersionsMatch((string)packaged["Version"], release.Version))
                throw new InvalidDataException("Package version " + (string)packaged["Version"] + " does not match release " + release.Version + ".");
            if ((string)packaged["Id"] != (string)installed["Id"])
                throw new InvalidDataException("Package belongs to a different mod: " + (string)packaged["Id"] + ".");
            if ((string)packaged["EntryMethod"] != (string)installed["EntryMethod"])
                throw new InvalidDataException("Package entry method does not match the installed mod.");
            if ((string)packaged["AssemblyName"] != (string)installed["AssemblyName"])
                throw new InvalidDataException("Package loader assembly does not match the installed mod.");

            AssertAssembly(Path.Combine(stagingDirectory, CoreAssemblyName), null);
            AssertAssembly(Path.Combine(stagingDirectory, LoaderAssemblyName), (string)packaged["AssemblyName"]);

            JObject.Parse(File.ReadAllText(Path.Combine(stagingDirectory, "lang.json"), Encoding.UTF8));
        }

        public static void WriteManifest(string stagingDirectory)
        {
            string root = Path.GetFullPath(stagingDirectory);
            StringBuilder builder = new StringBuilder();
            foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            {
                string relative = file.Substring(root.Length).TrimStart('\\', '/').Replace('\\', '/');
                if (string.Equals(relative, ManifestFileName, StringComparison.OrdinalIgnoreCase)) continue;
                builder.Append(HashFile(file)).Append('\t').Append(relative).Append('\n');
            }
            File.WriteAllText(Path.Combine(root, ManifestFileName), builder.ToString(), new UTF8Encoding(false));
        }

        public static void WritePending(string modPath, string version, string fromVersion)
        {
            string updateDirectory = Path.Combine(modPath, UpdateDirectoryName);
            Directory.CreateDirectory(updateDirectory);
            string pendingPath = Path.Combine(updateDirectory, PendingFileName);
            string tempPath = pendingPath + ".tmp";
            File.WriteAllText(tempPath, "version=" + version + "\nfrom=" + fromVersion + "\n", new UTF8Encoding(false));
            if (File.Exists(pendingPath)) File.Delete(pendingPath);
            File.Move(tempPath, pendingPath);
        }

        public static void ApplyStaged(string modPath, string stagingDirectory, string fromVersion)
        {
            string staging = Path.GetFullPath(stagingDirectory);
            string stagingPrefix = staging + Path.DirectorySeparatorChar;
            string updateDirectory = Path.Combine(modPath, UpdateDirectoryName);
            string pendingPath = Path.Combine(updateDirectory, PendingFileName);
            string backupDirectory = Path.Combine(updateDirectory, BackupDirectoryPrefix + SanitizeVersion(fromVersion));

            List<string> entries = new List<string>();
            foreach (string line in File.ReadAllLines(Path.Combine(staging, ManifestFileName)))
            {
                int tab = line.IndexOf('\t');
                if (tab <= 0) continue;

                string expectedHash = line.Substring(0, tab).Trim();
                string relative = line.Substring(tab + 1).Trim().Replace('\\', '/');
                if (relative.Length == 0) continue;

                string source = Path.GetFullPath(Path.Combine(staging, relative));
                if (!source.StartsWith(stagingPrefix, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Update entry escapes the staging directory: " + relative);
                if (!File.Exists(source) || !string.Equals(HashFile(source), expectedHash, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Update file is corrupted: " + relative);

                entries.Add(relative);
            }

            List<string> replaced = new List<string>();
            try
            {
                foreach (string relative in entries)
                {
                    string target = Path.Combine(modPath, relative);
                    BackupFile(target, backupDirectory, relative);
                    ReplaceFile(Path.Combine(staging, relative), target);
                    replaced.Add(relative);
                }
            }
            catch
            {
                RestoreFiles(replaced, backupDirectory, modPath);
                throw;
            }

            if (TryDeleteFile(pendingPath))
            {
                TryDeleteDirectory(staging);
                TryDeleteDirectory(Path.Combine(updateDirectory, DownloadDirectoryName));
            }
            PruneBackups(updateDirectory, backupDirectory);
        }

        public static string HashFile(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            using (SHA256 sha = SHA256.Create())
            {
                return UpdateClient.ToHex(sha.ComputeHash(stream));
            }
        }

        public static string SanitizeVersion(string version)
        {
            if (string.IsNullOrEmpty(version)) return "update";
            StringBuilder builder = new StringBuilder(version.Length);
            foreach (char character in version)
            {
                if (char.IsLetterOrDigit(character) || character == '.' || character == '-' || character == '_')
                    builder.Append(character);
            }
            return builder.Length == 0 ? "update" : builder.ToString();
        }

        public static bool TryDeleteFile(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path)) File.Delete(path);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path)) Directory.Delete(path, true);
                return true;
            }
            catch (Exception)
            {
                return false;
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

        private static void BackupFile(string target, string backupDirectory, string relative)
        {
            if (!File.Exists(target)) return;

            string backup = Path.Combine(backupDirectory, relative);
            string directory = Path.GetDirectoryName(backup);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            if (!File.Exists(backup)) File.Copy(target, backup, true);
        }

        private static void RestoreFiles(List<string> relativePaths, string backupDirectory, string modPath)
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
                    ModContext.Logger?.Error("Failed to restore " + relative + ": " + e.Message);
                }
            }
        }

        private static void PruneBackups(string updateDirectory, string keep)
        {
            try
            {
                foreach (string directory in Directory.GetDirectories(updateDirectory, BackupDirectoryPrefix + "*"))
                {
                    if (string.Equals(directory, keep, StringComparison.OrdinalIgnoreCase)) continue;
                    Directory.Delete(directory, true);
                }
            }
            catch (Exception)
            {
            }
        }

        private static bool VersionsMatch(string left, string right)
        {
            Version leftVersion, rightVersion;
            if (UpdateClient.TryParseVersion(left, out leftVersion) && UpdateClient.TryParseVersion(right, out rightVersion))
                return leftVersion == rightVersion;
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private static void AssertAssembly(string path, string expectedName)
        {
            AssemblyName name = AssemblyName.GetAssemblyName(path);
            string expected = string.IsNullOrEmpty(expectedName) ? Path.GetFileNameWithoutExtension(path) : Path.GetFileNameWithoutExtension(expectedName);
            if (!string.Equals(name.Name, expected, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Unexpected assembly " + name.Name + " in " + Path.GetFileName(path) + ".");
        }
    }
}
