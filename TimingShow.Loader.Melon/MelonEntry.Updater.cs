using MelonLoader.Utils;
using System.IO;
using TimingShow.Update;

namespace TimingShow.Melon
{
    public partial class MelonEntry
    {
        private const string UpdateDirectoryName = "Update";

        private static void InitializeUpdater(string modPath, string version, IModLogger logger)
        {
            string updateRoot = Path.Combine(MelonEnvironment.UserDataDirectory, "TimingShow", UpdateDirectoryName);
            string bootstrapPath = Path.Combine(MelonEnvironment.PluginsDirectory, "TimingShow.UpdateBootstrap.dll");
            bool bootstrapInstalled = File.Exists(bootstrapPath);
            if (!bootstrapInstalled) logger.Error("TimingShow.UpdateBootstrap.dll is missing from " + MelonEnvironment.PluginsDirectory + "; automatic updates are disabled.");

            UpdateBridge.Initialize(modPath, updateRoot, UpdateBridge.DefaultRepositoryUrl, version, UpdateTarget.Melon, bootstrapInstalled, logger);
        }
    }
}
