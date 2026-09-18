using System;
using System.IO;
using System.Diagnostics;
using UnityEngine;
using UnityFileDialog;
using static TimingShow.OptionsWidgets;

namespace TimingShow
{
    public static class Options
    {
        private static string _bufferSizeText;
        private static string _maxPointsText;
        private static string _analyzerPortText;
        private static string _analyzerTimeoutText;
        private static bool _showAdvancedSettings;

        private static bool _foldoutTitleSettings;
        private static bool _foldoutPlanetSettings;
        private static bool _foldoutReplaceSettings;
        private static bool _foldoutDeathSettings; 
        private static bool _foldoutWinSettings;
        private static bool _foldoutTimingHUD;
        private static bool _foldoutURHUD;
        private static bool _foldoutRatioHUD;
        private static bool _foldoutLogging;
        private static bool _foldoutXACCGraph;

        public static void OnGUI()
        {
            if (!ModContext.IsConfigOpen)
                OptionLogList.OnConfigOpened();

            ModContext.LastConfigGuiFrame = Time.frameCount;
            ModContext.UIDirty = true;

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
            OptionLogList.Draw();
            DrawAdvancedSettings();
        }


        private static void DrawLanguageSettings()
        {
            GUILayout.BeginHorizontal();
            foreach (string langCode in i18n.AvailableLanguages)
            {
                if (ActiveButton(langCode, ModContext.Settings.Language == langCode, 100))
                    ModContext.Settings.Language = langCode;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
        }

        private static void DrawTitleSettings()
        {
            FoldoutToggle(i18n.T("Toggle_Title"), ref ModContext.Settings.ShowInSongTitle, ref _foldoutTitleSettings);
            if (ModContext.Settings.ShowInSongTitle && _foldoutTitleSettings)
            {
                SliderInt("Label_Precision", ref ModContext.Settings.Perc1, 0, 5);
                SliderInt("Label_FontSize", ref ModContext.Settings.Title_FontSize, 20, 200);
                Toggle(ref ModContext.Settings.Title_UseJudgeColor, "HUD_UseJudgeColor");
                if (ModContext.Settings.Title_UseJudgeColor)
                {
                    if (!HitMarginCompat.IsGame34)
                        Toggle(ref ModContext.Settings.Title_EnableXPerfect, "Enable_XP", 40);
                }
                Toggle(ref ModContext.Settings.Title_ShowAngle, "Toggle_ShowAngle");
            }
        }

        private static void DrawPlanetSettings()
        {
            bool oldShowOnPlanet = ModContext.Settings.ShowOnPlanet;
            FoldoutToggle(i18n.T("Toggle_Planet"), ref ModContext.Settings.ShowOnPlanet, ref _foldoutPlanetSettings);
            
            if (oldShowOnPlanet != ModContext.Settings.ShowOnPlanet && ModContext.Settings.AutoReloadInEditor)
            {
                if (!ADOBase.isLevelEditor) return;
                ADOBase.RestartScene();
            }

            if (ModContext.Settings.ShowOnPlanet && _foldoutPlanetSettings)
            {
                SliderInt("Label_Precision", ref ModContext.Settings.Perc3, 0, 5);
                SliderInt("Label_FontSize", ref ModContext.Settings.Planet_FontSize, 20, 200);
                Toggle(ref ModContext.Settings.Planet_ShowAngle, "Toggle_ShowAngle");
                if (!HitMarginCompat.IsGame34)
                    Toggle(ref ModContext.Settings.Planet_EnableXPerfect, "Enable_XP");

                string replaceArrow = _foldoutReplaceSettings ? "▲" : "▼";
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(20);
                    if (GUILayout.Button($"{i18n.T("Setting_Title")} {replaceArrow}", GUILayout.ExpandWidth(false)))
                        _foldoutReplaceSettings = !_foldoutReplaceSettings;
                }
                GUILayout.EndHorizontal();

                if (!_foldoutReplaceSettings) return;

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(20);
                    GUILayout.BeginVertical();
                    {
                        Toggle(ref ModContext.Settings.ReplaceFailOverload, "Toggle_FailOverload", 0);
                        Toggle(ref ModContext.Settings.ReplaceTooEarly, "Toggle_TooEarly", 0);
                        Toggle(ref ModContext.Settings.ReplaceVeryEarly, "Toggle_VeryEarly", 0);
                        Toggle(ref ModContext.Settings.ReplaceEarlyPerfect, "Toggle_EarlyPerfect", 0);


                        if (HitMarginCompat.IsGame34)
                            Toggle(ref ModContext.Settings.ReplacePerfectMinus, "Toggle_PerfectMinus", 0);
                        else
                            Toggle(ref ModContext.Settings.ReplacePerfect, "Toggle_Perfect", 0);
                        
                        if (HitMarginCompat.HasNativeXPerfect)
                        {
                            Toggle(ref ModContext.Settings.ReplaceXPerfect, "Toggle_XPerfect", 0);
                            Toggle(ref ModContext.Settings.ReplacePerfectPlus, "Toggle_PerfectPlus", 0);
                        }

                        Toggle(ref ModContext.Settings.ReplaceLatePerfect, "Toggle_LatePerfect", 0);
                        Toggle(ref ModContext.Settings.ReplaceVeryLate, "Toggle_VeryLate", 0);
                        Toggle(ref ModContext.Settings.ReplaceTooLate, "Toggle_TooLate", 0);
                        Toggle(ref ModContext.Settings.ReplaceFailMiss, "Toggle_FailMiss", 0);
                        Toggle(ref ModContext.Settings.ReplaceMultipress, "Toggle_Multipress", 0);
                        Toggle(ref ModContext.Settings.ReplaceOverPress, "Toggle_OverPress", 0);
                        Toggle(ref ModContext.Settings.ReplaceAuto, "Toggle_Auto", 0);
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
            }
        }

        private static void DrawDeathAndWinSettings()
        {
            FoldoutToggle(i18n.T("Toggle_Death"), ref ModContext.Settings.ShowOnDeath, ref _foldoutDeathSettings);
            if (ModContext.Settings.ShowOnDeath && _foldoutDeathSettings)
            {
                SliderInt("Label_Precision", ref ModContext.Settings.Perc3, 0, 5);
                Toggle(ref ModContext.Settings.ShowOnDeath_ShowAvgTiming, "Toggle_Death_AvgTiming");
                Toggle(ref ModContext.Settings.ShowOnDeath_ShowUR, "Toggle_Death_UR");
                Toggle(ref ModContext.Settings.ShowOnDeath_ShowXACC, "Toggle_Death_XACC");
                Toggle(ref ModContext.Settings.ShowOnDeath_ShowRatio, "Toggle_Death_Ratio");
                SliderInt("Label_FontSize", ref ModContext.Settings.ShowOnDeath_FontSize, 20, 200);
            }
            
            FoldoutToggle(i18n.T("Toggle_Win"), ref ModContext.Settings.ShowInWinPage, ref _foldoutWinSettings);
            if (ModContext.Settings.ShowInWinPage && _foldoutWinSettings)
            {
                SliderInt("Label_Precision", ref ModContext.Settings.Perc4, 0, 5);
                Toggle(ref ModContext.Settings.ShowInWinPage_ShowAvgTiming, "Toggle_Death_AvgTiming");
                Toggle(ref ModContext.Settings.ShowInWinPage_ShowUR, "Toggle_Death_UR");
                Toggle(ref ModContext.Settings.ShowInWinPage_ShowRatio, "Toggle_Death_Ratio");
                SliderInt("Label_FontSize", ref ModContext.Settings.ShowInWinPage_FontSize, 20, 200);
            }
        }

        private static void DrawTimingHUD()
        {
            ToggleFold(i18n.T("Toggle_TimingHUD"), ref ModContext.Settings.ShowTimingHUD, ref _foldoutTimingHUD);
            if (ModContext.Settings.ShowTimingHUD && _foldoutTimingHUD)
            {
                HUDBase(
                    ref ModContext.Settings.HUD_x, ref ModContext.Settings.HUD_y, ref ModContext.Settings.HUD_scale,
                    ref ModContext.Settings.HUD_bold, ref ModContext.Settings.HUD_align, ref ModContext.Settings.HUD_Format,
                    ref ModContext.Settings.PercHUD
                );
                DrawHUDFontSettings(ref ModContext.Settings.HUD_UseCustomFont, ref ModContext.Settings.HUD_FontPath);
                Toggle(ref ModContext.Settings.HUD_UseJudgeColor, "HUD_UseJudgeColor");
                if (ModContext.Settings.HUD_UseJudgeColor)
                {
                    if (!HitMarginCompat.IsGame34)
                        Toggle(ref ModContext.Settings.HUD_EnableXPerfect, "Enable_XP", 40);
                }
                Toggle(ref ModContext.Settings.HUD_ShowAngle, "Toggle_ShowAngle");
            }
        }

        private static void DrawURHUD()
        {
            ToggleFold(i18n.T("Toggle_URHUD"), ref ModContext.Settings.ShowURHUD, ref _foldoutURHUD);
            if (ModContext.Settings.ShowURHUD && _foldoutURHUD)
            {
                HUDBase(
                    ref ModContext.Settings.URHUD_x, ref ModContext.Settings.URHUD_y, ref ModContext.Settings.URHUD_scale,
                    ref ModContext.Settings.URHUD_bold, ref ModContext.Settings.URHUD_align, ref ModContext.Settings.URHUD_Format,
                    ref ModContext.Settings.PercURHUD
                );
                DrawHUDFontSettings(ref ModContext.Settings.URHUD_UseCustomFont, ref ModContext.Settings.URHUD_FontPath);
            }
        }

        private static void DrawRatioHUD()
        {
            ToggleFold(i18n.T("Toggle_RatioHUD"), ref ModContext.Settings.ShowRatioHUD, ref _foldoutRatioHUD);
            if (ModContext.Settings.ShowRatioHUD && _foldoutRatioHUD)
            {
                HUDBase(
                    ref ModContext.Settings.RatioHUD_x, ref ModContext.Settings.RatioHUD_y, ref ModContext.Settings.RatioHUD_scale,
                    ref ModContext.Settings.RatioHUD_bold, ref ModContext.Settings.RatioHUD_align, ref ModContext.Settings.RatioHUD_Format,
                    ref ModContext.Settings.PercRatioHUD
                );
                DrawHUDFontSettings(ref ModContext.Settings.RatioHUD_UseCustomFont, ref ModContext.Settings.RatioHUD_FontPath);
                RatioModeButtons();
            }
        }

        private static void DrawHUDFontSettings(ref bool useCustomFont, ref string fontPath)
        {
            Toggle(ref useCustomFont, "Toggle_CustomFont");

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label(i18n.T("Label_FontPath"), GUILayout.Width(100));
            string displayPath = string.IsNullOrWhiteSpace(fontPath) ? i18n.T("Label_GameFont") : fontPath;
            GUILayout.Label(displayPath, GUILayout.MinWidth(240), GUILayout.MaxWidth(450));

            if (GUILayout.Button(i18n.T("Btn_SelectFont"), GUILayout.Width(80)))
            {
                string initialDirectory = string.IsNullOrWhiteSpace(fontPath) ? "" : Path.GetDirectoryName(fontPath);
                string selectedFont = FileBrowser.PickFile(
                    initialDirectory,
                    "Font",
                    new[] { "ttf", "otf" },
                    i18n.T("Btn_SelectFont"));

                if (!string.IsNullOrWhiteSpace(selectedFont))
                {
                    string extension = Path.GetExtension(selectedFont);
                    if (string.Equals(extension, ".ttf", System.StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(extension, ".otf", System.StringComparison.OrdinalIgnoreCase))
                    {
                        fontPath = Path.GetFullPath(selectedFont);
                        useCustomFont = true;
                    }
                    else
                    {
                        ModContext.Logger?.Log("Unsupported HUD font file: " + selectedFont);
                    }
                }
            }
            GUILayout.EndHorizontal();
        }

        private static void DrawXACCGraphSettings()
        {
            ToggleFold(i18n.T("Toggle_XACCGraph"), ref ModContext.Settings.ShowXACCGraph, ref _foldoutXACCGraph);
            if (ModContext.Settings.ShowXACCGraph && _foldoutXACCGraph)
            {
                Toggle(ref ModContext.Settings.XACCGraph_ShowEnd, "Toggle_ShowEnd");
                SliderFloat("Label_XOffset", ref ModContext.Settings.XACCGraph_X, 0.0f, 1.0f);
                SliderFloat("Label_YOffset", ref ModContext.Settings.XACCGraph_Y, 0.0f, 1.0f);
                SliderFloat("Label_Scale", ref ModContext.Settings.XACCGraph_Scale, 0.2f, 3.0f);
                IntField("Label_MaxPoints", ref _maxPointsText, ref ModContext.Settings.XACCGraph_MaxPoints, 20, 5000, 250);

                ColorPicker(i18n.T("Label_BgColor"), ref ModContext.Settings.XACCGraph_BgColor);
                ColorPicker(i18n.T("Label_LineColor"), ref ModContext.Settings.XACCGraph_LineColor);
                ColorPicker(i18n.T("Label_GridColor"), ref ModContext.Settings.XACCGraph_GridColor);
                ColorPicker(i18n.T("Label_AxisTextColor"), ref ModContext.Settings.XACCGraph_AxisTextColor);
                ColorPicker(i18n.T("Label_InfoTextColor"), ref ModContext.Settings.XACCGraph_ValueTextColor);
            }
        }

        private static void DrawLoggingSettings()
        {
            GUILayout.BeginVertical();
            {
                ToggleFold(i18n.T("Toggle_Logging"), ref ModContext.Settings.EnableLogging, ref _foldoutLogging);

                if (ModContext.Settings.EnableLogging && _foldoutLogging)
                {
                    SliderInt("Label_Precision", ref ModContext.Settings.PercLog, 0, 5);
                    if (!HitMarginCompat.IsGame34)
                        Toggle(ref ModContext.Settings.Logger_EnableXPerfect, "Enable_XP");
                    Toggle(ref ModContext.Settings.Logger_ShowAngle, "Toggle_ShowAngle");
                    Toggle(ref ModContext.Settings.LogAutoplay, "Toggle_LogAutoplay");
                    Toggle(ref ModContext.Settings.UseJsonWriter, "Toggle_UseJsonWriter");

                    // logdir
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    GUILayout.Label(i18n.T("Label_LogDir"), GUILayout.Width(140));
                    string absolutePath = OptionLogList.AbsLogPath(ModContext.Settings.LogDirectory);
                    string displayPath = string.IsNullOrWhiteSpace(absolutePath) ? "None" : absolutePath;
                    GUILayout.Label(displayPath, GUILayout.MinWidth(280), GUILayout.MaxWidth(480));
                    
                    if (GUILayout.Button(i18n.T("Btn_Browse"), GUILayout.Width(70)))
                    {
                        string defaultDir = OptionLogList.GetLogDirectory();
                        string selectedFolder = FileBrowser.PickFolder(defaultDir, "Folder", new string[0], i18n.T("Label_LogDir"));
                        if (!string.IsNullOrEmpty(selectedFolder))
                        {
                            ModContext.Settings.LogDirectory = Path.GetFullPath(selectedFolder);
                        }
                    }
                    GUILayout.EndHorizontal();
                    
                    // lbl buffersize
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    GUILayout.Label(i18n.T("Label_BufferSize"), GUILayout.Width(140));

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
                if (GUILayout.Button(i18n.T("Btn_OpenLogs"), GUILayout.Width(150)))
                {
                    try
                    {
                        string logDir = OptionLogList.GetLogDirectory();
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
            if (GUILayout.Button(i18n.T("Btn_Reset"), GUILayout.Width(150)))
            {
                ModContext.SessionOffsets.Clear();
                ModContext.ResetJudgeState();
                ModContext.LastTiming = 0;
                ModContext.LastAngle = 0;
                Patches.TimingCalcPatches.MarginTrackerAddHitPatch.ResetCounts();
            }
        }

        private static void DrawAdvancedSettings()
        {
            string foldoutArrow = _showAdvancedSettings ? "▲" : "▼";
            if (GUILayout.Button($"{i18n.T("Btn_Advanced")} {foldoutArrow}", GUILayout.Width(150)))
                _showAdvancedSettings = !_showAdvancedSettings;

            if (!_showAdvancedSettings) return;

            GUILayout.BeginVertical();
            {
                GUILayout.Space(5);
                
                // hookmode
                XPerfectBridge.HookState currentState = XPerfectBridge.CurrentState;
                string statusDisplayText;
                switch (currentState)
                {
                    case XPerfectBridge.HookState.Success:
                        statusDisplayText = $"<color=#55FF55> ({i18n.T("Status_HookSuccess")})</color>";
                        break;
                    case XPerfectBridge.HookState.Failed:
                        statusDisplayText = $"<color=#FF5555> ({i18n.T("Status_HookFailed")}{XPerfectBridge.LastErrorMessage})</color>";
                        break;
                    case XPerfectBridge.HookState.NotApplicable:
                        statusDisplayText = $"<color=#55CCFF> ({i18n.T("Status_HookNotApplicable")})</color>";
                        break;
                    case XPerfectBridge.HookState.Disabled:
                    default:
                        statusDisplayText = string.Empty;
                        break;
                }
                
                bool hookSupported = XPerfectBridge.IsSupported;
                bool prevGuiEnabled = GUI.enabled;
                GUI.enabled = hookSupported;

                bool newHookMode = ToggleWithDescription(
                    ModContext.Settings.UseHookMode,
                    "Toggle_HookMode",
                    "Desc_HookMode",
                    extraLabelHtml: statusDisplayText
                );

                GUI.enabled = prevGuiEnabled;

                if (hookSupported && newHookMode != ModContext.Settings.UseHookMode)
                {
                    ModContext.Settings.UseHookMode = newHookMode;
                    if (newHookMode) XPerfectBridge.TryInit(force: true);
                    else XPerfectBridge.UnloadHook();
                }

                GUILayout.Space(5);

                //autoreload
                ModContext.Settings.AutoReloadInEditor = ToggleWithDescription(
                    ModContext.Settings.AutoReloadInEditor,
                    "Toggle_AutoReloadInEditor",
                    "Desc_AutoReloadInEditor"
                );

                GUILayout.Space(5);

                bool previousAnalyzerEnabled = ModContext.Settings.AnalyzerBridgeEnabled;
                bool analyzerEnabled = ToggleWithDescription(
                    ModContext.Settings.AnalyzerBridgeEnabled,
                    "Toggle_AnalyzerBridge",
                    "Desc_AnalyzerBridge"
                );
                ModContext.Settings.AnalyzerBridgeEnabled = analyzerEnabled;
                if (previousAnalyzerEnabled && !analyzerEnabled)
                    LogAnalyzerBridge.Stop();

                if (analyzerEnabled)
                {
                    IntField("Label_AnalyzerPort", ref _analyzerPortText, ref ModContext.Settings.AnalyzerBridgePort, 0, 65535, 0, 160);
                    IntField("Label_AnalyzerTimeout", ref _analyzerTimeoutText, ref ModContext.Settings.AnalyzerBridgeTimeoutSec,
                        LogAnalyzerBridge.MinTimeoutSeconds, LogAnalyzerBridge.MaxTimeoutSeconds, LogAnalyzerBridge.DefaultTimeoutSeconds, 160);
                }
            }
            GUILayout.EndVertical();
        }
    }
}
