using System;

namespace TimingShow.Update
{
    public interface IUpdateService
    {
        bool Supported { get; }

        string Suffix { get; }

        string StatusText { get; }

        string UnavailableText { get; }

        bool UnavailableIsError { get; }

        event Action Changed;

        void Initialize(string modPath, string updateRoot, string repositoryUrl, string version, UpdateTarget target, bool autoApplyAvailable, IModLogger logger);

        void DrawBanner();

        void DrawSettings();

        void CheckNow();
    }
}
