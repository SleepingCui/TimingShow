using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityFileDialog;
using TimingShow.Bridge;
using TimingShow.Patches;
using TimingShow.Update;

namespace TimingShow.Options
{
    internal static class NewUIOptions
    {
        private static readonly string[] TabKeys =
        {
            "Tab_General",
            "Tab_Display",
            "Tab_Graphs",
            "Tab_Logging",
            "Tab_Advanced"
        };

        private const int TabsPerRow = 5;
        private const float TabGap = 6f;

        private static int _page;
        private static bool _restartScene;
        private static string _maxPointsText;
        private static string _scatterSampleCountText;
        private static string _scatterMaxPointsText;
        private static string _analyzerPortText;
        private static string _analyzerTimeoutText;
        private static string _diagBoundsIntervalText;
        private static string _diagBoundsToleranceText;
        private static string _diagBoundsLogLimitText;
        private static string _diagBoundsRepeatText;
        private static string _scatterWarningLimitText;
        private static string _logListDrawBudgetText;
        private static string _bufferSizeText;

        internal static void Draw()
        {
            if (ModContext.Settings == null) return;

            NewUISkin.Ensure();
            NewUIWidgets.BeginFrame();
            UpdateBridge.DrawBanner();
            DrawTabs();
            if (ModContext.Settings.InsecureUpdateDebugEnabled) NewUIWidgets.Warning(i18n.T("Warning_InsecureUpdateDebug"));
            GUILayout.Space(6f);
            DrawPage();
        }

        private static void DrawTabs()
        {
            for (int start = 0; start < TabKeys.Length; start += TabsPerRow)
            {
                int count = Mathf.Min(TabsPerRow, TabKeys.Length - start);
                Rect row = NewUIWidgets.Row(30f);
                float cellWidth = (row.width - TabGap * (count - 1)) / count;
                for (int i = 0; i < count; i++)
                {
                    Rect cell = new Rect(row.x + i * (cellWidth + TabGap), row.y, cellWidth, row.height);
                    if (NewUIWidgets.ButtonAt(cell, i18n.T(TabKeys[start + i]), _page == start + i)) _page = start + i;
                }
                GUILayout.Space(2f);
            }
        }

        private static void DrawPage()
        {
            _restartScene = false;

            GUILayout.BeginVertical(NewUISkin.PageStyle);
            {
                switch (_page)
                {
                case 0:
                    DrawGeneralCards();
                    break;
                case 1:
                    DrawDisplayCards();
                    DrawJudgeCards();
                    DrawHudCards();
                    DrawEditorCard();
                    break;
                case 2:
                    DrawGraphCards();
                    break;
                case 3:
                    DrawLoggingCards();
                    break;
                default:
                    DrawAdvancedCards();
                    break;
                }
            }
            GUILayout.EndVertical();

            if (_restartScene && ADOBase.isLevelEditor) ADOBase.RestartScene();
        }

        private static void BeginCard()
        {
            GUILayout.BeginVertical(NewUISkin.CardStyle);
        }

        private static void EndCard()
        {
            GUILayout.EndVertical();
            GUILayout.Space(6f);
        }

        private static void DrawGeneralCards()
        {
            Settings s = ModContext.Settings;

            BeginCard();
            s.NewSettingsUI = NewUIWidgets.Toggle("Toggle_NewSettingsUI", s.NewSettingsUI, "Desc_NewSettingsUI");
            EndCard();

            BeginCard();
            DrawLanguageRow(s);
            bool autoLanguage = s.AutoLanguage;
            s.AutoLanguage = NewUIWidgets.Toggle("Toggle_AutoLanguage", s.AutoLanguage);
            if (!autoLanguage && s.AutoLanguage) i18n.ApplyAutoLanguage();
            EndCard();

            BeginCard();
            NewUIWidgets.CardTitle("Btn_Other");
            s.SessionReport_Enabled = NewUIWidgets.Toggle("Toggle_SessionReport", s.SessionReport_Enabled);

            bool windowOpen = TimingShow.HUD.LogGraphWindow.LogGraphWindow.IsOpen;
            bool canToggleWindow = windowOpen || TimingShow.SessionReport.LastSessionReport.HasReport;
            string windowKey = windowOpen ? "Btn_CloseLogWindow" : "Btn_OpenLogWindow";

            Rect actions = NewUIWidgets.Row(30f);
            Rect windowButton = new Rect(actions.x, actions.y, 200f, actions.height);
            Rect resetButton = new Rect(windowButton.xMax + 8f, actions.y, 200f, actions.height);
            if (NewUIWidgets.ButtonAt(windowButton, i18n.T(windowKey), false, canToggleWindow))
            {
                if (windowOpen) TimingShow.SessionReport.LastSessionReport.CloseWindow();
                else TimingShow.SessionReport.LastSessionReport.OpenWindow();
            }

            if (NewUIWidgets.ButtonAt(resetButton, i18n.T("Btn_Reset"))) Players.ResetAll();

            NewUIWidgets.Divider();
            DrawUpdateRows(s);
            EndCard();

            BeginCard();
            NewUIWidgets.CardTitle("Tab_Theme");
            s.Theme_Primary = NewUIWidgets.ColorRow("Label_ThemePrimary", s.Theme_Primary, Settings.Theme_DefaultPrimary);
            s.Theme_Background = NewUIWidgets.ColorRow("Label_ThemeBackground", s.Theme_Background, Settings.Theme_DefaultBackground);
            s.Theme_Element = NewUIWidgets.ColorRow("Label_ThemeElement", s.Theme_Element, Settings.Theme_DefaultElement);
            s.Theme_Text = NewUIWidgets.ColorRow("Label_ThemeText", s.Theme_Text, Settings.Theme_DefaultText);
            NewUIWidgets.Hint(i18n.T("Hint_ThemeApply"));
            if (NewUIWidgets.Button(i18n.T("Btn_ThemeReset"), 180f))
            {
                s.Theme_Primary = Settings.Theme_DefaultPrimary;
                s.Theme_Background = Settings.Theme_DefaultBackground;
                s.Theme_Element = Settings.Theme_DefaultElement;
                s.Theme_Text = Settings.Theme_DefaultText;
            }
            EndCard();
        }

