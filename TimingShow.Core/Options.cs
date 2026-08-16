using System;
using System.IO;
using System.Diagnostics;
using UnityEngine;
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

        public static void OnGUI()
        {
            if (_activeButtonStyle == null)
                _activeButtonStyle = new GUIStyle(GUI.skin.button);
            
            DrawLanguageSettings();
            DrawTitleSettings();
            DrawPlanetSettings();
            DrawDeathAndWinSettings();
            DrawTimingHUD();
            DrawURHUD();
            DrawRatioHUD();
            DrawXACCGraphSettings();
            DrawLoggingSettings();
            DrawSessionControls();
            DrawAdvancedSettings();
        }


        private static void DrawLanguageSettings()
        {
            GUILayout.BeginHorizontal();
            foreach (string langCode in LangMan.AvailableLanguages)
            {
                _activeButtonStyle.fontStyle = (ModContext.Settings.Language == langCode) ? FontStyle.Bold : FontStyle.Normal;
                if (GUILayout.Button(langCode, _activeButtonStyle, GUILayout.Width(100)))
                    ModContext.Settings.Language = langCode;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
        }

        private static void DrawTitleSettings()
        {
            SettingFold(LangMan.T("Toggle_Title"), ref ModContext.Settings.ShowInSongTitle, ref ModContext.Settings.Perc1, ref _foldoutTitleSettings);
            if (ModContext.Settings.ShowInSongTitle && _foldoutTitleSettings)
            {
                Toggle(ref ModContext.Settings.Title_UseJudgeColor, "HUD_UseJudgeColor");
                if (ModContext.Settings.Title_UseJudgeColor)
                {
                    Toggle(ref ModContext.Settings.Title_EnableXPerfect, "Enable_XP", 40);
                }
                Toggle(ref ModContext.Settings.Title_ShowAngle, "Toggle_ShowAngle");
            }
        }

        private static void DrawPlanetSettings()
        {
            bool oldShowOnPlanet = ModContext.Settings.ShowOnPlanet;
            SettingFold(LangMan.T("Toggle_Planet"), ref ModContext.Settings.ShowOnPlanet, ref ModContext.Settings.Perc2, ref _foldoutPlanetSettings);

            if (oldShowOnPlanet != ModContext.Settings.ShowOnPlanet && ModContext.Settings.AutoReloadInEditor)
            {
                if (!ADOBase.isLevelEditor) return;
                ADOBase.RestartScene();
            }

            if (ModContext.Settings.ShowOnPlanet && _foldoutPlanetSettings)
            {
                Toggle(ref ModContext.Settings.Planet_ShowAngle, "Toggle_ShowAngle");
                Toggle(ref ModContext.Settings.Planet_EnableXPerfect, "Enable_XP");

                GUILayout.Label(LangMan.T("Setting_Title"));
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(20);
                    GUILayout.BeginVertical();
                    {
                        Toggle(ref ModContext.Settings.ReplaceFailOverload, "Toggle_FailOverload", 0);
                        Toggle(ref ModContext.Settings.ReplaceTooEarly, "Toggle_TooEarly", 0);
                        Toggle(ref ModContext.Settings.ReplaceVeryEarly, "Toggle_VeryEarly", 0);
                        Toggle(ref ModContext.Settings.ReplaceEarlyPerfect, "Toggle_EarlyPerfect", 0);
                        Toggle(ref ModContext.Settings.ReplacePerfect, "Toggle_Perfect", 0);
                        Toggle(ref ModContext.Settings.ReplaceLatePerfect, "Toggle_LatePerfect", 0);
                        Toggle(ref ModContext.Settings.ReplaceVeryLate, "Toggle_VeryLate", 0);
                        Toggle(ref ModContext.Settings.ReplaceTooLate, "Toggle_TooLate", 0);
                        Toggle(ref ModContext.Settings.ReplaceFailMiss, "Toggle_FailMiss", 0);
                        Toggle(ref ModContext.Settings.ReplaceMultipress, "Toggle_Multipress", 0);
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
            }
        }

        private static void DrawDeathAndWinSettings()
        {
            SettingRow(LangMan.T("Toggle_Death"), ref ModContext.Settings.ShowOnDeath, ref ModContext.Settings.Perc3);
            SettingRow(LangMan.T("Toggle_Win"), ref ModContext.Settings.ShowInWinPage, ref ModContext.Settings.Perc4);
        }

        private static void DrawTimingHUD()
        {
            ToggleFold(LangMan.T("Toggle_TimingHUD"), ref ModContext.Settings.ShowTimingHUD, ref _foldoutTimingHUD);
            if (ModContext.Settings.ShowTimingHUD && _foldoutTimingHUD)
            {
                HUDBase(
                    ref ModContext.Settings.HUD_x, ref ModContext.Settings.HUD_y, ref ModContext.Settings.HUD_scale,
                    ref ModContext.Settings.HUD_bold, ref ModContext.Settings.HUD_align, ref ModContext.Settings.HUD_Format,
                    ref ModContext.Settings.PercHUD
                );
                Toggle(ref ModContext.Settings.HUD_UseJudgeColor, "HUD_UseJudgeColor");
                if (ModContext.Settings.HUD_UseJudgeColor)
                {
                    Toggle(ref ModContext.Settings.HUD_EnableXPerfect, "Enable_XP", 40);
                }
                Toggle(ref ModContext.Settings.HUD_ShowAngle, "Toggle_ShowAngle");
            }
        }

        private static void DrawURHUD()
        {
            ToggleFold(LangMan.T("Toggle_URHUD"), ref ModContext.Settings.ShowURHUD, ref _foldoutURHUD);
            if (ModContext.Settings.ShowURHUD && _foldoutURHUD)
            {
                HUDBase(
                    ref ModContext.Settings.URHUD_x, ref ModContext.Settings.URHUD_y, ref ModContext.Settings.URHUD_scale,
                    ref ModContext.Settings.URHUD_bold, ref ModContext.Settings.URHUD_align, ref ModContext.Settings.URHUD_Format,
                    ref ModContext.Settings.PercURHUD
                );
            }
        }

        private static void DrawRatioHUD()
        {
            ToggleFold(LangMan.T("Toggle_RatioHUD"), ref ModContext.Settings.ShowRatioHUD, ref _foldoutRatioHUD);
            if (ModContext.Settings.ShowRatioHUD && _foldoutRatioHUD)
            {
                HUDBase(
                    ref ModContext.Settings.RatioHUD_x, ref ModContext.Settings.RatioHUD_y, ref ModContext.Settings.RatioHUD_scale,
                    ref ModContext.Settings.RatioHUD_bold, ref ModContext.Settings.RatioHUD_align, ref ModContext.Settings.RatioHUD_Format,
                    ref ModContext.Settings.PercRatioHUD
                );
                Toggle(ref ModContext.Settings.Ratio_UseXPerfect, "Enable_XP");
            }
        }

        private static void DrawXACCGraphSettings()
        {
            ToggleFold(LangMan.T("Toggle_XACCGraph"), ref ModContext.Settings.ShowXACCGraph, ref _foldoutXACCGraph);
            if (ModContext.Settings.ShowXACCGraph && _foldoutXACCGraph)
            {
                Toggle(ref ModContext.Settings.XACCGraph_ShowEnd, "Toggle_ShowEnd");
                SliderFloat("Label_XOffset", ref ModContext.Settings.XACCGraph_X, 0.0f, 1.0f);
                SliderFloat("Label_YOffset", ref ModContext.Settings.XACCGraph_Y, 0.0f, 1.0f);
                SliderFloat("Label_Scale", ref ModContext.Settings.XACCGraph_Scale, 0.2f, 3.0f);
                IntField("Label_MaxPoints", ref _maxPointsText, ref ModContext.Settings.XACCGraph_MaxPoints, 20, 5000, 250);

                ColorPicker(LangMan.T("Label_BgColor"), ref ModContext.Settings.XACCGraph_BgColor);
                ColorPicker(LangMan.T("Label_LineColor"), ref ModContext.Settings.XACCGraph_LineColor);
                ColorPicker(LangMan.T("Label_GridColor"), ref ModContext.Settings.XACCGraph_GridColor);
                ColorPicker(LangMan.T("Label_AxisTextColor"), ref ModContext.Settings.XACCGraph_AxisTextColor);
                ColorPicker(LangMan.T("Label_InfoTextColor"), ref ModContext.Settings.XACCGraph_ValueTextColor);
            }
        }

        private static void DrawLoggingSettings()
        {
            GUILayout.BeginVertical();
            {
                ToggleFold(LangMan.T("Toggle_Logging"), ref ModContext.Settings.EnableLogging, ref _foldoutLogging);

                if (ModContext.Settings.EnableLogging && _foldoutLogging)
                {
                    SliderInt("Label_Precision", ref ModContext.Settings.PercLog, 0, 5);
                    Toggle(ref ModContext.Settings.Logger_EnableXPerfect, "Enable_XP");
                    Toggle(ref ModContext.Settings.Logger_ShowAngle, "Toggle_ShowAngle");
                    Toggle(ref ModContext.Settings.LogAutoplay, "Toggle_LogAutoplay");
                    Toggle(ref ModContext.Settings.UseJsonWriter, "Toggle_UseJsonWriter");

                    // logdir
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    GUILayout.Label(LangMan.T("Label_LogDir"), GUILayout.Width(140));
                    string absolutePath = AbsLogPath(ModContext.Settings.LogDirectory);
                    string displayPath = string.IsNullOrWhiteSpace(absolutePath) ? "None" : absolutePath;
                    GUILayout.Label(displayPath, GUILayout.MinWidth(280), GUILayout.MaxWidth(480));
                    
                    if (GUILayout.Button(LangMan.T("Btn_Browse"), GUILayout.Width(70)))
                    {
                        string defaultDir = GetLogDirectory();
                        string selectedFolder = FileBrowser.PickFolder(defaultDir, "Folder", new string[0], LangMan.T("Label_LogDir"));
                        if (!string.IsNullOrEmpty(selectedFolder))
                        {
                            ModContext.Settings.LogDirectory = Path.GetFullPath(selectedFolder);
                        }
                    }
                    GUILayout.EndHorizontal();
                    
                    // lbl buffersize
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    GUILayout.Label(LangMan.T("Label_BufferSize"), GUILayout.Width(140));

                    if (_bufferSizeText == null) _bufferSizeText = ModContext.Settings.LogBufferSizeKB.ToString();
                    string newBufferSizeText = GUILayout.TextField(_bufferSizeText, GUILayout.Width(80));
                    if (newBufferSizeText != _bufferSizeText)
                    {
                        if (int.TryParse(newBufferSizeText, out int parsedVal) && parsedVal >= 8 && parsedVal <= 102400)
                        {
                            _bufferSizeText = newBufferSizeText;
                            ModContext.Settings.LogBufferSizeKB = parsedVal;
                        }
                        else
                        {
                            _bufferSizeText = "64";
                            ModContext.Settings.LogBufferSizeKB = 64;
                        }
                    }
                    GUILayout.EndHorizontal();
                }

                // btn openlogs
                GUILayout.Space(10);
                if (GUILayout.Button(LangMan.T("Btn_OpenLogs"), GUILayout.Width(150)))
                {
                    try
                    {
                        string logDir = GetLogDirectory();
                        if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
                        Process.Start(new ProcessStartInfo() { FileName = logDir, UseShellExecute = true, Verb = "open" });
                    }
                    catch (Exception e)
                    {
                        ModContext.Logger.Error(e.Message);
                    }
                }
            }
            GUILayout.EndVertical();
        }

        private static void DrawSessionControls()
        {
            if (GUILayout.Button(LangMan.T("Btn_Reset"), GUILayout.Width(150)))
            {
                ModContext.SessionOffsets.Clear();
                ModContext.LastHitMargin = HitMargin.Perfect;
                ModContext.LastTiming = 0;
                ModContext.LastAngle = 0;
            }
        }

        private static void DrawAdvancedSettings()
        {
            string foldoutArrow = _showAdvancedSettings ? "▲" : "▼";
            if (GUILayout.Button($"{LangMan.T("Btn_Advanced")} {foldoutArrow}", GUILayout.Width(150)))
                _showAdvancedSettings = !_showAdvancedSettings;

            if (!_showAdvancedSettings) return;

            GUILayout.BeginVertical();
            {
                GUILayout.Space(5);
                
                // hookmode
                bool newHookMode = ToggleWithDescription( ModContext.Settings.UseHookMode, "Toggle_HookMode", "Desc_HookMode" );
                if (newHookMode != ModContext.Settings.UseHookMode)
                {
                    ModContext.Settings.UseHookMode = newHookMode;
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
                IndentedLabel($"{LangMan.T("Label_CurrentStatus")}{statusDisplayText}");

                GUILayout.Space(5);
                
                // curmode
                ModContext.Settings.DisplayCurrMode = ToggleWithDescription(
                    ModContext.Settings.DisplayCurrMode,
                    "Toggle_DisplayCurrMode",
                    "Desc_DisplayCurrMode",
                    extraDescriptionHtml: " <color=#FF96B4>#FF96B4</color>"
                );

                GUILayout.Space(5);

                // oldfmt
                bool previousGuiState = GUI.enabled;
                if (!ModContext.Settings.UseJsonWriter)
                    GUI.enabled = false;
                ModContext.Settings.UseOldJsonFormat = ToggleWithDescription(
                    ModContext.Settings.UseOldJsonFormat,
                    "Toggle_UseOldJsonFormat",
                    "Desc_UseOldJsonFormat"
                );
                GUI.enabled = previousGuiState;

                GUILayout.Space(5);
                
                //autoreload
                ModContext.Settings.AutoReloadInEditor = ToggleWithDescription(
                    ModContext.Settings.AutoReloadInEditor,
                    "Toggle_AutoReloadInEditor",
                    "Desc_AutoReloadInEditor"
                );
            }
            GUILayout.EndVertical();
        }
        

        #region UI Components

        private static void FoldoutToggle(string label, ref bool toggle, ref bool foldout)
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

        private static void ToggleFold(string label, ref bool toggle, ref bool foldout)
        {
            FoldoutToggle(label, ref toggle, ref foldout);
        }

        private static void SettingFold(string label, ref bool toggle, ref int precision, ref bool foldout)
        {
            FoldoutToggle(label, ref toggle, ref foldout);

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

        private static bool ToggleWithDescription(bool value, string labelKey, string descKey, string extraDescriptionHtml = "", float indent = 20)
        {
            bool newValue = GUILayout.Toggle(value, LangMan.T(labelKey));
            IndentedLabel($"<color=#888888>{LangMan.T(descKey)}</color>{extraDescriptionHtml}", indent);
            return newValue;
        }

        private static void IndentedLabel(string text, float indent = 20)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(indent);
            GUILayout.Label(text);
            GUILayout.EndHorizontal();
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
            if (indent > 0) GUILayout.Space(indent);
            value = GUILayout.Toggle(value, LangMan.T(labelKey));
            GUILayout.EndHorizontal();
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

        #endregion

        #region Utils

        private static string GetDefaultLogDirectory()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "../Mods/TimingShow/Logs"));
        }

        private static string GetLogDirectory()
        {
            string abs = AbsLogPath(ModContext.Settings.LogDirectory);
            return string.IsNullOrWhiteSpace(abs) ? GetDefaultLogDirectory() : abs;
        }

        private static string AbsLogPath(string logDir)
        {
            if (string.IsNullOrWhiteSpace(logDir))
                return null;
            try
            {
                if (Path.IsPathRooted(logDir)) return Path.GetFullPath(logDir);
                string gameRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                return Path.GetFullPath(Path.Combine(gameRoot, logDir));
            }
            catch (Exception e)
            {
                ModContext.Logger.Log($"Err parsing path: {e.Message}");
                return logDir;
            }
        }

        #endregion
    }
}