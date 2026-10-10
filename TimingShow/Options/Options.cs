using System;
using System.IO;
using System.Diagnostics;
using UnityEngine;
using UnityFileDialog;
using static TimingShow.Options.OptionsWidgets;
using TimingShow.Bridge;
using TimingShow.HUD;

namespace TimingShow.Options
{
    public static class Options
    {
        private static string _bufferSizeText;
        private static string _maxPointsText;
        private static string _analyzerPortText;
        private static string _analyzerTimeoutText;
        private static string _scatterSampleCountText;
        private static string _scatterMaxPointsText;
        private static string _logGraphMaxPointsText;
        private static bool _showAdvancedSettings;
        private static bool _showDebugSettings;
        private static bool _showLogGraphSettings;

        private static bool _foldoutTitleSettings;
        private static bool _foldoutPlanetSettings;
        private static bool _foldoutReplaceSettings;
        private static bool _foldoutDeathSettings; 
        private static bool _foldoutWinSettings;
        private static bool _foldoutTimingHUD;
        private static bool _foldoutAvgHUD;
        private static bool _foldoutURHUD;
        private static bool _foldoutRatioHUD;
        private static bool _foldoutLogging;
        private static bool _foldoutXACCGraph;
        private static bool _foldoutTimingScatter;

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
            DrawAvgHUD();
            DrawURHUD();
            DrawRatioHUD();
            DrawXACCGraphSettings();
            DrawTimingScatterSettings();
            DrawLoggingSettings();
            DrawSessionControls();
            OptionLogList.Draw();
            DrawLogGraphSettings();
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
                Toggle(ref ModContext.Settings.Title_Bump, "Toggle_Bump");
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
                Toggle(ref ModContext.Settings.HUD_Bump, "Toggle_Bump");
            }
        }

        private static void DrawAvgHUD()
        {
            ToggleFold(i18n.T("Toggle_AvgHUD"), ref ModContext.Settings.ShowAvgHUD, ref _foldoutAvgHUD);
            if (ModContext.Settings.ShowAvgHUD && _foldoutAvgHUD)
            {
                HUDBase(
                    ref ModContext.Settings.AvgHUD_x, ref ModContext.Settings.AvgHUD_y, ref ModContext.Settings.AvgHUD_scale,
                    ref ModContext.Settings.AvgHUD_bold, ref ModContext.Settings.AvgHUD_align, ref ModContext.Settings.AvgHUD_Format,
                    ref ModContext.Settings.PercAvgHUD
                );
                DrawHUDFontSettings(ref ModContext.Settings.AvgHUD_UseCustomFont, ref ModContext.Settings.AvgHUD_FontPath);
                Toggle(ref ModContext.Settings.AvgHUD_Bump, "Toggle_Bump");
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
                Toggle(ref ModContext.Settings.URHUD_Bump, "Toggle_Bump");
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
                Toggle(ref ModContext.Settings.RatioHUD_Bump, "Toggle_Bump");
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
                Toggle(ref ModContext.Settings.XACCGraph_ShowPerfInfo, "Toggle_ShowPerfInfo");
                SliderFloat("Label_XOffset", ref ModContext.Settings.XACCGraph_X, 0.0f, 1.0f);
                SliderFloat("Label_YOffset", ref ModContext.Settings.XACCGraph_Y, 0.0f, 1.0f);
                SliderFloat("Label_GraphWidth", ref ModContext.Settings.XACCGraph_Width, 80f, 1200f);
                SliderFloat("Label_GraphHeight", ref ModContext.Settings.XACCGraph_Height, 40f, 600f);
                SliderFloat("Label_Scale", ref ModContext.Settings.XACCGraph_Scale, 0.2f, 3.0f);
                IntField("Label_MaxPoints", ref _maxPointsText, ref ModContext.Settings.XACCGraph_MaxPoints, 20, 5000, 250);

                ColorPicker(i18n.T("Label_BgColor"), ref ModContext.Settings.XACCGraph_BgColor);
                ColorPicker(i18n.T("Label_LineColor"), ref ModContext.Settings.XACCGraph_LineColor);
                ColorPicker(i18n.T("Label_GridColor"), ref ModContext.Settings.XACCGraph_GridColor);
                ColorPicker(i18n.T("Label_AxisTextColor"), ref ModContext.Settings.XACCGraph_AxisTextColor);
                ColorPicker(i18n.T("Label_InfoTextColor"), ref ModContext.Settings.XACCGraph_ValueTextColor);

                DrawHUDFontSettings(ref ModContext.Settings.XACCGraph_UseCustomFont, ref ModContext.Settings.XACCGraph_FontPath);
            }
        }

        private static void DrawTimingScatterSettings()
        {
            ToggleFold(i18n.T("Toggle_TimingScatter"), ref ModContext.Settings.ShowTimingScatter, ref _foldoutTimingScatter);
            if (ModContext.Settings.ShowTimingScatter && _foldoutTimingScatter)
            {
                Toggle(ref ModContext.Settings.TimingScatter_ShowEnd, "Toggle_ShowEnd");
                Toggle(ref ModContext.Settings.TimingScatter_IgnoreOutliers, "Toggle_IgnoreOutliers");
                Toggle(ref ModContext.Settings.TimingScatter_ShowPerfInfo, "Toggle_ShowPerfInfo");
                SliderFloat("Label_XOffset", ref ModContext.Settings.TimingScatter_X, 0.0f, 1.0f);
                SliderFloat("Label_YOffset", ref ModContext.Settings.TimingScatter_Y, 0.0f, 1.0f);
                SliderFloat("Label_GraphWidth", ref ModContext.Settings.TimingScatter_Width, 80f, 1200f);
                SliderFloat("Label_GraphHeight", ref ModContext.Settings.TimingScatter_Height, 40f, 600f);
                SliderFloat("Label_Scale", ref ModContext.Settings.TimingScatter_Scale, 0.2f, 3.0f);
                IntField("Label_SampleCount", ref _scatterSampleCountText, ref ModContext.Settings.TimingScatter_SampleCount, 10, 20000, 200);
                IntField("Label_MaxRenderPoints", ref _scatterMaxPointsText, ref ModContext.Settings.TimingScatter_MaxRenderPoints, 20, 8000, 800);
                SliderFloat("Label_PointSize", ref ModContext.Settings.TimingScatter_PointSize, 1.0f, 12.0f);

                Toggle(ref ModContext.Settings.TimingScatter_UseHitAxis, "Toggle_UseHitAxis");
                if (!ModContext.Settings.TimingScatter_UseHitAxis) Toggle(ref ModContext.Settings.TimingScatter_AutoScroll, "Toggle_AutoScroll");
                Toggle(ref ModContext.Settings.TimingScatter_UseJudgeColor, "Toggle_UseJudgeColor");
                Toggle(ref ModContext.Settings.TimingScatter_ShowZeroLine, "Toggle_ShowZeroLine");
                Toggle(ref ModContext.Settings.TimingScatter_ShowAvgLine, "Toggle_ShowAvgLine");
                Toggle(ref ModContext.Settings.TimingScatter_ShowJudgeBands, "Toggle_ShowJudgeBands");
                if (ModContext.Settings.TimingScatter_ShowJudgeBands)
                {
                    Toggle(ref ModContext.Settings.TimingScatter_BandAutoWindow, "Toggle_BandAutoWindow");
                    if (!ModContext.Settings.TimingScatter_BandAutoWindow)
                        SliderFloat("Label_BandThresholdBpm", ref ModContext.Settings.TimingScatter_BandThresholdBpm, 100f, 600f);

                    ColorPicker(i18n.T("Label_BandPerfectColor"), ref ModContext.Settings.TimingScatter_BandPerfectColor);
                    ColorPicker(i18n.T("Label_BandElPerfectColor"), ref ModContext.Settings.TimingScatter_BandElPerfectColor);
                    ColorPicker(i18n.T("Label_BandEarlyLateColor"), ref ModContext.Settings.TimingScatter_BandEarlyLateColor);
                }

                Toggle(ref ModContext.Settings.TimingScatter_ShowXpBand, "Toggle_ShowXpBand");
                if (ModContext.Settings.TimingScatter_ShowXpBand)
                {
                    ColorPicker(i18n.T("Label_BandXpColor"), ref ModContext.Settings.TimingScatter_BandXpColor);
                }

                ColorPicker(i18n.T("Label_BgColor"), ref ModContext.Settings.TimingScatter_BgColor);
                ColorPicker(i18n.T("Label_GridColor"), ref ModContext.Settings.TimingScatter_GridColor);
                ColorPicker(i18n.T("Label_PointColor"), ref ModContext.Settings.TimingScatter_PointColor);
                ColorPicker(i18n.T("Label_ZeroLineColor"), ref ModContext.Settings.TimingScatter_ZeroLineColor);
                ColorPicker(i18n.T("Label_AvgLineColor"), ref ModContext.Settings.TimingScatter_AvgLineColor);
                ColorPicker(i18n.T("Label_AxisTextColor"), ref ModContext.Settings.TimingScatter_AxisTextColor);

                DrawHUDFontSettings(ref ModContext.Settings.TimingScatter_UseCustomFont, ref ModContext.Settings.TimingScatter_FontPath);
            }
        }
        
        private static void DrawLogGraphSettings()
        {
            string foldoutArrow = _showLogGraphSettings ? "▲" : "▼";
            if (GUILayout.Button($"{i18n.T("Toggle_LogGraph")} {foldoutArrow}", GUILayout.Width(150)))
                _showLogGraphSettings = !_showLogGraphSettings;

            if (!_showLogGraphSettings) return;

            GUILayout.BeginVertical();
            {
                GUILayout.Space(5);
                SliderFloat("LogGraph_UiScale", ref ModContext.Settings.LogGraph_UIScale, 0.75f, 3.0f);
                Toggle(ref ModContext.Settings.LogGraph_IgnoreOutliers, "Toggle_IgnoreOutliers");
                Toggle(ref ModContext.Settings.LogGraph_UseHitAxis, "Toggle_UseHitAxis");
                Toggle(ref ModContext.Settings.LogGraph_UseJudgeColor, "Toggle_UseJudgeColor");
                Toggle(ref ModContext.Settings.LogGraph_ShowZeroLine, "Toggle_ShowZeroLine");
                Toggle(ref ModContext.Settings.LogGraph_ShowAvgLine, "Toggle_ShowAvgLine");
                IntField("Label_MaxRenderPoints", ref _logGraphMaxPointsText, ref ModContext.Settings.LogGraph_MaxRenderPoints, 20, 8000, 800);
                SliderFloat("Label_PointSize", ref ModContext.Settings.LogGraph_PointSize, 1.0f, 12.0f);

                ColorPicker(i18n.T("Label_BgColor"), ref ModContext.Settings.LogGraph_BgColor);
                ColorPicker(i18n.T("Label_GridColor"), ref ModContext.Settings.LogGraph_GridColor);
                ColorPicker(i18n.T("Label_PointColor"), ref ModContext.Settings.LogGraph_PointColor);
                ColorPicker(i18n.T("Label_ZeroLineColor"), ref ModContext.Settings.LogGraph_ZeroLineColor);
                ColorPicker(i18n.T("Label_AvgLineColor"), ref ModContext.Settings.LogGraph_AvgLineColor);
                ColorPicker(i18n.T("Label_AxisTextColor"), ref ModContext.Settings.LogGraph_AxisTextColor);

                DrawHUDFontSettings(ref ModContext.Settings.LogGraph_UseCustomFont, ref ModContext.Settings.LogGraph_FontPath);
            }
            GUILayout.EndVertical();
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
                CalcUR.Reset();
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
                GUILayout.Space(8);

                DrawDebugSettings();
            }
            GUILayout.EndVertical();
        }

        private static void DrawDebugSettings()
        {
            string debugFoldoutArrow = _showDebugSettings ? "▲" : "▼";
            if (GUILayout.Button($"{i18n.T("Btn_Debug")} {debugFoldoutArrow}", GUILayout.Width(150)))
                _showDebugSettings = !_showDebugSettings;

            IndentedLabel(i18n.T("Desc_DebugWarning"));

            if (!_showDebugSettings) return;

            GUILayout.Space(5);

            Toggle(ref ModContext.Settings.Diag_GraphBounds, "Toggle_DiagGraphBounds");
            Toggle(ref ModContext.Settings.Diag_InputTrace, "Toggle_DiagInputTrace");
            Toggle(ref ModContext.Settings.Diag_TrackMeshBounds, "Toggle_DiagTrackMesh");

            SliderFloat("Label_DiagBoundsInterval", ref ModContext.Settings.Diag_BoundsIntervalSec, 0.1f, 5f);
            SliderFloat("Label_DiagBoundsTolerance", ref ModContext.Settings.Diag_BoundsTolerancePx, 0f, 4f);
            SliderInt("Label_DiagBoundsLogLimit", ref ModContext.Settings.Diag_BoundsLogLimit, 1, 200);
            SliderFloat("Label_DiagBoundsRepeat", ref ModContext.Settings.Diag_BoundsLogRepeatSec, 0f, 30f);

            SliderInt("Label_ScatterWarningLimit", ref ModContext.Settings.Diag_ScatterWarningLimit, 0, 50);
            SliderInt("Label_LogListDrawBudget", ref ModContext.Settings.LogListDrawBudget, 4, 400);

            GUILayout.Space(5);
        }
    }
}
