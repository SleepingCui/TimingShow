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
            GUI.enabled = previous;

            if (!supported)
                IndentedLabel("<color=#888888>" + i18n.T("Updater_Unsupported") + "</color>");
        }
    }
}
