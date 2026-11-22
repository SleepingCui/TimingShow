using System;
using System.IO;
using TimingShow.HUD;
using TimingShow.Update;

namespace TimingShow.Options
{
    internal static class CacheCleaner
    {
        private const string UpdateDirectoryName = "Update";
        private const string DownloadDirectoryName = "download";
        private const string StagingDirectoryName = "staging";
        private const string PendingFileName = "pending.json";

        internal static string Clear()
        {
            int files = 0;
            long bytes = 0;

            DeleteFile(OptionLogList.MetaCachePath, ref files, ref bytes);
            DeleteCacheFolder(OptionLogList.GetCacheDirectory(), ref files, ref bytes);
            DeleteCacheFolder(LogGraphFont.CacheDirectory(), ref files, ref bytes);
            DeleteUpdateLeftovers(ref files, ref bytes);

            OptionLogList.ResetCacheState();

            return files == 0
                ? i18n.T("Hint_CacheEmpty")
                : string.Format(i18n.T("Hint_CacheCleared"), files, OptionLogList.FormatFileSize(bytes));
        }

        private static void DeleteCacheFolder(string directory, ref int files, ref long bytes)
        {
            if (!IsDedicatedCacheFolder(directory)) return;
            DeleteFiles(directory, ref files, ref bytes);
        }

        private static bool IsDedicatedCacheFolder(string directory)
        {
            if (string.IsNullOrEmpty(directory)) return false;

            string leaf;
            try
            {
                leaf = Path.GetFileName(directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            }
            catch (Exception e)
            {
                ModLog.UnityLog("Failed to inspect cache directory " + directory + ": " + e.Message);
                return false;
            }

            if (string.IsNullOrEmpty(leaf)) return false;
            return string.Equals(leaf, "Cache", StringComparison.OrdinalIgnoreCase)
                || string.Equals(leaf, "TimingShow", StringComparison.OrdinalIgnoreCase);
        }

        private static void DeleteFiles(string directory, ref int files, ref long bytes)
        {
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory)) return;

            string[] paths;
            try
            {
                paths = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
            }
            catch (Exception e)
            {
                ModLog.UnityLog("Failed to enumerate cache files: " + e.Message);
                return;
            }

            for (int i = 0; i < paths.Length; i++) DeleteFile(paths[i], ref files, ref bytes);
        }

        private static void DeleteFile(string path, ref int files, ref long bytes)
        {
            if (!File.Exists(path)) return;

            try
            {
                long length = new FileInfo(path).Length;
                File.Delete(path);
                files++;
                bytes += length;
            }
            catch (Exception e)
            {
                ModLog.UnityLog("Failed to delete cache file " + path + ": " + e.Message);
            }
        }

        private static void DeleteUpdateLeftovers(ref int files, ref long bytes)
        {
            string dataDirectory = ModContext.DataDirectory;
            if (string.IsNullOrEmpty(dataDirectory)) return;

            string updateRoot = Path.Combine(dataDirectory, UpdateDirectoryName);
            if (!Directory.Exists(updateRoot) || IsUpdatePending(updateRoot)) return;

            DeleteDirectory(Path.Combine(updateRoot, DownloadDirectoryName), ref files, ref bytes);
            DeleteDirectory(Path.Combine(updateRoot, StagingDirectoryName), ref files, ref bytes);
        }

        private static bool IsUpdatePending(string updateRoot)
        {
            if (File.Exists(Path.Combine(updateRoot, PendingFileName))) return true;

            IUpdateBannerSource banner = UpdateBridge.Banner;
            if (banner == null) return false;

            UpdateBannerKind kind = banner.BannerKind;
            return kind == UpdateBannerKind.Downloading || kind == UpdateBannerKind.Verifying || kind == UpdateBannerKind.Ready;
        }

        private static void DeleteDirectory(string directory, ref int files, ref long bytes)
        {
            if (!Directory.Exists(directory)) return;

            DeleteFiles(directory, ref files, ref bytes);
            try
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
            catch (Exception e)
            {
                ModLog.UnityLog("Failed to delete update leftovers " + directory + ": " + e.Message);
            }
        }
    }
}
