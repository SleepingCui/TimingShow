using UnityEngine;

namespace TimingShow.Update
{
    internal static class UpdateBanner
    {
        public static void Draw()
        {
            switch (Updater.State)
            {
                case UpdateState.Available:
                    DrawAvailable();
                    break;
                case UpdateState.Downloading:
                    DrawDownloading();
                    break;
                case UpdateState.Verifying:
                    DrawVerifying();
                    break;
                case UpdateState.Ready:
                    DrawMessage(i18n.T("Updater_Pending"));
                    break;
                case UpdateState.Failed:
                    DrawFailed();
                    break;
            }
        }

        private static void DrawAvailable()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(string.Format(i18n.T("Updater_Banner"), Updater.AvailableVersion), GUILayout.ExpandWidth(true));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(i18n.T("Btn_Update"), GUILayout.Width(120f))) Updater.StartUpdate();
            if (GUILayout.Button(i18n.T("Btn_Cancel"), GUILayout.Width(120f))) Updater.Dismiss();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.Space(8);
        }

        private static void DrawDownloading()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            string label = string.Format(i18n.T("Updater_Downloading"), Updater.AvailableVersion);
            GUILayout.Label(label + " " + Mathf.RoundToInt(Updater.Progress * 100f) + "%", GUILayout.ExpandWidth(true));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(i18n.T("Btn_Cancel"), GUILayout.Width(120f))) Updater.CancelDownload();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.Space(8);
        }

        private static void DrawVerifying()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(i18n.T("Updater_Verifying"), GUILayout.ExpandWidth(true));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(i18n.T("Btn_Cancel"), GUILayout.Width(120f))) Updater.CancelDownload();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.Space(8);
        }

        private static void DrawFailed()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(string.Format(i18n.T("Updater_Failed"), Updater.ErrorMessage), GUILayout.ExpandWidth(true));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(i18n.T("Btn_Retry"), GUILayout.Width(120f))) Updater.StartUpdate();
            if (GUILayout.Button(i18n.T("Btn_Cancel"), GUILayout.Width(120f))) Updater.Dismiss();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.Space(8);
        }

        private static void DrawMessage(string text)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(text, GUILayout.ExpandWidth(true));
            GUILayout.EndVertical();
            GUILayout.Space(8);
        }
    }
}
