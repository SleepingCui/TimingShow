using UnityEngine;
using static TimingShow.Options.OptionsWidgets;

namespace TimingShow.Update
{
    internal static class UpdateSettingsUI
    {
        public static void Draw()
        {
            Settings settings = ModContext.Settings;
            if (settings == null) return;

            bool supported = Updater.Supported;
            bool previous = GUI.enabled;
            GUI.enabled = supported;
            Toggle(ref settings.UpdateCheckEnabled, "Updater_Enable");

            GUI.enabled = supported && settings.UpdateCheckEnabled;
            Toggle(ref settings.UpdateAutoUpdate, "Updater_Auto");

            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(ContentIndent);
                if (GUILayout.Button(i18n.T("Btn_CheckUpdateNow"), GUILayout.Width(200)))
                    Updater.CheckNow();
                GUILayout.Label(CheckResultText(), GUILayout.ExpandWidth(true));
            }
            GUILayout.EndHorizontal();

            GUI.enabled = previous;

            if (!supported)
            {
                string color = Updater.UnavailableReasonKey == Updater.BootstrapMissingReasonKey ? "#FF5555" : "#888888";
                IndentedLabel($"<color={color}>{i18n.T(Updater.UnavailableReasonKey)}</color>");
            }
        }

        private static string CheckResultText()
        {
            switch (Updater.CheckResult)
            {
                case UpdateCheckResult.UpToDate:
                    return i18n.T("Updater_CheckLatest");
                case UpdateCheckResult.Available:
                    return i18n.T("Updater_CheckAvailable");
                case UpdateCheckResult.Failed:
                    return string.Format(i18n.T("Updater_CheckFailed"), Updater.CheckError);
                default:
                    return string.Empty;
            }
        }
    }
}