        private static void DrawUpdateRows(Settings s)
        {
            if (!UpdateBridge.IsAvailable) return;

            bool supported = UpdateBridge.Supported;
            s.UpdateCheckEnabled = NewUIWidgets.Toggle("Updater_Enable", s.UpdateCheckEnabled, null, 0f, supported);
            s.UpdateAutoUpdate = NewUIWidgets.Toggle("Updater_Auto", s.UpdateAutoUpdate, null, 0f, supported && s.UpdateCheckEnabled);

            Rect row = NewUIWidgets.Row(30f);
            Rect checkButton = new Rect(row.x, row.y, 200f, row.height);
            if (NewUIWidgets.ButtonAt(checkButton, i18n.T("Btn_CheckUpdateNow"), false, supported)) UpdateBridge.CheckNow();

            string status = UpdateBridge.StatusText;
            if (!string.IsNullOrEmpty(status))
            {
                float statusWidth = Mathf.Max(10f, row.xMax - checkButton.xMax - 10f);
                GUI.Label(new Rect(checkButton.xMax + 10f, row.y, statusWidth, row.height), status, NewUISkin.DimLabelStyle);
            }

            if (supported) return;

            string reason = UpdateBridge.UnavailableText;
            if (!string.IsNullOrEmpty(reason))
                NewUIWidgets.Hint(reason, UpdateBridge.UnavailableIsError ? new Color(1f, 0.333f, 0.333f) : NewUISkin.TextMuted);
        }

        private static void DrawLanguageRow(Settings s)
        {
            List<string> languages = new List<string>(i18n.AvailableLanguages);
            if (languages.Count == 0) return;

            Rect row = NewUIWidgets.Row(30f);
            float gap = 6f;
            float cellWidth = (row.width - 12f - gap * (languages.Count - 1)) / languages.Count;

            for (int i = 0; i < languages.Count; i++)
            {
                string language = languages[i];
                Rect cell = new Rect(row.x + 6f + i * (cellWidth + gap), row.y, cellWidth, 26f);
                if (!NewUIWidgets.ButtonAt(cell, language, s.Language == language)) continue;
                s.Language = language;
                s.AutoLanguage = false;
            }
        }

