using UnityEngine;
using UnityModManagerNet;

namespace TimingShow
{
    public static class Options
    {
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
                GUILayout.Label(LangMan.T("Title_TimingHUD"));

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

            GUILayout.Space(15);
            if (GUILayout.Button(LangMan.T("Btn_Reset"), GUILayout.Width(150)))
            {
                Main.SessionOffsets.Clear();
                Main.LastTiming = 0;
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
    }
}