using UnityEngine;

namespace TimingShow.SessionReport
{
    public sealed class SessionReportHotkey : MonoBehaviour
    {
        private static GameObject _host;

        public static void EnsureHost()
        {
            if (_host != null) return;

            try
            {
                _host = new GameObject("TimingShow_SessionReportHotkey");
                _host.hideFlags = HideFlags.HideAndDontSave;
                Object.DontDestroyOnLoad(_host);
                _host.AddComponent<SessionReportHotkey>();
            }
            catch (System.Exception e)
            {
                ModContext.Logger?.Error("Failed to create session report hotkey host: " + e.Message);
                if (_host != null) Object.Destroy(_host);
                _host = null;
            }
        }

        public static void DestroyHost()
        {
            if (_host == null) return;

            Object.Destroy(_host);
            _host = null;
        }

        private void Update()
        {
            if (!ModContext.IsEnabled) return;

            Settings settings = ModContext.Settings;
            if (settings == null || settings.ReportKey == KeyCode.None) return;
            if (ModContext.IsPlaying && !ModContext.IsLevelFinished) return;
            if (Options.Options.HotkeySuppressed) return;

            if (!Input.GetKeyDown(settings.ReportKey)) return;

            LastSessionReport.ToggleWindow();
        }
    }
}
