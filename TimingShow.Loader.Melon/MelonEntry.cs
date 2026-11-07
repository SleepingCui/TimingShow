using MelonLoader;
using MelonLoader.Utils;
using System.IO;
using System.Reflection;
using UnityEngine;
using TimingShow.Bridge;
using TimingShow.Update;
using OptionsPage = TimingShow.Options.Options;

namespace TimingShow.Melon
{
    public class MelonEntry : MelonMod
    {
        private const float WindowWidth = 900f;
        private const float WindowHeight = 600f;
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

            Updater.Initialize(modPath, updateRoot, Updater.DefaultRepositoryUrl, version, UpdateTarget.Melon, bootstrapInstalled, logger);
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

            _settingsWindowRect = GUILayout.Window(GetHashCode(), _settingsWindowRect, (id) =>
                {
                    _scrollPos = GUILayout.BeginScrollView( _scrollPos, GUILayout.Width(WindowWidth - 20), GUILayout.Height(WindowHeight - 60));
                    OptionsPage.OnGUI();
                    GUILayout.EndScrollView();

                    // keybind
                    GUILayout.Space(4);
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label(i18n.T("Label_ConfigKey"), GUILayout.Width(100));
                        if (_isRebinding)
                        {
                            GUILayout.Button(i18n.T("Btn_PressKey"), GUILayout.Width(140));
                            CaptureConfigKey();
                        }
                        else
                        {
                            KeyCode key = ModContext.Settings.ConfigKey;
                            string keyText = key == KeyCode.None ? i18n.T("Status_Disabled") : key.ToString();
                            if (GUILayout.Button(keyText, GUILayout.Width(140)))
                                _isRebinding = true;
                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(i18n.T("Btn_SaveAndClose"), GUILayout.Width(120)))
                    {
                        ModContext.SaveSettings();
                        _showSettings = false;
                    }
                    GUILayout.EndHorizontal();

                    GUI.DragWindow(new Rect(0, 0, WindowWidth, 20));
                },
                $"TimingShow v{Info.Version}",
                GUILayout.Width(WindowWidth),
                GUILayout.Height(WindowHeight)
            );
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