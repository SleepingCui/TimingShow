using System;
using System.IO;
using System.Diagnostics;
using UnityEngine;
using UnityModManagerNet;
using UnityFileDialog;

namespace TimingShow
{
    public static class Options
    {
        private static string _bufferSizeText;
        private static string _maxPointsText;
        private static bool _showAdvancedSettings;

        private static bool _foldoutTitleSettings;
        private static bool _foldoutPlanetSettings;
        private static bool _foldoutTimingHUD;
        private static bool _foldoutURHUD;
        private static bool _foldoutRatioHUD;
        private static bool _foldoutLogging;
        private static bool _foldoutXACCGraph;

        private static GUIStyle _activeButtonStyle;

        public static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            if (_activeButtonStyle == null) _activeButtonStyle = new GUIStyle(GUI.skin.button);

            // lang
            GUILayout.BeginHorizontal();
            foreach (string langCode in LangMan.AvailableLanguages)
            {
                _activeButtonStyle.fontStyle = (Main.Settings.Language == langCode) ? FontStyle.Bold : FontStyle.Normal;

                if (GUILayout.Button(langCode, _activeButtonStyle, GUILayout.Width(100)))
                {
                    Main.Settings.Language = langCode;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            // title
            SettingFold(LangMan.T("Toggle_Title"), ref Main.Settings.ShowInSongTitle, ref Main.Settings.Perc1, ref _foldoutTitleSettings);
            if (Main.Settings.ShowInSongTitle && _foldoutTitleSettings)
            {
                Toggle(ref Main.Settings.Title_UseJudgeColor, "HUD_UseJudgeColor");

                if (Main.Settings.Title_UseJudgeColor)
                {
                    Toggle(ref Main.Settings.Title_EnableXPerfect, "Enable_XP", 40);
                }
            }

            // planet 
            bool oldShowOnPlanet = Main.Settings.ShowOnPlanet;
            SettingFold(LangMan.T("Toggle_Planet"), ref Main.Settings.ShowOnPlanet, ref Main.Settings.Perc2, ref _foldoutPlanetSettings);
            if (oldShowOnPlanet != Main.Settings.ShowOnPlanet && Main.Settings.AutoReloadInEditor)
            {
                Patches.EditorReloadPatch.TriggerEditorReload();
            }

            if (Main.Settings.ShowOnPlanet && _foldoutPlanetSettings)
            {
                Toggle(ref Main.Settings.Planet_EnableXPerfect, "Enable_XP");

                GUILayout.Label(LangMan.T("Setting_Title"));
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(20);
                    GUILayout.BeginVertical();
                    {
                        ToggleNoIndent(ref Main.Settings.ReplaceFailOverload, "Toggle_FailOverload");
                        ToggleNoIndent(ref Main.Settings.ReplaceTooEarly, "Toggle_TooEarly");
                        ToggleNoIndent(ref Main.Settings.ReplaceVeryEarly, "Toggle_VeryEarly");
                        ToggleNoIndent(ref Main.Settings.ReplaceEarlyPerfect, "Toggle_EarlyPerfect");
                        ToggleNoIndent(ref Main.Settings.ReplacePerfect, "Toggle_Perfect");
                        ToggleNoIndent(ref Main.Settings.ReplaceLatePerfect, "Toggle_LatePerfect");
                        ToggleNoIndent(ref Main.Settings.ReplaceVeryLate, "Toggle_VeryLate");
                        ToggleNoIndent(ref Main.Settings.ReplaceTooLate, "Toggle_TooLate");
                        ToggleNoIndent(ref Main.Settings.ReplaceFailMiss, "Toggle_FailMiss");
                        ToggleNoIndent(ref Main.Settings.ReplaceMultipress, "Toggle_Multipress");
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
            }

            // death,win
            SettingRow(LangMan.T("Toggle_Death"), ref Main.Settings.ShowOnDeath, ref Main.Settings.Perc3);
            SettingRow(LangMan.T("Toggle_Win"), ref Main.Settings.ShowInWinPage, ref Main.Settings.Perc4);

            // timinghud
            ToggleFold(LangMan.T("Toggle_TimingHUD"), ref Main.Settings.ShowTimingHUD, ref _foldoutTimingHUD);
            if (Main.Settings.ShowTimingHUD && _foldoutTimingHUD)
            {
                HUDBase(
                    ref Main.Settings.HUD_x, ref Main.Settings.HUD_y, ref Main.Settings.HUD_scale,
                    ref Main.Settings.HUD_bold, ref Main.Settings.HUD_align, ref Main.Settings.HUD_Format,
                    ref Main.Settings.PercHUD
                );

                Toggle(ref Main.Settings.HUD_UseJudgeColor, "HUD_UseJudgeColor");

                if (Main.Settings.HUD_UseJudgeColor)
                {
                    Toggle(ref Main.Settings.HUD_EnableXPerfect, "Enable_XP", 40);
                }
            }

            // urhud
            ToggleFold(LangMan.T("Toggle_URHUD"), ref Main.Settings.ShowURHUD, ref _foldoutURHUD);
            if (Main.Settings.ShowURHUD && _foldoutURHUD)
            {
                HUDBase(
                    ref Main.Settings.URHUD_x, ref Main.Settings.URHUD_y, ref Main.Settings.URHUD_scale,
                    ref Main.Settings.URHUD_bold, ref Main.Settings.URHUD_align, ref Main.Settings.URHUD_Format,
                    ref Main.Settings.PercURHUD
                );
            }

            // ratiohud
            ToggleFold(LangMan.T("Toggle_RatioHUD"), ref Main.Settings.ShowRatioHUD, ref _foldoutRatioHUD);
            if (Main.Settings.ShowRatioHUD && _foldoutRatioHUD)
            {
                HUDBase(
                    ref Main.Settings.RatioHUD_x, ref Main.Settings.RatioHUD_y, ref Main.Settings.RatioHUD_scale,
                    ref Main.Settings.RatioHUD_bold, ref Main.Settings.RatioHUD_align, ref Main.Settings.RatioHUD_Format,
                    ref Main.Settings.PercRatioHUD
                );

                Toggle(ref Main.Settings.Ratio_UseXPerfect, "Enable_XP");
            }

            // xaccgraph
            ToggleFold(LangMan.T("Toggle_XACCGraph"), ref Main.Settings.ShowXACCGraph, ref _foldoutXACCGraph);
            if (Main.Settings.ShowXACCGraph && _foldoutXACCGraph)
            {
                Toggle(ref Main.Settings.XACCGraph_ShowEnd, "Toggle_ShowEnd");
                SliderFloat("Label_XOffset", ref Main.Settings.XACCGraph_X, 0.0f, 1.0f);
                SliderFloat("Label_YOffset", ref Main.Settings.XACCGraph_Y, 0.0f, 1.0f);
                SliderFloat("Label_Scale", ref Main.Settings.XACCGraph_Scale, 0.2f, 3.0f);
                IntField("Label_MaxPoints", ref _maxPointsText, ref Main.Settings.XACCGraph_MaxPoints, 20, 5000, 250);

                ColorPicker(LangMan.T("Label_BgColor"), ref Main.Settings.XACCGraph_BgColor);
                ColorPicker(LangMan.T("Label_LineColor"), ref Main.Settings.XACCGraph_LineColor);
                ColorPicker(LangMan.T("Label_GridColor"), ref Main.Settings.XACCGraph_GridColor);
                ColorPicker(LangMan.T("Label_AxisTextColor"), ref Main.Settings.XACCGraph_AxisTextColor);
                ColorPicker(LangMan.T("Label_InfoTextColor"), ref Main.Settings.XACCGraph_ValueTextColor);
            }

            // logger
            GUILayout.BeginVertical();
            {
                ToggleFold(LangMan.T("Toggle_Logging"), ref Main.Settings.EnableLogging, ref _foldoutLogging);

                if (Main.Settings.EnableLogging && _foldoutLogging)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    GUILayout.Label(LangMan.T("Label_Precision") + $"{Main.Settings.PercLog}", GUILayout.Width(120));
                    Main.Settings.PercLog = Mathf.RoundToInt(GUILayout.HorizontalSlider(Main.Settings.PercLog, 0, 5, GUILayout.Width(100)));
                    GUILayout.EndHorizontal();

                    Toggle(ref Main.Settings.Logger_EnableXPerfect, "Enable_XP");
                    Toggle(ref Main.Settings.LogAutoplay, "Toggle_LogAutoplay");
                    
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    GUILayout.Label(LangMan.T("Label_LogDir"), GUILayout.Width(140));

                    string absolutePath = AbsLogPath(Main.Settings.LogDirectory);
                    string displayPath = string.IsNullOrWhiteSpace(absolutePath) ? "未选择" : absolutePath;
                    GUILayout.Label(displayPath, GUILayout.MinWidth(280), GUILayout.MaxWidth(480));

                    if (GUILayout.Button(LangMan.T("Btn_Browse"), GUILayout.Width(70)))
                    {
                        string defaultDir = AbsLogPath(Main.Settings.LogDirectory);
                        if (string.IsNullOrWhiteSpace(defaultDir))
                            defaultDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Mods/TimingShow/Logs"));

                        string selectedFolder = FileBrowser.PickFolder(defaultDir, "Folder", new string[0], LangMan.T("Label_LogDir"));
                        if (!string.IsNullOrEmpty(selectedFolder))
                        {
                            Main.Settings.LogDirectory = Path.GetFullPath(selectedFolder);
                        }
                    }
                    GUILayout.EndHorizontal();
                    
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    GUILayout.Label(LangMan.T("Label_BufferSize"), GUILayout.Width(140));

                    if (_bufferSizeText == null) _bufferSizeText = Main.Settings.LogBufferSizeKB.ToString();
                    string newBufferSizeText = GUILayout.TextField(_bufferSizeText, GUILayout.Width(80));

                    if (newBufferSizeText != _bufferSizeText)
                    {
                        if (int.TryParse(newBufferSizeText, out int parsedVal) && parsedVal >= 8 && parsedVal <= 102400)
                        {
                            _bufferSizeText = newBufferSizeText;
                            Main.Settings.LogBufferSizeKB = parsedVal;
                        }
                        else
                        {
                            _bufferSizeText = "64";
                            Main.Settings.LogBufferSizeKB = 64;
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                
                GUILayout.Space(10);
                if (GUILayout.Button(LangMan.T("Btn_OpenLogs"), GUILayout.Width(150)))
                {
                    try
                    {
                        string logDir = string.IsNullOrWhiteSpace(Main.Settings.LogDirectory) ? Path.Combine(Application.dataPath, "../Mods/TimingShow/Logs") : Main.Settings.LogDirectory;
                        if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
                        Process.Start(new ProcessStartInfo() { FileName = logDir, UseShellExecute = true, Verb = "open" });
                    }
                    catch (Exception e)
                    {
                        Main.Logger.Error(e.Message);
                    }
                }
            }
            GUILayout.EndVertical();

            if (GUILayout.Button(LangMan.T("Btn_Reset"), GUILayout.Width(150)))
            {
                Main.SessionOffsets.Clear();
                Main.LastHitMargin = HitMargin.Perfect;
                Main.LastTiming = 0;
            }

            // adv settings
            string foldoutArrow = _showAdvancedSettings ? "▲" : "▼";
            if (GUILayout.Button($"{LangMan.T("Btn_Advanced")} {foldoutArrow}", GUILayout.Width(150)))
                _showAdvancedSettings = !_showAdvancedSettings;

            if (_showAdvancedSettings)
            {
                GUILayout.BeginVertical();
                {
                    GUILayout.Space(5);
                    bool newHookMode = GUILayout.Toggle(Main.Settings.UseHookMode, LangMan.T("Toggle_HookMode"));
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(20);
                        GUILayout.Label($"<color=#888888>{LangMan.T("Desc_HookMode")}</color>");
                    }
                    GUILayout.EndHorizontal();

                    if (newHookMode != Main.Settings.UseHookMode)
                    {
                        Main.Settings.UseHookMode = newHookMode;
                        if (newHookMode) XPerfectBridge.TryInit(force: true);
                        else XPerfectBridge.UnloadHook();
                    }

                    XPerfectBridge.HookState currentState = XPerfectBridge.CurrentState;
                    string statusDisplayText;

                    switch (currentState)
                    {
                        case XPerfectBridge.HookState.Success:
                            statusDisplayText = $"<color=#55FF55>{LangMan.T("Status_HookSuccess")}</color>";
                            break;
                        case XPerfectBridge.HookState.Failed:
                            statusDisplayText = $"<color=#FF5555>{LangMan.T("Status_HookFailed")}{XPerfectBridge.LastErrorMessage}</color>";
                            break;
                        case XPerfectBridge.HookState.Disabled:
                        default:
                            statusDisplayText = $"<color=#888888>{LangMan.T("Status_HookDisabled")}</color>";
                            break;
                    }

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(20);
                        GUILayout.Label($"{LangMan.T("Label_CurrentStatus")}{statusDisplayText}");
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Space(5);

                    Main.Settings.DisplayCurrMode = GUILayout.Toggle(Main.Settings.DisplayCurrMode, LangMan.T("Toggle_DisplayCurrMode"));
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(20);
                        GUILayout.Label($"<color=#888888>{LangMan.T("Desc_DisplayCurrMode")}</color> <color=#FF96B4>#FF96B4</color>");
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Space(5);

                    Main.Settings.UseBinaryWriter = GUILayout.Toggle(Main.Settings.UseBinaryWriter, LangMan.T("Toggle_UseBinaryWriter"));
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(20);
                        GUILayout.Label($"<color=#888888>{LangMan.T("Desc_UseBinaryWriter")}</color>");
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Space(5);

                    bool previousGuiState = GUI.enabled;
                    if (Main.Settings.UseBinaryWriter)
                    {
                        GUI.enabled = false;
                    }

                    Main.Settings.UseOldJsonFormat = GUILayout.Toggle(Main.Settings.UseOldJsonFormat, LangMan.T("Toggle_UseOldJsonFormat"));
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(20);
                        GUILayout.Label($"<color=#888888>{LangMan.T("Desc_UseOldJsonFormat")}</color>");
                    }
                    GUILayout.EndHorizontal();

                    GUI.enabled = previousGuiState;

                    GUILayout.Space(5);

                    Main.Settings.AutoReloadInEditor = GUILayout.Toggle(Main.Settings.AutoReloadInEditor, LangMan.T("Toggle_AutoReloadInEditor"));
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(20);
                        GUILayout.Label($"<color=#888888>{LangMan.T("Desc_AutoReloadInEditor")}</color>");
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
        }


        
        #region utils

        private static void ToggleFold(string label, ref bool toggle, ref bool foldout)
        {
            GUILayout.BeginHorizontal();
            toggle = GUILayout.Toggle(toggle, label, GUILayout.ExpandWidth(false));

            if (toggle)
            {
                GUILayout.Space(10);
                string arrow = foldout ? "▲" : "▼";
                if (GUILayout.Button(arrow, GUILayout.Width(28), GUILayout.Height(18)))
                {
                    foldout = !foldout;
                }
            }
            GUILayout.EndHorizontal();
        }

        private static void SettingFold(string label, ref bool toggle, ref int precision, ref bool foldout)
        {
            GUILayout.BeginHorizontal();
            toggle = GUILayout.Toggle(toggle, label, GUILayout.ExpandWidth(false));

            if (toggle)
            {
                GUILayout.Space(10);
                string arrow = foldout ? "▲" : "▼";
                if (GUILayout.Button(arrow, GUILayout.Width(28), GUILayout.Height(18)))
                {
                    foldout = !foldout;
                }
            }
            GUILayout.EndHorizontal();

            if (toggle)
            {
                SliderInt("Label_Precision", ref precision, 0, 5);
            }
        }

        private static void SettingRow(string label, ref bool toggle, ref int precision)
        {
            toggle = GUILayout.Toggle(toggle, label);
            if (toggle)
            {
                SliderInt("Label_Precision", ref precision, 0, 5);
            }
        }

        private static void HUDBase(ref float x, ref float y, ref float scale, ref bool bold, ref int align, ref string format, ref int prec)
        {
            SliderFloat("Label_XOffset", ref x, -0.5f, 0.5f);
            SliderFloat("Label_YOffset", ref y, -0.5f, 0.5f);
            SliderFloat("Label_Scale", ref scale, 0.2f, 3.0f);
            Toggle(ref bold, "Toggle_Bold");
            AlignButtons(ref align);

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label(LangMan.T("Label_Format"), GUILayout.Width(100));
            format = GUILayout.TextField(format, GUILayout.Width(200));
            GUILayout.EndHorizontal();

            SliderInt("Label_Precision", ref prec, 0, 5);
        }

        private static void SliderFloat(string labelKey, ref float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label(LangMan.T(labelKey) + $"{value:F2}", GUILayout.Width(120));
            value = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(120));
            GUILayout.EndHorizontal();
        }

        private static void SliderInt(string labelKey, ref int value, int min, int max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label(LangMan.T(labelKey) + $"{value}", GUILayout.Width(120));
            value = Mathf.RoundToInt(GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(100)));
            GUILayout.EndHorizontal();
        }

        private static void Toggle(ref bool value, string labelKey, float indent = 20)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(indent);
            value = GUILayout.Toggle(value, LangMan.T(labelKey));
            GUILayout.EndHorizontal();
        }

        private static void ToggleNoIndent(ref bool value, string labelKey)
        {
            value = GUILayout.Toggle(value, LangMan.T(labelKey));
        }

        private static void IntField(string labelKey, ref string text, ref int value, int min, int max, int fallback, float labelWidth = 120)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label(LangMan.T(labelKey), GUILayout.Width(labelWidth));
            if (text == null) text = value.ToString();
            string newText = GUILayout.TextField(text, GUILayout.Width(80));
            if (newText != text)
            {
                if (int.TryParse(newText, out int parsed) && parsed >= min && parsed <= max)
                {
                    text = newText;
                    value = parsed;
                }
                else
                {
                    text = fallback.ToString();
                    value = fallback;
                }
            }
            GUILayout.EndHorizontal();
        }