        private static void DrawDisplayCards()
        {
            Settings s = ModContext.Settings;

            {
                BeginCard();
                s.ShowInSongTitle = NewUIWidgets.Toggle("Toggle_Title", s.ShowInSongTitle);
                if (s.ShowInSongTitle)
                {
                    s.Perc1 = NewUIWidgets.IntSlider("Label_Precision", s.Perc1, 0, 5);
                    s.Title_FontSize = NewUIWidgets.IntSlider("Label_FontSize", s.Title_FontSize, 20, 200);
                    s.Title_UseJudgeColor = NewUIWidgets.Toggle("HUD_UseJudgeColor", s.Title_UseJudgeColor);
                    if (s.Title_UseJudgeColor && !HitMarginCompat.IsGame34)
                        s.Title_EnableXPerfect = NewUIWidgets.Toggle("Enable_XP", s.Title_EnableXPerfect, null, 16f);
                    s.Title_ShowAngle = NewUIWidgets.Toggle("Toggle_ShowAngle", s.Title_ShowAngle);
                    s.Title_Bump = NewUIWidgets.Toggle("Toggle_Bump", s.Title_Bump);
                }
                EndCard();

                BeginCard();
                bool oldShowOnPlanet = s.ShowOnPlanet;
                s.ShowOnPlanet = NewUIWidgets.Toggle("Toggle_Planet", s.ShowOnPlanet);
                if (oldShowOnPlanet != s.ShowOnPlanet && s.AutoReloadInEditor) _restartScene = true;

                if (s.ShowOnPlanet)
                {
                    s.Perc2 = NewUIWidgets.IntSlider("Label_Precision", s.Perc2, 0, 5);
                    s.Planet_FontSize = NewUIWidgets.IntSlider("Label_FontSize", s.Planet_FontSize, 20, 200);
                    s.Planet_ShowAngle = NewUIWidgets.Toggle("Toggle_ShowAngle", s.Planet_ShowAngle);
                    if (!HitMarginCompat.IsGame34)
                        s.Planet_EnableXPerfect = NewUIWidgets.Toggle("Enable_XP", s.Planet_EnableXPerfect);
                }
                EndCard();

                if (s.ShowOnPlanet)
                {
                    BeginCard();
                    NewUIWidgets.CardTitle("Setting_Title");
                    s.ReplaceFailOverload = NewUIWidgets.Toggle("Toggle_FailOverload", s.ReplaceFailOverload);
                    s.ReplaceTooEarly = NewUIWidgets.Toggle("Toggle_TooEarly", s.ReplaceTooEarly);
                    s.ReplaceVeryEarly = NewUIWidgets.Toggle("Toggle_VeryEarly", s.ReplaceVeryEarly);
                    s.ReplaceEarlyPerfect = NewUIWidgets.Toggle("Toggle_EarlyPerfect", s.ReplaceEarlyPerfect);

                    if (HitMarginCompat.IsGame34)
                        s.ReplacePerfectMinus = NewUIWidgets.Toggle("Toggle_PerfectMinus", s.ReplacePerfectMinus);
                    else
                        s.ReplacePerfect = NewUIWidgets.Toggle("Toggle_Perfect", s.ReplacePerfect);

                    if (HitMarginCompat.HasNativeXPerfect)
                    {
                        s.ReplaceXPerfect = NewUIWidgets.Toggle("Toggle_XPerfect", s.ReplaceXPerfect);
                        s.ReplacePerfectPlus = NewUIWidgets.Toggle("Toggle_PerfectPlus", s.ReplacePerfectPlus);
                    }

                    s.ReplaceLatePerfect = NewUIWidgets.Toggle("Toggle_LatePerfect", s.ReplaceLatePerfect);
                    s.ReplaceVeryLate = NewUIWidgets.Toggle("Toggle_VeryLate", s.ReplaceVeryLate);
                    s.ReplaceTooLate = NewUIWidgets.Toggle("Toggle_TooLate", s.ReplaceTooLate);
                    s.ReplaceFailMiss = NewUIWidgets.Toggle("Toggle_FailMiss", s.ReplaceFailMiss);
                    s.ReplaceMultipress = NewUIWidgets.Toggle("Toggle_Multipress", s.ReplaceMultipress);
                    s.ReplaceOverPress = NewUIWidgets.Toggle("Toggle_OverPress", s.ReplaceOverPress);
                    s.ReplaceAuto = NewUIWidgets.Toggle("Toggle_Auto", s.ReplaceAuto);
                    EndCard();
                }
            }
        }

        private static void DrawJudgeCards()
        {
            Settings s = ModContext.Settings;

            {
                BeginCard();
                s.ShowOnDeath = NewUIWidgets.Toggle("Toggle_Death", s.ShowOnDeath);
                if (s.ShowOnDeath)
                {
                    s.Perc3 = NewUIWidgets.IntSlider("Label_Precision", s.Perc3, 0, 5);
                    s.ShowOnDeath_ShowAvgTiming = NewUIWidgets.Toggle("Toggle_Death_AvgTiming", s.ShowOnDeath_ShowAvgTiming);
                    s.ShowOnDeath_ShowUR = NewUIWidgets.Toggle("Toggle_Death_UR", s.ShowOnDeath_ShowUR);
                    s.ShowOnDeath_ShowXACC = NewUIWidgets.Toggle("Toggle_Death_XACC", s.ShowOnDeath_ShowXACC);
                    s.ShowOnDeath_ShowRatio = NewUIWidgets.Toggle("Toggle_Death_Ratio", s.ShowOnDeath_ShowRatio);
                    s.ShowOnDeath_FontSize = NewUIWidgets.IntSlider("Label_FontSize", s.ShowOnDeath_FontSize, 20, 200);
                }
                EndCard();

                BeginCard();
                s.ShowInWinPage = NewUIWidgets.Toggle("Toggle_Win", s.ShowInWinPage);
                if (s.ShowInWinPage)
                {
                    s.Perc4 = NewUIWidgets.IntSlider("Label_Precision", s.Perc4, 0, 5);
                    s.ShowInWinPage_ShowAvgTiming = NewUIWidgets.Toggle("Toggle_Death_AvgTiming", s.ShowInWinPage_ShowAvgTiming);
                    s.ShowInWinPage_ShowUR = NewUIWidgets.Toggle("Toggle_Death_UR", s.ShowInWinPage_ShowUR);
                    s.ShowInWinPage_ShowRatio = NewUIWidgets.Toggle("Toggle_Death_Ratio", s.ShowInWinPage_ShowRatio);
                    s.Coop_WinPageAllPlayers = NewUIWidgets.Toggle("Toggle_Coop_WinPageAllPlayers", s.Coop_WinPageAllPlayers);
                    s.ShowInWinPage_FontSize = NewUIWidgets.IntSlider("Label_FontSize", s.ShowInWinPage_FontSize, 20, 200);
                }
                EndCard();
            }
        }

