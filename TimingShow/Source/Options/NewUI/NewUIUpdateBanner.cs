using TimingShow.Update;
using UnityEngine;

namespace TimingShow.Options
{
    internal static class NewUIUpdateBanner
    {
        private const float Height = 44f;
        private const float IconSize = 20f;
        private const float ButtonWidth = 76f;
        private const float SkipButtonWidth = 120f;
        private const float ButtonHeight = 24f;
        private const float ButtonGap = 6f;
        private const float Padding = 12f;

        internal static void Draw()
        {
            IUpdateBannerSource source = UpdateBridge.Banner;
            if (source == null)
            {
                UpdateBridge.DrawBanner();
                return;
            }

            UpdateBannerKind kind = source.BannerKind;
            if (kind == UpdateBannerKind.None) return;

            bool error = kind == UpdateBannerKind.Failed;
            bool downloading = kind == UpdateBannerKind.Downloading;

            Rect row = NewUIWidgets.Row(Height);
            Color fill = error
                ? NewUISkin.Lighten(Color.Lerp(NewUISkin.PanelAlt, NewUISkin.Error, 0.12f), 0.02f)
                : kind == UpdateBannerKind.Available ? NewUISkin.Lighten(Color.Lerp(NewUISkin.PanelAlt, NewUISkin.Accent, 0.12f), 0.02f) : NewUISkin.PanelAlt;
            Color border = error ? NewUISkin.ErrorBorder : NewUISkin.Border;
            NewUISkin.DrawBorder(row, 8f, 1f, border, fill);

            Rect iconRect = new Rect(row.x + Padding, row.y + (Height - IconSize) * 0.5f, IconSize, IconSize);
            Texture2D icon = error ? NewUISkin.StatusErrorIcon : NewUISkin.StatusInfoIcon;
            if (icon != null) GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);

            float buttonY = row.y + (Height - ButtonHeight) * 0.5f;
            float textRight = row.xMax - Padding;
            if (kind != UpdateBannerKind.Ready)
            {
                Rect cancelRect = new Rect(textRight - ButtonWidth, buttonY, ButtonWidth, ButtonHeight);
                if (NewUIWidgets.ButtonAt(cancelRect, i18n.T("Btn_Cancel")))
                {
                    if (downloading || kind == UpdateBannerKind.Verifying) source.CancelDownload();
                    else source.Dismiss();
                }
                textRight = cancelRect.x - ButtonGap;
            }

            if (kind == UpdateBannerKind.Available || error)
            {
                Rect primaryRect = new Rect(textRight - ButtonWidth, buttonY, ButtonWidth, ButtonHeight);
                if (NewUIWidgets.ButtonAt(primaryRect, i18n.T(error ? "Btn_Retry" : "Btn_Update"))) source.StartUpdate();
                textRight = primaryRect.x - ButtonGap;
            }

            if (kind == UpdateBannerKind.Available)
            {
                Rect skipRect = new Rect(textRight - SkipButtonWidth, buttonY, SkipButtonWidth, ButtonHeight);
                if (NewUIWidgets.ButtonAt(skipRect, i18n.T("Btn_SkipVersion"))) source.SkipVersion();
                textRight = skipRect.x - ButtonGap;
            }

            float textX = iconRect.xMax + 10f;
            float textWidth = Mathf.Max(20f, textRight - textX);
            float textY = row.y + (downloading ? 9f : (Height - 18f) * 0.5f);
            GUI.Label(new Rect(textX, textY, textWidth, 18f), Text(kind, source), NewUISkin.LabelStyle);

            if (downloading)
            {
                Rect track = new Rect(textX, row.y + Height - 14f, textWidth, 4f);
                NewUISkin.DrawCapsule(track, NewUISkin.Fade(NewUISkin.Border, 0.7f));
                float width = track.width * Mathf.Clamp01(source.BannerProgress);
                if (width > 1f) NewUISkin.DrawCapsule(new Rect(track.x, track.y, width, track.height), NewUISkin.Accent);
            }

            NewUIWidgets.Space(2f);
        }

        private static string Text(UpdateBannerKind kind, IUpdateBannerSource source)
        {
            switch (kind)
            {
                case UpdateBannerKind.Available: return string.Format(i18n.T("Updater_Banner"), source.BannerVersion);
                case UpdateBannerKind.Downloading:
                    return string.Format(i18n.T("Updater_Downloading"), source.BannerVersion) + " " + (Mathf.Clamp01(source.BannerProgress) * 100f).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "%";
                case UpdateBannerKind.Verifying: return i18n.T("Updater_Verifying");
                case UpdateBannerKind.Failed: return string.Format(i18n.T("Updater_Failed"), source.BannerError);
                default: return i18n.T("Updater_Pending");
            }
        }
    }
}
