using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TimingShow.Update;

namespace TimingShow.Bootstrap
{
    internal sealed class StartupIntegrityReport
    {
        public bool Enabled;
        public bool CoreUsable;
        public bool NeedsRepair;
        public string Summary = string.Empty;
        public readonly List<string> Problems = new List<string>();

        public void Finish()
        {
            NeedsRepair = Enabled && Problems.Count > 0;
            Summary = string.Join(", ", Problems.ToArray());
        }
    }

    internal static class StartupIntegrity
    {
        public const string SettingKey = "Startup_VerifyFiles";
        public const string BootstrapFileName = "TimingShow.UpdateBootstrap.dll";

        private const string CoreAssemblyName = "TimingShow";
        private const string UpdaterFileName = "TimingShow.Updater.dll";
        private const string UpdaterAssemblyName = "TimingShow.Updater";
        private const string BootstrapAssemblyName = "TimingShow.UpdateBootstrap";

        public static StartupIntegrityReport Check(string modPath, string pluginsPath, bool melonLayout, string loaderFileName, string settingsPath)
        {
            StartupIntegrityReport report = new StartupIntegrityReport();
            report.Enabled = IsEnabled(settingsPath);
            if (report.Enabled)
            {
                string coreProblem = CheckAssembly(Path.Combine(modPath, RepairInstaller.CoreFileName), CoreAssemblyName);
                report.CoreUsable = coreProblem == null;
                Add(report, coreProblem);
                Add(report, CheckAssembly(Path.Combine(modPath, UpdaterFileName), UpdaterAssemblyName));
                Add(report, CheckAssembly(Path.Combine(modPath, loaderFileName), Path.GetFileNameWithoutExtension(loaderFileName)));
                Add(report, CheckLanguage(modPath));
                if (melonLayout)
                {
                    if (!string.IsNullOrEmpty(pluginsPath)) Add(report, CheckAssembly(Path.Combine(pluginsPath, BootstrapFileName), BootstrapAssemblyName));
                }
                else
                {
                    Add(report, CheckInfo(modPath));
                }
            }

            report.Finish();
            return report;
        }

        public static bool IsEnabled(string settingsPath)
        {
            if (string.IsNullOrEmpty(settingsPath) || !File.Exists(settingsPath)) return true;

            try
            {
                Dictionary<string, object> settings = JsonText.ParseObject(File.ReadAllText(settingsPath));
                object value;
                if (!settings.TryGetValue(SettingKey, out value)) return true;
                if (!(value is bool)) return true;
                return (bool)value;
            }
            catch (Exception)
            {
                return true;
            }
        }

        public static string ResolveLoaderFileName(string modPath, string fallback)
        {
            string candidate = null;
            try
            {
                string location = Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(location)) candidate = TrimToAssemblyName(Path.GetFileName(location));
            }
            catch (Exception)
            {
            }

            if (candidate != null && File.Exists(Path.Combine(modPath, candidate))) return candidate;
            return fallback;
        }

        internal static string TrimToAssemblyName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;
            int index = fileName.IndexOf(".dll", StringComparison.OrdinalIgnoreCase);
            if (index < 0) return null;
            return fileName.Substring(0, index + 4);
        }

        private static void Add(StartupIntegrityReport report, string problem)
        {
            if (problem != null) report.Problems.Add(problem);
        }

        private static string CheckAssembly(string path, string expectedName)
        {
            string fileName = Path.GetFileName(path);
            if (!File.Exists(path)) return fileName + " is missing";

            try
            {
                if (new FileInfo(path).Length == 0) return fileName + " is empty";
            }
            catch (Exception)
            {
                return fileName + " could not be read";
            }

            try
            {
                AssemblyName assembly = AssemblyName.GetAssemblyName(path);
                if (assembly == null || !string.Equals(assembly.Name, expectedName, StringComparison.OrdinalIgnoreCase)) return fileName + " is not " + expectedName;
            }
            catch (BadImageFormatException)
            {
                return fileName + " is corrupted";
            }
            catch (FileLoadException)
            {
                return fileName + " is corrupted";
            }
            catch (Exception)
            {
                return fileName + " could not be read";
            }

            return null;
        }

        private static string CheckLanguage(string modPath)
        {
            string path = Path.Combine(modPath, RepairInstaller.LanguageFileName);
            if (!File.Exists(path)) return RepairInstaller.LanguageFileName + " is missing";
            if (SafeLength(path) == 0) return RepairInstaller.LanguageFileName + " is empty";

            try
            {
                Dictionary<string, object> languages = JsonText.ParseObject(File.ReadAllText(path));
                if (languages.Count == 0) return RepairInstaller.LanguageFileName + " lists no languages";
            }
            catch (FormatException)
            {
                return RepairInstaller.LanguageFileName + " is not valid JSON";
            }
            catch (Exception)
            {
                return RepairInstaller.LanguageFileName + " could not be read";
            }

            return null;
        }

        private static string CheckInfo(string modPath)
        {
            string path = Path.Combine(modPath, RepairInstaller.InfoFileName);
            if (!File.Exists(path)) return RepairInstaller.InfoFileName + " is missing";

            try
            {
                Dictionary<string, object> info = JsonText.ParseObject(File.ReadAllText(path));
                if (!HasText(info, "Id")) return RepairInstaller.InfoFileName + " has no Id";
                if (!HasText(info, "EntryMethod")) return RepairInstaller.InfoFileName + " has no EntryMethod";
            }
            catch (FormatException)
            {
                return RepairInstaller.InfoFileName + " is not valid JSON";
            }
            catch (Exception)
            {
                return RepairInstaller.InfoFileName + " could not be read";
            }

            return null;
        }

        private static bool HasText(Dictionary<string, object> values, string key)
        {
            object value;
            return values.TryGetValue(key, out value) && value is string && !string.IsNullOrEmpty((string)value);
        }

        private static long SafeLength(string path)
        {
            try
            {
                return new FileInfo(path).Length;
            }
            catch (Exception)
            {
                return 1;
            }
        }
    }
}