        private static void DrawHudRows(ref float x, ref float y, ref float scale, ref bool bold, ref int align, ref string format, ref int precision)
        {
            x = NewUIWidgets.Slider("Label_XOffset", x, -0.5f, 0.5f);
            y = NewUIWidgets.Slider("Label_YOffset", y, -0.5f, 0.5f);
            scale = NewUIWidgets.Slider("Label_Scale", scale, 0.2f, 3f);
            bold = NewUIWidgets.Toggle("Toggle_Bold", bold);
            align = NewUIWidgets.Choice("Label_Align", align, new[] { i18n.T("Btn_Left"), i18n.T("Btn_Center"), i18n.T("Btn_Right") });
            format = NewUIWidgets.TextRow("Label_Format", format, 200f);
            precision = NewUIWidgets.IntSlider("Label_Precision", precision, 0, 5);
        }

        private static void DrawHudFontRow(ref bool useCustomFont, ref string fontPath)
        {
            useCustomFont = NewUIWidgets.Toggle("Toggle_CustomFont", useCustomFont);

            Rect row = NewUIWidgets.Row(28f);
            GUI.Label(new Rect(row.x + 8f, row.y, 110f, row.height), i18n.T("Label_FontPath"), NewUISkin.LabelStyle);
            string displayPath = string.IsNullOrWhiteSpace(fontPath) ? i18n.T("Label_GameFont") : fontPath;
            GUI.Label(new Rect(row.x + 118f, row.y, row.width - 118f - 92f, row.height), displayPath, NewUISkin.MutedLabelStyle);

            if (!NewUIWidgets.ButtonAt(new Rect(row.xMax - 86f, row.y + 2f, 84f, 24f), i18n.T("Btn_SelectFont"))) return;

            string initialDirectory = string.IsNullOrWhiteSpace(fontPath) ? "" : Path.GetDirectoryName(fontPath);
            string selectedFont = FileBrowser.PickFile(initialDirectory, "Font", new[] { "ttf", "otf" }, i18n.T("Btn_SelectFont"));
            if (string.IsNullOrWhiteSpace(selectedFont)) return;

            string extension = Path.GetExtension(selectedFont);
            if (string.Equals(extension, ".ttf", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".otf", StringComparison.OrdinalIgnoreCase))
            {
                fontPath = Path.GetFullPath(selectedFont);
                useCustomFont = true;
            }
            else
            {
                ModContext.Logger?.Log("Unsupported HUD font file: " + selectedFont);
            }
        }

        private static void DrawHudCards()
        {
            Settings s = ModContext.Settings;

            {
                BeginCard();
                s.ShowTimingHUD = NewUIWidgets.Toggle("Toggle_TimingHUD", s.ShowTimingHUD);
                if (s.ShowTimingHUD)
                {
                    DrawHudRows(ref s.HUD_x, ref s.HUD_y, ref s.HUD_scale, ref s.HUD_bold, ref s.HUD_align, ref s.HUD_Format, ref s.PercHUD);
                    DrawHudFontRow(ref s.HUD_UseCustomFont, ref s.HUD_FontPath);
                    s.HUD_UseJudgeColor = NewUIWidgets.Toggle("HUD_UseJudgeColor", s.HUD_UseJudgeColor);
                    if (s.HUD_UseJudgeColor && !HitMarginCompat.IsGame34)
                        s.HUD_EnableXPerfect = NewUIWidgets.Toggle("Enable_XP", s.HUD_EnableXPerfect, null, 16f);
                    s.HUD_ShowAngle = NewUIWidgets.Toggle("Toggle_ShowAngle", s.HUD_ShowAngle);
                    s.HUD_Bump = NewUIWidgets.Toggle("Toggle_Bump", s.HUD_Bump);
                    s.Coop_TimingHUD = NewUIWidgets.Toggle("Toggle_CoopPlayers", s.Coop_TimingHUD);
                }
                EndCard();

                BeginCard();
                s.ShowAvgHUD = NewUIWidgets.Toggle("Toggle_AvgHUD", s.ShowAvgHUD);
                if (s.ShowAvgHUD)
                {
                    DrawHudRows(ref s.AvgHUD_x, ref s.AvgHUD_y, ref s.AvgHUD_scale, ref s.AvgHUD_bold, ref s.AvgHUD_align, ref s.AvgHUD_Format, ref s.PercAvgHUD);
                    DrawHudFontRow(ref s.AvgHUD_UseCustomFont, ref s.AvgHUD_FontPath);
                    s.AvgHUD_Bump = NewUIWidgets.Toggle("Toggle_Bump", s.AvgHUD_Bump);
                    s.Coop_AvgHUD = NewUIWidgets.Toggle("Toggle_CoopPlayers", s.Coop_AvgHUD);
                }
                EndCard();

                BeginCard();
                s.ShowURHUD = NewUIWidgets.Toggle("Toggle_URHUD", s.ShowURHUD);
                if (s.ShowURHUD)
                {
                    DrawHudRows(ref s.URHUD_x, ref s.URHUD_y, ref s.URHUD_scale, ref s.URHUD_bold, ref s.URHUD_align, ref s.URHUD_Format, ref s.PercURHUD);
                    DrawHudFontRow(ref s.URHUD_UseCustomFont, ref s.URHUD_FontPath);
                    s.URHUD_Bump = NewUIWidgets.Toggle("Toggle_Bump", s.URHUD_Bump);
                    s.Coop_URHUD = NewUIWidgets.Toggle("Toggle_CoopPlayers", s.Coop_URHUD);
                }
                EndCard();

                BeginCard();
                s.ShowRatioHUD = NewUIWidgets.Toggle("Toggle_RatioHUD", s.ShowRatioHUD);
                if (s.ShowRatioHUD)
                {
                    DrawHudRows(ref s.RatioHUD_x, ref s.RatioHUD_y, ref s.RatioHUD_scale, ref s.RatioHUD_bold, ref s.RatioHUD_align, ref s.RatioHUD_Format, ref s.PercRatioHUD);
                    DrawHudFontRow(ref s.RatioHUD_UseCustomFont, ref s.RatioHUD_FontPath);
                    s.RatioHUD_Bump = NewUIWidgets.Toggle("Toggle_Bump", s.RatioHUD_Bump);
                    s.Coop_RatioHUD = NewUIWidgets.Toggle("Toggle_CoopPlayers", s.Coop_RatioHUD);
                    s.Ratio_Mode = NewUIWidgets.Choice("Label_RatioMode", s.Ratio_Mode, new[] { i18n.T("Btn_RatioNormalPerfect"), i18n.T("Btn_RatioPerfectFamily"), i18n.T("Btn_RatioXPerfect") });
                }
                EndCard();
            }
        }

