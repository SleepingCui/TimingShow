using System;

namespace TimingShow.Bootstrap
{
    internal static class CoreFailureDetector
    {
        private const string CoreFrame = "at TimingShow.";

        private static readonly string[] LoaderFrames =
        {
            "at TimingShow.UMM.",
            "at TimingShow.Melon.",
            "at TimingShow.Bootstrap.",
            "at TimingShow.Loader."
        };

        public static bool IsCoreCrash(string condition, string stackTrace)
        {
            return HasCoreFrame(condition) || HasCoreFrame(stackTrace);
        }

        private static bool HasCoreFrame(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            foreach (string line in text.Split('\n'))
            {
                if (line.IndexOf(CoreFrame, StringComparison.Ordinal) < 0) continue;
                if (!IsLoaderFrame(line)) return true;
            }
            return false;
        }

        private static bool IsLoaderFrame(string line)
        {
            foreach (string frame in LoaderFrames)
            {
                if (line.IndexOf(frame, StringComparison.Ordinal) >= 0) return true;
            }
            return false;
        }
    }
}