        private static void AlignButtons(ref int align)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label(LangMan.T("Label_Align"), GUILayout.Width(100));

            string[] labels = { LangMan.T("Btn_Left"), LangMan.T("Btn_Center"), LangMan.T("Btn_Right") };
            for (int i = 0; i < 3; i++)
            {
                _activeButtonStyle.fontStyle = (align == i) ? FontStyle.Bold : FontStyle.Normal;
                if (GUILayout.Button(labels[i], _activeButtonStyle, GUILayout.Width(60)))
                    align = i;
            }
            GUILayout.EndHorizontal();
        }

        private static void ColorPicker(string label, ref Color color)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label(label, GUILayout.Width(100));

            GUILayout.Label("R", GUILayout.Width(15));
            color.r = GUILayout.HorizontalSlider(color.r, 0f, 1f, GUILayout.Width(50));
            GUILayout.Label("G", GUILayout.Width(15));
            color.g = GUILayout.HorizontalSlider(color.g, 0f, 1f, GUILayout.Width(50));
            GUILayout.Label("B", GUILayout.Width(15));
            color.b = GUILayout.HorizontalSlider(color.b, 0f, 1f, GUILayout.Width(50));
            GUILayout.Label("A", GUILayout.Width(15));
            color.a = GUILayout.HorizontalSlider(color.a, 0f, 1f, GUILayout.Width(50));

            GUILayout.EndHorizontal();
        }

        private static string AbsLogPath(string logDir)
        {
            if (string.IsNullOrWhiteSpace(logDir))
                return null;
            try
            {
                if (Path.IsPathRooted(logDir)) return Path.GetFullPath(logDir);
                string gameRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string fullPath = Path.GetFullPath(Path.Combine(gameRoot, logDir));
                return Path.GetFullPath(fullPath);
            }
            catch (Exception e)
            {
                Main.Logger.Log($"Err parsing path: {e.Message}");
                return logDir;
            }
        }

        #endregion
    }
}