        private static void DrawGraphCards()
        {
            Settings s = ModContext.Settings;

            {
                BeginCard();
                s.ShowXACCGraph = NewUIWidgets.Toggle("Toggle_XACCGraph", s.ShowXACCGraph);
                if (s.ShowXACCGraph)
                {
                    s.XACCGraph_ShowEnd = NewUIWidgets.Toggle("Toggle_ShowEnd", s.XACCGraph_ShowEnd);
                    s.XACCGraph_ShowPerfInfo = NewUIWidgets.Toggle("Toggle_ShowPerfInfo", s.XACCGraph_ShowPerfInfo);
                    s.XACCGraph_ShowBorder = NewUIWidgets.Toggle("Toggle_ShowBorder", s.XACCGraph_ShowBorder);
                    s.XACCGraph_X = NewUIWidgets.Slider("Label_XOffset", s.XACCGraph_X, 0f, 1f);
                    s.XACCGraph_Y = NewUIWidgets.Slider("Label_YOffset", s.XACCGraph_Y, 0f, 1f);
                    s.XACCGraph_Width = NewUIWidgets.Slider("Label_GraphWidth", s.XACCGraph_Width, 80f, 1200f, "0");
                    s.XACCGraph_Height = NewUIWidgets.Slider("Label_GraphHeight", s.XACCGraph_Height, 40f, 600f, "0");
                    s.XACCGraph_Scale = NewUIWidgets.Slider("Label_Scale", s.XACCGraph_Scale, 0.2f, 3f);
                    s.XACCGraph_MaxPoints = NewUIWidgets.IntField("Label_MaxPoints", ref _maxPointsText, s.XACCGraph_MaxPoints, 20, 5000);

                    s.XACCGraph_BgColor = NewUIWidgets.ColorRow("Label_BgColor", s.XACCGraph_BgColor, Settings.XACCGraph_DefaultBgColor);
                    s.XACCGraph_LineColor = NewUIWidgets.ColorRow("Label_LineColor", s.XACCGraph_LineColor, Settings.XACCGraph_DefaultLineColor);
                    s.XACCGraph_GridColor = NewUIWidgets.ColorRow("Label_GridColor", s.XACCGraph_GridColor, Settings.XACCGraph_DefaultGridColor);
                    s.XACCGraph_AxisTextColor = NewUIWidgets.ColorRow("Label_AxisTextColor", s.XACCGraph_AxisTextColor, Settings.XACCGraph_DefaultAxisTextColor);
                    s.XACCGraph_ValueTextColor = NewUIWidgets.ColorRow("Label_InfoTextColor", s.XACCGraph_ValueTextColor, Settings.XACCGraph_DefaultValueTextColor);
                    if (s.XACCGraph_ShowBorder)
                        s.XACCGraph_BorderColor = NewUIWidgets.ColorRow("Label_BorderColor", s.XACCGraph_BorderColor, Settings.XACCGraph_DefaultBorderColor);

                    DrawHudFontRow(ref s.XACCGraph_UseCustomFont, ref s.XACCGraph_FontPath);
                }
                EndCard();

                BeginCard();
                s.ShowTimingScatter = NewUIWidgets.Toggle("Toggle_TimingScatter", s.ShowTimingScatter);
                if (s.ShowTimingScatter)
                {
                    s.TimingScatter_ShowEnd = NewUIWidgets.Toggle("Toggle_ShowEnd", s.TimingScatter_ShowEnd);
                    s.TimingScatter_IgnoreOutliers = NewUIWidgets.Toggle("Toggle_IgnoreOutliers", s.TimingScatter_IgnoreOutliers);
                    s.TimingScatter_ShowPerfInfo = NewUIWidgets.Toggle("Toggle_ShowPerfInfo", s.TimingScatter_ShowPerfInfo);
                    s.TimingScatter_X = NewUIWidgets.Slider("Label_XOffset", s.TimingScatter_X, 0f, 1f);
                    s.TimingScatter_Y = NewUIWidgets.Slider("Label_YOffset", s.TimingScatter_Y, 0f, 1f);
                    s.TimingScatter_Width = NewUIWidgets.Slider("Label_GraphWidth", s.TimingScatter_Width, 80f, 1200f, "0");
                    s.TimingScatter_Height = NewUIWidgets.Slider("Label_GraphHeight", s.TimingScatter_Height, 40f, 600f, "0");
                    s.TimingScatter_Scale = NewUIWidgets.Slider("Label_Scale", s.TimingScatter_Scale, 0.2f, 3f);
                    s.TimingScatter_SampleCount = NewUIWidgets.IntField("Label_SampleCount", ref _scatterSampleCountText, s.TimingScatter_SampleCount, 10, 20000);
                    s.TimingScatter_MaxRenderPoints = NewUIWidgets.IntField("Label_MaxRenderPoints", ref _scatterMaxPointsText, s.TimingScatter_MaxRenderPoints, 20, 8000);
                    s.TimingScatter_PointSize = NewUIWidgets.Slider("Label_PointSize", s.TimingScatter_PointSize, 1f, 12f);

                    s.TimingScatter_UseHitAxis = NewUIWidgets.Toggle("Toggle_UseHitAxis", s.TimingScatter_UseHitAxis);
                    if (!s.TimingScatter_UseHitAxis)
                        s.TimingScatter_AutoScroll = NewUIWidgets.Toggle("Toggle_AutoScroll", s.TimingScatter_AutoScroll);
                    s.TimingScatter_UseJudgeColor = NewUIWidgets.Toggle("Toggle_UseJudgeColor", s.TimingScatter_UseJudgeColor);
                    s.TimingScatter_ShowZeroLine = NewUIWidgets.Toggle("Toggle_ShowZeroLine", s.TimingScatter_ShowZeroLine);
                    s.TimingScatter_ShowAvgLine = NewUIWidgets.Toggle("Toggle_ShowAvgLine", s.TimingScatter_ShowAvgLine);
                    s.TimingScatter_ShowBorder = NewUIWidgets.Toggle("Toggle_ShowBorder", s.TimingScatter_ShowBorder);
                    s.TimingScatter_ShowJudgeBands = NewUIWidgets.Toggle("Toggle_ShowJudgeBands", s.TimingScatter_ShowJudgeBands);
                    if (s.TimingScatter_ShowJudgeBands)
                    {
                        s.TimingScatter_BandAutoWindow = NewUIWidgets.Toggle("Toggle_BandAutoWindow", s.TimingScatter_BandAutoWindow);
                        if (!s.TimingScatter_BandAutoWindow)
                            s.TimingScatter_BandThresholdBpm = NewUIWidgets.Slider("Label_BandThresholdBpm", s.TimingScatter_BandThresholdBpm, 100f, 600f, "0");

                        s.TimingScatter_BandPerfectColor = NewUIWidgets.ColorRow("Label_BandPerfectColor", s.TimingScatter_BandPerfectColor, Settings.TimingScatter_DefaultBandPerfectColor);
                        s.TimingScatter_BandElPerfectColor = NewUIWidgets.ColorRow("Label_BandElPerfectColor", s.TimingScatter_BandElPerfectColor, Settings.TimingScatter_DefaultBandElPerfectColor);
                        s.TimingScatter_BandEarlyLateColor = NewUIWidgets.ColorRow("Label_BandEarlyLateColor", s.TimingScatter_BandEarlyLateColor, Settings.TimingScatter_DefaultBandEarlyLateColor);
                    }

                    s.TimingScatter_ShowXpBand = NewUIWidgets.Toggle("Toggle_ShowXpBand", s.TimingScatter_ShowXpBand);
                    if (s.TimingScatter_ShowXpBand)
                        s.TimingScatter_BandXpColor = NewUIWidgets.ColorRow("Label_BandXpColor", s.TimingScatter_BandXpColor, Settings.TimingScatter_DefaultBandXpColor);

                    s.TimingScatter_BgColor = NewUIWidgets.ColorRow("Label_BgColor", s.TimingScatter_BgColor, Settings.TimingScatter_DefaultBgColor);
                    s.TimingScatter_GridColor = NewUIWidgets.ColorRow("Label_GridColor", s.TimingScatter_GridColor, Settings.TimingScatter_DefaultGridColor);
                    s.TimingScatter_PointColor = NewUIWidgets.ColorRow("Label_PointColor", s.TimingScatter_PointColor, Settings.TimingScatter_DefaultPointColor);
                    s.TimingScatter_ZeroLineColor = NewUIWidgets.ColorRow("Label_ZeroLineColor", s.TimingScatter_ZeroLineColor, Settings.TimingScatter_DefaultZeroLineColor);
                    s.TimingScatter_AvgLineColor = NewUIWidgets.ColorRow("Label_AvgLineColor", s.TimingScatter_AvgLineColor, Settings.TimingScatter_DefaultAvgLineColor);
                    s.TimingScatter_AxisTextColor = NewUIWidgets.ColorRow("Label_AxisTextColor", s.TimingScatter_AxisTextColor, Settings.TimingScatter_DefaultAxisTextColor);
                    if (s.TimingScatter_ShowBorder)
                        s.TimingScatter_BorderColor = NewUIWidgets.ColorRow("Label_BorderColor", s.TimingScatter_BorderColor, Settings.TimingScatter_DefaultBorderColor);

                    DrawHudFontRow(ref s.TimingScatter_UseCustomFont, ref s.TimingScatter_FontPath);
                }
                EndCard();
            }
        }

