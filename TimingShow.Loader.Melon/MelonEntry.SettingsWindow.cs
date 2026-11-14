using System.Globalization;
using UnityEngine;
using TimingShow.Options;
using OptionsPage = TimingShow.Options.Options;

namespace TimingShow.Melon
{
    public partial class MelonEntry
    {
        private const float WindowWidth = 900f;
        private const float WindowHeight = 600f;
        private const float FooterHeight = 32f;

        private bool _showSettings;
        private bool _isRebinding;
        private float _renderScale = 1f;
        private float _pendingScale = 1f;
        private Rect _settingsWindowRect = new Rect(20f, 20f, WindowWidth, WindowHeight);
        private Vector2 _scrollPos;

        public override void OnGUI()
        {
            if (!_showSettings) return;

            float scale = _renderScale;
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            try
            {
                float availableWidth = Screen.width / scale;
                float availableHeight = Screen.height / scale;
                _settingsWindowRect.width = Mathf.Min(WindowWidth, Mathf.Max(320f, availableWidth));
                _settingsWindowRect.height = Mathf.Min(WindowHeight, Mathf.Max(220f, availableHeight));
                _settingsWindowRect.x = Mathf.Clamp(_settingsWindowRect.x, 0f, Mathf.Max(0f, availableWidth - _settingsWindowRect.width));
                _settingsWindowRect.y = Mathf.Clamp(_settingsWindowRect.y, 0f, Mathf.Max(0f, availableHeight - _settingsWindowRect.height));

                _settingsWindowRect = GUI.Window(GetHashCode(), _settingsWindowRect, DrawSettingsWindow, GUIContent.none, SettingsWindow.WindowStyle);
            }
            finally
            {
                GUI.matrix = previous;
            }
        }

        private static float CurrentWindowScale
        {
            get { return Mathf.Clamp(ModContext.Settings.WindowScale, Settings.MinWindowScale, Settings.MaxWindowScale); }
        }

        private void OpenSettings()
        {
            _renderScale = CurrentWindowScale;
            _pendingScale = _renderScale;
            _showSettings = true;
        }

        private void CloseSettings()
        {
            ModContext.Settings.WindowScale = _pendingScale;
            ModEntry.SaveSettings();
            _showSettings = false;
        }

        private void DrawSettingsWindow(int id)
        {
            Rect windowRect = new Rect(0f, 0f, _settingsWindowRect.width, _settingsWindowRect.height);
            bool close = SettingsWindow.DrawFrame(windowRect, $"TimingShow v{Info.Version}");

            Rect content = SettingsWindow.ContentRect(windowRect);
            Rect scrollRect = new Rect(content.x, content.y, content.width, Mathf.Max(40f, content.height - FooterHeight - 10f));
            Rect footerRect = new Rect(content.x, content.yMax - FooterHeight, content.width, FooterHeight);

            GUILayout.BeginArea(scrollRect);
            _scrollPos = GUILayout.BeginScrollView(_scrollPos);
            OptionsPage.OnGUI();
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            DrawFooter(footerRect);

            if (!close) return;

            CloseSettings();
        }

        private void DrawFooter(Rect rect)
        {
            SettingsWindow.Divider(new Rect(rect.x, rect.y - 6f, rect.width, 1f));
            SettingsWindow.Label(new Rect(rect.x, rect.y, 110f, rect.height), i18n.T("Label_ConfigKey"));

            KeyCode key = ModContext.Settings.ConfigKey;
            string keyText = _isRebinding
                ? i18n.T("Btn_PressKey")
                : (key == KeyCode.None ? i18n.T("Status_Disabled") : key.ToString());
            if (SettingsWindow.Button(new Rect(rect.x + 112f, rect.y + 2f, 130f, rect.height - 4f), keyText, _isRebinding)) _isRebinding = true;
            if (_isRebinding) CaptureConfigKey();

            float sliderX = rect.x + 252f;
            float sliderWidth = rect.xMax - 158f - sliderX;
            if (sliderWidth > 120f) DrawWindowScaleSlider(new Rect(sliderX, rect.y, sliderWidth, rect.height));

            if (!SettingsWindow.Button(new Rect(rect.xMax - 150f, rect.y + 2f, 150f, rect.height - 4f), i18n.T("Btn_SaveAndClose"), true)) return;

            CloseSettings();
        }

        private void DrawWindowScaleSlider(Rect rect)
        {
            float labelWidth = 42f;
            float valueWidth = 46f;
            SettingsWindow.Label(new Rect(rect.x, rect.y, labelWidth, rect.height), i18n.T("Label_WindowScale"));

            float trackX = rect.x + labelWidth + 6f;
            float trackWidth = Mathf.Max(40f, rect.width - labelWidth - valueWidth - 12f);
            Rect track = new Rect(trackX, rect.center.y - 8f, trackWidth, 16f);

            float scale = SettingsWindow.Slider(track, _pendingScale, Settings.MinWindowScale, Settings.MaxWindowScale);
            _pendingScale = Mathf.Round(scale * 20f) / 20f;

            SettingsWindow.ValueLabel(new Rect(rect.xMax - valueWidth, rect.y, valueWidth, rect.height), _pendingScale.ToString("0.00", CultureInfo.InvariantCulture) + "\u00D7");
        }

        private void CaptureConfigKey()
        {
            Event evt = Event.current;
            if (evt == null || !evt.isKey || evt.type != EventType.KeyDown || evt.keyCode == KeyCode.None) return;

            if (evt.keyCode != KeyCode.Escape)
            {
                ModContext.Settings.ConfigKey = evt.keyCode;
                MelonHotkeys.SetConfigKey(evt.keyCode);
            }

            _isRebinding = false;
            evt.Use();
        }
    }
}
