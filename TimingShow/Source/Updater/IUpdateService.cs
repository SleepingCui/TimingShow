using System;

namespace TimingShow.Update
{
    public enum UpdateBannerKind
    {
        None,
        Available,
        Downloading,
        Verifying,
        Ready,
        Failed
    }

    public interface IUpdateBannerSource
    {
        UpdateBannerKind BannerKind { get; }

        string BannerVersion { get; }

        float BannerProgress { get; }

        string BannerError { get; }

        void StartUpdate();

        void CancelDownload();

        void Dismiss();
    }

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
