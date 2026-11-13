using MelonLoader;
using MelonLoader.Utils;
using System.IO;
using System.Reflection;
using UnityEngine;
using TimingShow.Bridge;
using TimingShow.Options;
using TimingShow.Update;
using OptionsPage = TimingShow.Options.Options;

namespace TimingShow.Melon
{
    public class MelonEntry : MelonMod
    {
        private const float WindowWidth = 900f;
        private const float WindowHeight = 600f;
        private const float FooterHeight = 32f;
        private const string UpdateDirectoryName = "Update";

        private bool _showSettings;
        private bool _isRebinding;
        private Rect _settingsWindowRect = new Rect(20, 20, WindowWidth, WindowHeight);
        private Vector2 _scrollPos;

        public override void OnInitializeMelon()
        {
            string modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var logger = new MelonLoggerAdapter();
            ModContext.Initialize(modPath, logger);
            ModContext.IsMelon = true;
            ModContext.Settings = Settings.Load(modPath);

            i18n.LoadLanguages(modPath);
            MelonHotkeys.Initialize(ModContext.Settings);
            ModContext.InitializeJudgeCompat();
            XPerfectBridge.TryInit();
            var harmony = new HarmonyLib.Harmony("TimingShow.Loader.Melon");
            ModContext.HarmonyInstance = harmony;
            ModContext.Enable();

            InitializeUpdater(modPath, Info.Version, logger);
        }

        private static void InitializeUpdater(string modPath, string version, IModLogger logger)
        {
            string updateRoot = Path.Combine(MelonEnvironment.UserDataDirectory, "TimingShow", UpdateDirectoryName);
            string bootstrapPath = Path.Combine(MelonEnvironment.PluginsDirectory, "TimingShow.UpdateBootstrap.dll");
            bool bootstrapInstalled = File.Exists(bootstrapPath);
            if (!bootstrapInstalled) logger.Error("TimingShow.UpdateBootstrap.dll is missing from " + MelonEnvironment.PluginsDirectory + "; automatic updates are disabled.");

            UpdateBridge.Initialize(modPath, updateRoot, UpdateBridge.DefaultRepositoryUrl, version, UpdateTarget.Melon, bootstrapInstalled, logger);
        }

        public override void OnUpdate()
        {
            if (_isRebinding) return;

            var key = ModContext.Settings.ConfigKey;
            if (key == KeyCode.None) key = KeyCode.F9;

            if (Input.GetKeyDown(key))
            {
                _showSettings = !_showSettings;
                if (!_showSettings)
                    ModContext.SaveSettings();
            }
        }

        public override void OnGUI()
        {
            if (!_showSettings) return;

            _settingsWindowRect = GUI.Window(GetHashCode(), _settingsWindowRect, DrawSettingsWindow, GUIContent.none, SettingsWindow.WindowStyle);
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

            ModContext.SaveSettings();
            _showSettings = false;
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

            if (!SettingsWindow.Button(new Rect(rect.xMax - 150f, rect.y + 2f, 150f, rect.height - 4f), i18n.T("Btn_SaveAndClose"), true)) return;

            ModContext.SaveSettings();
            _showSettings = false;
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

        public override void OnApplicationQuit()
        {
            ModContext.SaveSettings();
            ModContext.Disable();
        }
    }
}