using MelonLoader;
using System.IO;
using System.Reflection;
using UnityEngine;

[assembly: MelonInfo(typeof(TimingShow.MelonMain), "TimingShow", "1.8.1", "SleepingCui")]
[assembly: MelonGame("7th Beat Games", "A Dance of Fire and Ice")]

namespace TimingShow
{
    public class MelonMain : MelonMod
    {
        private bool _showSettings;
        private bool _isRebinding;
        private Rect _settingsWindowRect = new Rect(20, 20, 540, 600);
        private Vector2 _scrollPos;

        public override void OnInitializeMelon()
        {
            string modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var logger = new MelonLoggerAdapter();
            ModContext.Initialize(modPath, logger);
            ModContext.Settings = Settings.Load(modPath);

            LangMan.LoadLanguages(modPath);
            XPerfectBridge.TryInit();
            var harmony = new HarmonyLib.Harmony("TimingShow.Melon");
            ModContext.HarmonyInstance = harmony;
            ModContext.Enable();
        }

        public override void OnUpdate()
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

        public override void OnGUI()
        {
            if (!_showSettings) return;

            _settingsWindowRect = GUILayout.Window(GetHashCode(), _settingsWindowRect, (id) =>
                {
                    _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Width(520), GUILayout.Height(540));
                    Options.OnGUI();
                    GUILayout.EndScrollView();

                    // keybind
                    GUILayout.Space(4);
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("Config Key", GUILayout.Width(100));
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
                            // var oldColor = GUI.color;
                            // GUI.color = Color.yellow;
                            GUILayout.Button("Press a key...", GUILayout.Width(140));
                            // GUI.color = oldColor;
                        }
                        else
                        {
                            if (GUILayout.Button(ModContext.Settings.ConfigKey.ToString(), GUILayout.Width(140)))
                                _isRebinding = true;
                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Save & Close", GUILayout.Width(120)))
                    {
                        ModContext.SaveSettings();
                        _showSettings = false;
                    }
                    GUILayout.EndHorizontal();

                    GUI.DragWindow(new Rect(0, 0, 540, 20));
                },
                $"TimingShow (Melon)",
                GUILayout.Width(540),
                GUILayout.Height(600)
            );
        }

        public override void OnApplicationQuit()
        {
            ModContext.SaveSettings();
            ModContext.Disable();
        }
    }
}
