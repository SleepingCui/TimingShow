using System;
using System.IO;
using MelonLoader;
using MelonLoader.Utils;
using TimingShow.Update;

namespace TimingShow.UpdateBootstrap
{
    public class BootstrapEntry : MelonPlugin
    {
        public override void OnPreInitialization()
        {
            try
            {
                string updateRoot = ResolveUpdateRoot();
                if (!PendingUpdateFile.Exists(updateRoot)) return;

                MelonLogger.Msg("[UpdateBootstrap] Applying pending update from " + updateRoot);

                string fallbackModPath = MelonEnvironment.ModsDirectory;
                PendingUpdateApplier.Apply(updateRoot, fallbackModPath, Log, Error);
            }
            catch (Exception e)
            {
                Error("Failed to apply the pending update: " + e);
            }
        }

        private static string ResolveUpdateRoot()
        {
            try
            {
                string userData = MelonEnvironment.UserDataDirectory;
                if (!string.IsNullOrEmpty(userData)) return Path.Combine(userData, "TimingShow", PendingUpdateApplier.UpdateDirectoryName);
            }
            catch (Exception)
            {
            }

            return Path.Combine(MelonEnvironment.GameRootDirectory, "UserData", "TimingShow", PendingUpdateApplier.UpdateDirectoryName);
        }

        private static void Log(string message)
        {
            MelonLogger.Msg("[UpdateBootstrap] " + message);
        }

        private static void Error(string message)
        {
            MelonLogger.Error("[UpdateBootstrap] " + message);
        }
    }
}