        private static void DrawLoggingCards()
        {
            Settings s = ModContext.Settings;

            {
                BeginCard();
                s.EnableLogging = NewUIWidgets.Toggle("Toggle_Logging", s.EnableLogging);
                if (s.EnableLogging)
                {
                    s.PercLog = NewUIWidgets.IntSlider("Label_Precision", s.PercLog, 0, 5);
                    if (!HitMarginCompat.IsGame34)
                        s.Logger_EnableXPerfect = NewUIWidgets.Toggle("Enable_XP", s.Logger_EnableXPerfect);
                    s.Logger_ShowAngle = NewUIWidgets.Toggle("Toggle_ShowAngle", s.Logger_ShowAngle);
                    s.LogAutoplay = NewUIWidgets.Toggle("Toggle_LogAutoplay", s.LogAutoplay);
                    s.UseJsonWriter = NewUIWidgets.Toggle("Toggle_UseJsonWriter", s.UseJsonWriter);
                    s.LogFullClearOnly = NewUIWidgets.Toggle("Toggle_LogFullClearOnly", s.LogFullClearOnly);

                    Rect dirRow = NewUIWidgets.Row(28f);
                    GUI.Label(new Rect(dirRow.x + 8f, dirRow.y, 140f, dirRow.height), i18n.T("Label_LogDir"), NewUISkin.LabelStyle);
                    string absolutePath = OptionLogList.AbsLogPath(s.LogDirectory);
                    string displayPath = string.IsNullOrWhiteSpace(absolutePath) ? "None" : absolutePath;
                    GUI.Label(new Rect(dirRow.x + 148f, dirRow.y, dirRow.width - 148f - 92f, dirRow.height), displayPath, NewUISkin.MutedLabelStyle);
                    if (NewUIWidgets.ButtonAt(new Rect(dirRow.xMax - 86f, dirRow.y + 2f, 84f, 24f), i18n.T("Btn_Browse")))
                    {
                        string selectedFolder = FileBrowser.PickFolder(OptionLogList.GetLogDirectory(), "Folder", new string[0], i18n.T("Label_LogDir"));
                        if (!string.IsNullOrEmpty(selectedFolder)) s.LogDirectory = Path.GetFullPath(selectedFolder);
                    }

                    s.LogBufferSizeKB = NewUIWidgets.IntField("Label_BufferSize", ref _bufferSizeText, s.LogBufferSizeKB, 8, 102400);
                }
                EndCard();

                if (NewUIWidgets.Button(i18n.T("Btn_OpenLogs"), 150f))
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

                GUILayout.Space(4f);
                OptionLogList.DrawNewUI();
            }
        }

