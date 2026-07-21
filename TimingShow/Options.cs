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

        public static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            GUILayout.BeginHorizontal();

            foreach (string langCode in LangMan.AvailableLanguages)
            {
                GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
                if (Main.Settings.Language == langCode) btnStyle.fontStyle = FontStyle.Bold;

                if (GUILayout.Button(langCode, btnStyle, GUILayout.Width(100)))
                {
                    Main.Settings.Language = langCode;
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            DrawSettingRow(LangMan.T("Toggle_Title"), ref Main.Settings.ShowInSongTitle, ref Main.Settings.Perc1);
            if (Main.Settings.ShowInSongTitle)
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

            DrawSettingRow(LangMan.T("Toggle_Planet"), ref Main.Settings.ShowOnPlanet, ref Main.Settings.Perc2);
            if (Main.Settings.ShowOnPlanet)
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

            DrawSettingRow(LangMan.T("Toggle_Death"), ref Main.Settings.ShowOnDeath, ref Main.Settings.Perc3);
            DrawSettingRow(LangMan.T("Toggle_Win"), ref Main.Settings.ShowInWinPage, ref Main.Settings.Perc4);

            Main.Settings.ShowTimingHUD = GUILayout.Toggle(Main.Settings.ShowTimingHUD, LangMan.T("Toggle_TimingHUD"));
            if (Main.Settings.ShowTimingHUD)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label(LangMan.T("Label_XOffset") + $"{Main.Settings.HUD_x:F2}", GUILayout.Width(120));
                Main.Settings.HUD_x = GUILayout.HorizontalSlider(Main.Settings.HUD_x, -0.5f, 0.5f, GUILayout.Width(120));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label(LangMan.T("Label_YOffset") + $"{Main.Settings.HUD_y:F2}", GUILayout.Width(120));
                Main.Settings.HUD_y = GUILayout.HorizontalSlider(Main.Settings.HUD_y, -0.5f, 0.5f, GUILayout.Width(120));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label(LangMan.T("Label_Scale") + $"{Main.Settings.HUD_scale:F2}", GUILayout.Width(120));
                Main.Settings.HUD_scale = GUILayout.HorizontalSlider(Main.Settings.HUD_scale, 0.2f, 3.0f, GUILayout.Width(120));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                Main.Settings.HUD_bold = GUILayout.Toggle(Main.Settings.HUD_bold, LangMan.T("Toggle_Bold"));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label(LangMan.T("Label_Align"), GUILayout.Width(100));

                GUIStyle leftStyle = new GUIStyle(GUI.skin.button);
                if (Main.Settings.HUD_align == 0) leftStyle.fontStyle = FontStyle.Bold;
                if (GUILayout.Button(LangMan.T("Btn_Left"), leftStyle, GUILayout.Width(60))) Main.Settings.HUD_align = 0;

                GUIStyle centerStyle = new GUIStyle(GUI.skin.button);
                if (Main.Settings.HUD_align == 1) centerStyle.fontStyle = FontStyle.Bold;
                if (GUILayout.Button(LangMan.T("Btn_Center"), centerStyle, GUILayout.Width(60))) Main.Settings.HUD_align = 1;

                GUIStyle rightStyle = new GUIStyle(GUI.skin.button);
                if (Main.Settings.HUD_align == 2) rightStyle.fontStyle = FontStyle.Bold;
                if (GUILayout.Button(LangMan.T("Btn_Right"), rightStyle, GUILayout.Width(60))) Main.Settings.HUD_align = 2;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label(LangMan.T("Label_Format"), GUILayout.Width(100));
                Main.Settings.HUD_Format = GUILayout.TextField(Main.Settings.HUD_Format, GUILayout.Width(200));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label(LangMan.T("Label_Precision") + $"{Main.Settings.PercHUD}", GUILayout.Width(120));
                Main.Settings.PercHUD = Mathf.RoundToInt(GUILayout.HorizontalSlider(Main.Settings.PercHUD, 0, 5, GUILayout.Width(100)));
                GUILayout.EndHorizontal();

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

            GUILayout.BeginVertical();
            {
                Main.Settings.EnableLogging = GUILayout.Toggle(Main.Settings.EnableLogging, LangMan.T("Toggle_Logging"));

                if (Main.Settings.EnableLogging)
                {
                    GUILayout.Space(20);
                    Main.Settings.Logger_EnableXPerfect = GUILayout.Toggle(Main.Settings.Logger_EnableXPerfect, LangMan.T("Enable_XP"));
                    Main.Settings.LogAutoplay = GUILayout.Toggle(Main.Settings.LogAutoplay, LangMan.T("Toggle_LogAutoplay"));

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    GUILayout.Label(LangMan.T("Label_LogDir"), GUILayout.Width(160));


                    string absolutePath = GetAbsoluteLogPath(Main.Settings.LogDirectory);
                    string displayPath = string.IsNullOrWhiteSpace(absolutePath) ? "未选择" : absolutePath;
                    GUILayout.Label(displayPath, GUILayout.MinWidth(300), GUILayout.MaxWidth(500));

                    if (GUILayout.Button(LangMan.T("Btn_Browse"), GUILayout.Width(70)))
                    {
                        string defaultDir = GetAbsoluteLogPath(Main.Settings.LogDirectory);
                        if (string.IsNullOrWhiteSpace(defaultDir)) defaultDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Mods/TimingShow/Logs"));

                        string selectedFolder = FileBrowser.PickFolder(defaultDir, "Folder", new string[0], LangMan.T("Label_LogDir"));
                        if (!string.IsNullOrEmpty(selectedFolder))
                        {
                            Main.Settings.LogDirectory = Path.GetFullPath(selectedFolder);
                        }
                    }

                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    GUILayout.Label(LangMan.T("Label_BufferSize"), GUILayout.Width(160));

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


                GUILayout.Space(15);
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

                        if (newHookMode)
                        {
                            XPerfectBridge.TryInit(force: true);
                        }
                        else
                        {
                            XPerfectBridge.UnloadHook();
                        }
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
                }
                GUILayout.EndVertical();
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

        private static string GetAbsoluteLogPath(string logDir)
        {
            if (string.IsNullOrWhiteSpace(logDir))
                return null;
            try
            {
                string basePath = Application.dataPath;
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