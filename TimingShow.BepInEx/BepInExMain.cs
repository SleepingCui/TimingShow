using BepInEx;
using HarmonyLib;
using System.IO;
using UnityEngine;

namespace TimingShow
{
    [BepInPlugin(Guid, "TimingShow", Version)]
    public class BepInExMain : BaseUnityPlugin
    {
        public const string Guid = "TimingShow";
        public const string Version = "1.9.1";

        private const float WindowWidth = 900f;
        private const float WindowHeight = 600f;

        private bool _showSettings;
        private bool _isRebinding;
        private Rect _settingsWindowRect = new Rect(20, 20, WindowWidth, WindowHeight);
        private Vector2 _scrollPos;

        private void Awake()
        {
            string modPath = Path.GetDirectoryName(Info.Location);
            var logger = new BepInExLoggerAdapter(Logger);
            ModContext.Initialize(modPath, logger);
            ModContext.Settings = Settings.Load(modPath);

            i18n.LoadLanguages(ModContext.ModPath);
            ModContext.InitializeJudgeCompat();
            XPerfectBridge.TryInit();

            var harmony = new Harmony(Info.Metadata.GUID);
            ModContext.HarmonyInstance = harmony;
            ModContext.Enable();
        }

        private void Update()
        {
            var key = ModContext.Settings.ConfigKey;
            if (key == KeyCode.None) key = KeyCode.F9;

            if (Input.GetKeyDown(key))
            {
                _showSettings = !_showSettings;
                if (!_showSettings)
                    ModContext.SaveSettings();
            }
        }

        private void OnGUI()
        {
            if (!_showSettings) return;

            _settingsWindowRect = GUILayout.Window(GetHashCode(), _settingsWindowRect, (id) =>
                {
                    _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Width(WindowWidth - 20), GUILayout.Height(WindowHeight - 60));
                    Options.OnGUI();
                    GUILayout.EndScrollView();

                    // keybind
                    GUILayout.Space(4);
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label(i18n.T("Label_ConfigKey"), GUILayout.Width(100));
                        if (_isRebinding)
                        {
                            var evt = Event.current;
                            if (evt != null && evt.isKey && evt.type == EventType.KeyDown)
                            {
                                if (evt.keyCode == KeyCode.Escape)
                                    _isRebinding = false;
                                else
                                {
                                    ModContext.Settings.ConfigKey = evt.keyCode;
                                    _isRebinding = false;
                                }
                                evt.Use();
                            }
                            GUILayout.Button(i18n.T("Btn_PressKey"), GUILayout.Width(140));
                        }
                        else
                        {
                            if (GUILayout.Button(ModContext.Settings.ConfigKey.ToString(), GUILayout.Width(140)))
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
                $"TimingShow v{Info.Metadata.Version}",
                GUILayout.Width(WindowWidth),
                GUILayout.Height(WindowHeight)
            );
        }

        private void OnApplicationQuit()
        {
            ModContext.SaveSettings();
            ModContext.Disable();
        }
    }
}
