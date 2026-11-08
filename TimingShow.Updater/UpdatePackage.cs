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
        private const string CoreAssemblyName = "TimingShow.dll";
        private const string UmmLoaderAssemblyName = "TimingShow.Loader.UMM.dll";
        private const string MelonLoaderAssemblyName = "TimingShow.Loader.Melon.dll";
        private const string BootstrapAssemblyName = "TimingShow.UpdateBootstrap.dll";
        private const string InfoFileName = "Info.json";
        private const string LanguageFileName = "lang.json";
        private const string MelonPackageDirectory = "Mods/";

        private static readonly string[] UmmRequiredFiles = { CoreAssemblyName, UmmLoaderAssemblyName, InfoFileName, LanguageFileName };
        private static readonly string[] MelonRequiredFiles = { CoreAssemblyName, MelonLoaderAssemblyName, LanguageFileName };

        private static readonly string[] UmmExcludedFiles = { MelonLoaderAssemblyName, BootstrapAssemblyName };
        private static readonly string[] MelonExcludedFiles = { UmmLoaderAssemblyName, BootstrapAssemblyName, InfoFileName };

        public static string[] GetRequiredFiles(UpdateTarget target)
        {
            return target == UpdateTarget.Melon ? MelonRequiredFiles : UmmRequiredFiles;
        }

        public static bool IsApplicableFile(string relative, UpdateTarget target)
        {
            if (string.IsNullOrEmpty(relative)) return false;

            string name = Path.GetFileName(relative.Replace('/', Path.DirectorySeparatorChar));
            string[] excluded = target == UpdateTarget.Melon ? MelonExcludedFiles : UmmExcludedFiles;
            foreach (string candidate in excluded)
            {
                if (string.Equals(name, candidate, StringComparison.OrdinalIgnoreCase)) return false;
            }
            return true;
        }

        public static void Extract(string packagePath, string stagingDirectory, UpdateTarget target)
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

                    if (target == UpdateTarget.Melon)
                    {
                        if (!relative.StartsWith(MelonPackageDirectory, StringComparison.OrdinalIgnoreCase))
                            continue;
                        relative = relative.Substring(MelonPackageDirectory.Length);
                    }

                    string destination = Path.GetFullPath(Path.Combine(root, relative));
                    if (!destination.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Package entry escapes the staging directory: " + relative);

                    string parent = Path.GetDirectoryName(destination);
                    if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
                    using (Stream input = entry.Open())
                    using (FileStream output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        input.CopyTo(output);
                    }
                }
            }
        }

        public static void Verify(string stagingDirectory, string modPath, UpdateRelease release, UpdateTarget target)
        {
            foreach (string required in GetRequiredFiles(target))
            {
                string path = Path.Combine(stagingDirectory, required);
                if (!File.Exists(path) || new FileInfo(path).Length == 0)
                    throw new InvalidDataException("Update package is missing " + required + ".");
            }

            AssertAssembly(Path.Combine(stagingDirectory, CoreAssemblyName), null);
            JObject.Parse(File.ReadAllText(Path.Combine(stagingDirectory, LanguageFileName), Encoding.UTF8));

            if (target == UpdateTarget.Melon)
            {
                AssertAssembly(Path.Combine(stagingDirectory, MelonLoaderAssemblyName), null);
                return;
            }

            JObject packaged = JObject.Parse(File.ReadAllText(Path.Combine(stagingDirectory, InfoFileName), Encoding.UTF8));
            JObject installed = JObject.Parse(File.ReadAllText(Path.Combine(modPath, InfoFileName), Encoding.UTF8));

            if (!VersionsMatch((string)packaged["Version"], release.Version))
                throw new InvalidDataException("Package version " + (string)packaged["Version"] + " does not match release " + release.Version + ".");
            if ((string)packaged["Id"] != (string)installed["Id"])
                throw new InvalidDataException("Package belongs to a different mod: " + (string)packaged["Id"] + ".");
            if ((string)packaged["EntryMethod"] != (string)installed["EntryMethod"])
                throw new InvalidDataException("Package entry method does not match the installed mod.");
            if ((string)packaged["AssemblyName"] != (string)installed["AssemblyName"])
                throw new InvalidDataException("Package loader assembly does not match the installed mod.");

            AssertAssembly(Path.Combine(stagingDirectory, UmmLoaderAssemblyName), (string)packaged["AssemblyName"]);
        }

        public static void WriteManifest(string stagingDirectory, UpdateTarget target)
        {
            string root = Path.GetFullPath(stagingDirectory);
            StringBuilder builder = new StringBuilder();
            foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            {
                string relative = file.Substring(root.Length).TrimStart('\\', '/').Replace('\\', '/');
                if (string.Equals(relative, PendingUpdateApplier.ManifestFileName, StringComparison.OrdinalIgnoreCase)) continue;
                if (!IsApplicableFile(relative, target)) continue;
                builder.Append(HashFile(file)).Append('\t').Append(relative).Append('\n');
            }
            File.WriteAllText(Path.Combine(root, PendingUpdateApplier.ManifestFileName), builder.ToString(), new UTF8Encoding(false));
        }

        public static void WritePending(string updateRoot, string modPath, string version, string fromVersion, UpdateTarget target)
        {
            Directory.CreateDirectory(updateRoot);
            string pendingPath = Path.Combine(updateRoot, PendingUpdateApplier.PendingFileName);
            string tempPath = pendingPath + ".tmp";
            string text = "version=" + version + "\nfrom=" + fromVersion + "\ntarget=" + (target == UpdateTarget.Melon ? "Melon" : "Umm") + "\npath=" + Path.GetFullPath(modPath) + "\n";
            File.WriteAllText(tempPath, text, new UTF8Encoding(false));
            if (File.Exists(pendingPath)) File.Delete(pendingPath);
            File.Move(tempPath, pendingPath);
        }

        public static bool ApplyStaged(string updateRoot, string modPath)
        {
            return PendingUpdateApplier.Apply(updateRoot, modPath, Log, Error);
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

        private static void Log(string message)
        {
            ModContext.Logger?.Log(message);
        }

        private static void Error(string message)
        {
            ModContext.Logger?.Error(message);
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