        private static void DrawEditorCard()
        {
            Settings s = ModContext.Settings;

            {
                BeginCard();
                s.EditorHits_Enabled = NewUIWidgets.Toggle("Toggle_EditorHits", s.EditorHits_Enabled);
                if (s.EditorHits_Enabled)
                {
                    s.EditorHits_PanelWidth = NewUIWidgets.Slider("HitPanel_PanelWidth", s.EditorHits_PanelWidth, 240f, 460f, "0");
                    s.EditorHits_PanelKey = NewUIWidgets.KeyRow("Label_HitPanelKey", s.EditorHits_PanelKey);
                    string toggleKey = s.EditorHits_PanelVisible ? "Btn_HideHitPanel" : "Btn_ShowHitPanel";
                    if (NewUIWidgets.Button(i18n.T(toggleKey), 200f))
                        s.EditorHits_PanelVisible = !s.EditorHits_PanelVisible;
                }
                EndCard();
            }
        }

        private static void DrawAdvancedCards()
        {
            Settings s = ModContext.Settings;

            {
                BeginCard();
                NewUIWidgets.CardTitle("Btn_Advanced");

                bool hookSupported = XPerfectBridge.IsSupported;
                bool hookMode = NewUIWidgets.Toggle("Toggle_HookMode", s.UseHookMode, "Desc_HookMode", 0f, hookSupported);
                if (hookSupported && hookMode != s.UseHookMode)
                {
                    s.UseHookMode = hookMode;
                    if (hookMode) XPerfectBridge.TryInit(force: true);
                    else XPerfectBridge.UnloadHook();
                }

                DrawHookStatus(XPerfectBridge.CurrentState);

                bool disableInputHooks = NewUIWidgets.Toggle("Toggle_DisableInputHooks", s.DisableInputHooks, "Desc_DisableInputHooks");
                if (disableInputHooks != s.DisableInputHooks)
                {
                    s.DisableInputHooks = disableInputHooks;
                    if (disableInputHooks) InputPatches.Uninstall(ModContext.HarmonyInstance);
                    else InputPatches.Install(ModContext.HarmonyInstance);
                }

                s.AutoReloadInEditor = NewUIWidgets.Toggle("Toggle_AutoReloadInEditor", s.AutoReloadInEditor, "Desc_AutoReloadInEditor");

                bool previousAnalyzerEnabled = s.AnalyzerBridgeEnabled;
                s.AnalyzerBridgeEnabled = NewUIWidgets.Toggle("Toggle_AnalyzerBridge", s.AnalyzerBridgeEnabled, "Desc_AnalyzerBridge");
                if (previousAnalyzerEnabled && !s.AnalyzerBridgeEnabled) LogAnalyzerBridge.Stop();

                if (s.AnalyzerBridgeEnabled)
                {
                    s.AnalyzerBridgePort = NewUIWidgets.IntField("Label_AnalyzerPort", ref _analyzerPortText, s.AnalyzerBridgePort, 0, 65535);
                    s.AnalyzerBridgeTimeoutSec = NewUIWidgets.IntField("Label_AnalyzerTimeout", ref _analyzerTimeoutText, s.AnalyzerBridgeTimeoutSec, LogAnalyzerBridge.MinTimeoutSeconds, LogAnalyzerBridge.MaxTimeoutSeconds);
                }
                EndCard();

                BeginCard();
                if (NewUIWidgets.Section("Btn_Debug", "newui_debug"))
                {
                    NewUIWidgets.Hint(i18n.T("Desc_DebugWarning"));
                    s.Diag_PerfLogEnabled = NewUIWidgets.Toggle("Toggle_DiagPerfLog", s.Diag_PerfLogEnabled);
                    s.Diag_GraphBounds = NewUIWidgets.Toggle("Toggle_DiagGraphBounds", s.Diag_GraphBounds);
                    s.Diag_InputTrace = NewUIWidgets.Toggle("Toggle_DiagInputTrace", s.Diag_InputTrace);
                    s.Diag_TrackMeshBounds = NewUIWidgets.Toggle("Toggle_DiagTrackMesh", s.Diag_TrackMeshBounds);

                    if (UpdateBridge.IsAvailable)
                    {
                        bool updateTrace = s.Diag_UpdateTrace;
                        s.Diag_UpdateTrace = NewUIWidgets.Toggle("Toggle_DiagUpdateTrace", s.Diag_UpdateTrace, null, 0f, UpdateBridge.Supported);
                        if (!updateTrace && s.Diag_UpdateTrace) UpdateBridge.CheckNow();

                        s.Debug_CustomUpdateUrl = NewUIWidgets.Toggle("Toggle_DebugCustomUrl", s.Debug_CustomUpdateUrl, "Desc_DebugCustomUrl", 0f, UpdateBridge.Supported);
                        if (s.Debug_CustomUpdateUrl)
                            s.Debug_UpdateUrl = NewUIWidgets.TextRow("Label_DebugUpdateUrl", s.Debug_UpdateUrl ?? string.Empty);
                        s.Debug_SkipUpdateChecksum = NewUIWidgets.Toggle("Toggle_DebugSkipChecksum", s.Debug_SkipUpdateChecksum, "Desc_DebugSkipChecksum", 0f, UpdateBridge.Supported);
                        s.Debug_SkipTlsValidation = NewUIWidgets.Toggle("Toggle_DebugSkipTls", s.Debug_SkipTlsValidation, "Desc_DebugSkipTls", 0f, UpdateBridge.Supported);
                    }

                    s.Diag_BoundsIntervalSec = NewUIWidgets.FloatField("Label_DiagBoundsInterval", ref _diagBoundsIntervalText, s.Diag_BoundsIntervalSec, 0.1f, 5f);
                    s.Diag_BoundsTolerancePx = NewUIWidgets.FloatField("Label_DiagBoundsTolerance", ref _diagBoundsToleranceText, s.Diag_BoundsTolerancePx, 0f, 4f);
                    s.Diag_BoundsLogLimit = NewUIWidgets.IntField("Label_DiagBoundsLogLimit", ref _diagBoundsLogLimitText, s.Diag_BoundsLogLimit, 1, 200);
                    s.Diag_BoundsLogRepeatSec = NewUIWidgets.FloatField("Label_DiagBoundsRepeat", ref _diagBoundsRepeatText, s.Diag_BoundsLogRepeatSec, 0f, 30f);
                    s.Diag_ScatterWarningLimit = NewUIWidgets.IntField("Label_ScatterWarningLimit", ref _scatterWarningLimitText, s.Diag_ScatterWarningLimit, 0, 50);
                    s.LogListDrawBudget = NewUIWidgets.IntField("Label_LogListDrawBudget", ref _logListDrawBudgetText, s.LogListDrawBudget, 4, 400);
                }
                EndCard();
            }
        }

        private static void DrawHookStatus(XPerfectBridge.HookState state)
        {
            string status;
            Color color;

            switch (state)
            {
            case XPerfectBridge.HookState.Success:
                status = i18n.T("Status_HookSuccess");
                color = new Color(0.333f, 1f, 0.333f);
                break;
            case XPerfectBridge.HookState.Failed:
                status = i18n.T("Status_HookFailed") + XPerfectBridge.LastErrorMessage;
                color = new Color(1f, 0.333f, 0.333f);
                break;
            case XPerfectBridge.HookState.NotApplicable:
                status = i18n.T("Status_HookNotApplicable");
                color = new Color(0.333f, 0.8f, 1f);
                break;
            default:
                return;
            }

            NewUIWidgets.Hint(i18n.T("Label_CurrentStatus") + ": " + status, color);
        }

    }
}
