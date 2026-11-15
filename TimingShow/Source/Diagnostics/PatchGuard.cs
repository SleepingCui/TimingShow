using System;
using System.Collections.Generic;

namespace TimingShow.Diagnostics
{
    internal static class PatchGuard
    {
        private const int MaxEntries = 24;
        private static readonly HashSet<string> Reported = new HashSet<string>();

        internal static bool Enabled
        {
            get
            {
                Settings settings = ModContext.Settings;
                return settings == null || settings.PatchExceptionGuard;
            }
        }

        internal static void Report(string patch, Exception exception)
        {
            string key = patch + "|" + exception.GetType().Name + "|" + exception.Message;
            if (Reported.Count >= MaxEntries) Reported.Clear();
            if (!Reported.Add(key)) return;

            ModContext.Logger?.Error($"Patch {patch} failed: {exception.GetType().Name}: {exception.Message}");
        }
    }
}
