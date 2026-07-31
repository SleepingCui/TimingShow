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
        private static string bufferSizeText = null;
        private static bool showAdvancedSettings = false;

        private static bool foldoutTitleSettings = false;
        private static bool foldoutPlanetSettings = false;
        private static bool foldoutTimingHUD = false;
        private static bool foldoutURHUD = false;
        private static bool foldoutRatioHUD = false;
        private static bool foldoutLogging = false;

        private static GUIStyle activeButtonStyle;

        public static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            if (activeButtonStyle == null) activeButtonStyle = new GUIStyle(GUI.skin.button);

            GUILayout.BeginHorizontal();
            foreach (string langCode in LangMan.AvailableLanguages)
            {
                activeButtonStyle.fontStyle = (Main.Settings.Language == langCode) ? FontStyle.Bold : FontStyle.Normal;

                if (GUILayout.Button(langCode, activeButtonStyle, GUILayout.Width(100)))
                {
                    Main.Settings.Language = langCode;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            // title
            DrawSettingRowFold(LangMan.T("Toggle_Title"), ref Main.Settings.ShowInSongTitle, ref Main.Settings.Perc1, ref foldoutTitleSettings);
            if (Main.Settings.ShowInSongTitle && foldoutTitleSettings)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                Main.Settings.Title_UseJudgeColor = GUILayout.Toggle(Main.Settings.Title_UseJudgeColor, LangMan.T("HUD_UseJudgeColor"));
                GUILayout.EndHorizontal();

                if (Main.Settings.Title_UseJudgeColor)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(40);
                    Main.Settings.Title_EnableXPerfect = GUILayout.Toggle(Main.Settings.Title_EnableXPerfect, LangMan.T("Enable_XP"));
                    GUILayout.EndHorizontal();
                }
            }

            // planet 
            bool oldShowOnPlanet = Main.Settings.ShowOnPlanet;
            DrawSettingRowFold(LangMan.T("Toggle_Planet"), ref Main.Settings.ShowOnPlanet, ref Main.Settings.Perc2, ref foldoutPlanetSettings);
            if (oldShowOnPlanet != Main.Settings.ShowOnPlanet && Main.Settings.AutoReloadInEditor)
            {
                Patches.EditorReloadPatch.TriggerEditorReload();
            }

            if (Main.Settings.ShowOnPlanet && foldoutPlanetSettings)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                Main.Settings.Planet_EnableXPerfect = GUILayout.Toggle(Main.Settings.Planet_EnableXPerfect, LangMan.T("Enable_XP"));
                GUILayout.EndHorizontal();

                GUILayout.Label(LangMan.T("Setting_Title"));
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(20);
                    GUILayout.BeginVertical();
                    {
                        Main.Settings.ReplaceFailOverload = GUILayout.Toggle(Main.Settings.ReplaceFailOverload, LangMan.T("Toggle_FailOverload"));
                        Main.Settings.ReplaceTooEarly = GUILayout.Toggle(Main.Settings.ReplaceTooEarly, LangMan.T("Toggle_TooEarly"));
                        Main.Settings.ReplaceVeryEarly = GUILayout.Toggle(Main.Settings.ReplaceVeryEarly, LangMan.T("Toggle_VeryEarly"));
                        Main.Settings.ReplaceEarlyPerfect = GUILayout.Toggle(Main.Settings.ReplaceEarlyPerfect, LangMan.T("Toggle_EarlyPerfect"));
                        Main.Settings.ReplacePerfect = GUILayout.Toggle(Main.Settings.ReplacePerfect, LangMan.T("Toggle_Perfect"));
                        Main.Settings.ReplaceLatePerfect = GUILayout.Toggle(Main.Settings.ReplaceLatePerfect, LangMan.T("Toggle_LatePerfect"));
                        Main.Settings.ReplaceVeryLate = GUILayout.Toggle(Main.Settings.ReplaceVeryLate, LangMan.T("Toggle_VeryLate"));
                        Main.Settings.ReplaceTooLate = GUILayout.Toggle(Main.Settings.ReplaceTooLate, LangMan.T("Toggle_TooLate"));
                        Main.Settings.ReplaceFailMiss = GUILayout.Toggle(Main.Settings.ReplaceFailMiss, LangMan.T("Toggle_FailMiss"));
                        Main.Settings.ReplaceMultipress = GUILayout.Toggle(Main.Settings.ReplaceMultipress, LangMan.T("Toggle_Multipress"));
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
            }

            // death,win
            DrawSettingRow(LangMan.T("Toggle_Death"), ref Main.Settings.ShowOnDeath, ref Main.Settings.Perc3);
            DrawSettingRow(LangMan.T("Toggle_Win"), ref Main.Settings.ShowInWinPage, ref Main.Settings.Perc4);

            // timinghud
            DrawToggleFold(LangMan.T("Toggle_TimingHUD"), ref Main.Settings.ShowTimingHUD, ref foldoutTimingHUD);
            if (Main.Settings.ShowTimingHUD && foldoutTimingHUD)
            {
                DrawHUDBaseSettings(
                    ref Main.Settings.HUD_x, ref Main.Settings.HUD_y, ref Main.Settings.HUD_scale,
                    ref Main.Settings.HUD_bold, ref Main.Settings.HUD_align, ref Main.Settings.HUD_Format,
                    ref Main.Settings.PercHUD
                );

                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                Main.Settings.HUD_UseJudgeColor = GUILayout.Toggle(Main.Settings.HUD_UseJudgeColor, LangMan.T("HUD_UseJudgeColor"));
                GUILayout.EndHorizontal();

                if (Main.Settings.HUD_UseJudgeColor)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(40);
                    Main.Settings.HUD_EnableXPerfect = GUILayout.Toggle(Main.Settings.HUD_EnableXPerfect, LangMan.T("Enable_XP"));
                    GUILayout.EndHorizontal();
                }
            }

            // urhud
            DrawToggleFold(LangMan.T("Toggle_URHUD"), ref Main.Settings.ShowURHUD, ref foldoutURHUD);
            if (Main.Settings.ShowURHUD && foldoutURHUD)
            {
                DrawHUDBaseSettings(
                    ref Main.Settings.URHUD_x, ref Main.Settings.URHUD_y, ref Main.Settings.URHUD_scale,
                    ref Main.Settings.URHUD_bold, ref Main.Settings.URHUD_align, ref Main.Settings.URHUD_Format,
                    ref Main.Settings.PercURHUD
                );
            }

            // ratiohud
            DrawToggleFold(LangMan.T("Toggle_RatioHUD"), ref Main.Settings.ShowRatioHUD, ref foldoutRatioHUD);
            if (Main.Settings.ShowRatioHUD && foldoutRatioHUD)
            {
                DrawHUDBaseSettings(
                    ref Main.Settings.RatioHUD_x, ref Main.Settings.RatioHUD_y, ref Main.Settings.RatioHUD_scale,
                    ref Main.Settings.RatioHUD_bold, ref Main.Settings.RatioHUD_align, ref Main.Settings.RatioHUD_Format,
                    ref Main.Settings.PercRatioHUD
                );

                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                Main.Settings.Ratio_UseXPerfect = GUILayout.Toggle(Main.Settings.Ratio_UseXPerfect, LangMan.T("Enable_XP"));
                GUILayout.EndHorizontal();
            }

            // logger
            GUILayout.BeginVertical();
            {
                DrawToggleFold(LangMan.T("Toggle_Logging"), ref Main.Settings.EnableLogging, ref foldoutLogging);

                if (Main.Settings.EnableLogging && foldoutLogging)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    GUILayout.Label(LangMan.T("Label_Precision") + $"{Main.Settings.PercLog}", GUILayout.Width(120));
                    Main.Settings.PercLog = Mathf.RoundToInt(GUILayout.HorizontalSlider(Main.Settings.PercLog, 0, 5, GUILayout.Width(100)));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    Main.Settings.Logger_EnableXPerfect = GUILayout.Toggle(Main.Settings.Logger_EnableXPerfect, LangMan.T("Enable_XP"));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    Main.Settings.LogAutoplay = GUILayout.Toggle(Main.Settings.LogAutoplay, LangMan.T("Toggle_LogAutoplay"));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    GUILayout.Label(LangMan.T("Label_LogDir"), GUILayout.Width(140));

                    string absolutePath = GetAbsoluteLogPath(Main.Settings.LogDirectory);
                    string displayPath = string.IsNullOrWhiteSpace(absolutePath) ? "未选择" : absolutePath;
                    GUILayout.Label(displayPath, GUILayout.MinWidth(280), GUILayout.MaxWidth(480));

                    if (GUILayout.Button(LangMan.T("Btn_Browse"), GUILayout.Width(70)))
                    {
                        string defaultDir = GetAbsoluteLogPath(Main.Settings.LogDirectory);
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

                    if (bufferSizeText == null) bufferSizeText = Main.Settings.LogBufferSizeKB.ToString();
                    string newBufferSizeText = GUILayout.TextField(bufferSizeText, GUILayout.Width(80));
                    if (newBufferSizeText != bufferSizeText)
                    {
                        bufferSizeText = newBufferSizeText;
                        if (int.TryParse(newBufferSizeText, out int parsedVal) && parsedVal > 0)
                            Main.Settings.LogBufferSizeKB = parsedVal;
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

            // adv
            string foldoutArrow = showAdvancedSettings ? "▲" : "▼";
            if (GUILayout.Button($"{LangMan.T("Btn_Advanced")} {foldoutArrow}", GUILayout.Width(150)))
                showAdvancedSettings = !showAdvancedSettings;

            if (showAdvancedSettings)
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


        // utils
        private static void DrawToggleFold(string label, ref bool toggle, ref bool foldout)
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


        private static void DrawSettingRowFold(string label, ref bool toggle, ref int precision, ref bool foldout)
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
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                string precisionLabel = LangMan.T("Label_Precision");
                GUILayout.Label($"{precisionLabel}{precision}", GUILayout.Width(120));
                precision = Mathf.RoundToInt(GUILayout.HorizontalSlider(precision, 0, 5, GUILayout.Width(100)));
                GUILayout.EndHorizontal();
            }
        }

        private static void DrawSettingRow(string label, ref bool toggle, ref int precision)
        {
            toggle = GUILayout.Toggle(toggle, label);
            if (toggle)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                string precisionLabel = LangMan.T("Label_Precision");
                GUILayout.Label($"{precisionLabel}{precision}", GUILayout.Width(120));
                precision = Mathf.RoundToInt(GUILayout.HorizontalSlider(precision, 0, 5, GUILayout.Width(100)));
                GUILayout.EndHorizontal();
            }
        }

        private static void DrawHUDBaseSettings(ref float posX, ref float posY, ref float scale, ref bool isBold, ref int align, ref string format, ref int precision)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label(LangMan.T("Label_XOffset") + $"{posX:F2}", GUILayout.Width(120));
            posX = GUILayout.HorizontalSlider(posX, -0.5f, 0.5f, GUILayout.Width(120));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label(LangMan.T("Label_YOffset") + $"{posY:F2}", GUILayout.Width(120));
            posY = GUILayout.HorizontalSlider(posY, -0.5f, 0.5f, GUILayout.Width(120));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label(LangMan.T("Label_Scale") + $"{scale:F2}", GUILayout.Width(120));
            scale = GUILayout.HorizontalSlider(scale, 0.2f, 3.0f, GUILayout.Width(120));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            isBold = GUILayout.Toggle(isBold, LangMan.T("Toggle_Bold"));
            GUILayout.EndHorizontal();

            DrawAlignButtons(ref align);

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label(LangMan.T("Label_Format"), GUILayout.Width(100));
            format = GUILayout.TextField(format, GUILayout.Width(200));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label(LangMan.T("Label_Precision") + $"{precision}", GUILayout.Width(120));
            precision = Mathf.RoundToInt(GUILayout.HorizontalSlider(precision, 0, 5, GUILayout.Width(100)));
            GUILayout.EndHorizontal();
        }

        private static void DrawAlignButtons(ref int alignSetting)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label(LangMan.T("Label_Align"), GUILayout.Width(100));

            string[] labels = { LangMan.T("Btn_Left"), LangMan.T("Btn_Center"), LangMan.T("Btn_Right") };
            for (int i = 0; i < 3; i++)
            {
                activeButtonStyle.fontStyle = (alignSetting == i) ? FontStyle.Bold : FontStyle.Normal;
                if (GUILayout.Button(labels[i], activeButtonStyle, GUILayout.Width(60)))
                    alignSetting = i;
            }
            GUILayout.EndHorizontal();
        }

        private static string GetAbsoluteLogPath(string logDir)
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
    }
}