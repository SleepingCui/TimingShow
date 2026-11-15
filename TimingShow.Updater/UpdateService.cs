using System;

namespace TimingShow.Update
{
    public sealed class UpdateService : IUpdateService, IUpdateBannerSource
    {
        public bool Supported
        {
            get { return Updater.Supported; }
        }

        public string Suffix
        {
            get { return Updater.Suffix; }
        }

        public string StatusText
        {
            get { return UpdateSettingsUI.CheckResultText(); }
        }

        public string UnavailableText
        {
            get { return Updater.Supported ? string.Empty : i18n.T(Updater.UnavailableReasonKey); }
        }

        public bool UnavailableIsError
        {
            get { return Updater.UnavailableReasonKey == Updater.BootstrapMissingReasonKey; }
        }

        public event Action Changed
        {
            add { Updater.Changed += value; }
            remove { Updater.Changed -= value; }
        }

        public void Initialize(string modPath, string updateRoot, string repositoryUrl, string version, UpdateTarget target, bool autoApplyAvailable, IModLogger logger)
        {
            Updater.Initialize(modPath, updateRoot, repositoryUrl, version, target, autoApplyAvailable, logger);
        }

        public UpdateBannerKind BannerKind
        {
            get
            {
                switch (Updater.State)
                {
                    case UpdateState.Available: return UpdateBannerKind.Available;
                    case UpdateState.Downloading: return UpdateBannerKind.Downloading;
                    case UpdateState.Verifying: return UpdateBannerKind.Verifying;
                    case UpdateState.Ready: return UpdateBannerKind.Ready;
                    case UpdateState.Failed: return UpdateBannerKind.Failed;
                    default: return UpdateBannerKind.None;
                }
            }
        }

        public string BannerVersion
        {
            get { return Updater.AvailableVersion; }
        }

        public float BannerProgress
        {
            get { return Updater.Progress; }
        }

        public string BannerError
        {
            get { return Updater.ErrorMessage; }
        }

        public void StartUpdate()
        {
            Updater.StartUpdate();
        }

        public void CancelDownload()
        {
            Updater.CancelDownload();
        }

        public void Dismiss()
        {
            Updater.Dismiss();
        }

        public void DrawBanner()
        {
            UpdateBanner.Draw();
        }

        public void DrawSettings()
        {
            UpdateSettingsUI.Draw();
        }

        public void CheckNow()
        {
            Updater.CheckNow();
        }
    }
}
