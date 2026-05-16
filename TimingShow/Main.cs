using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityModManagerNet;
using static Settings;

namespace TimingShow
{
    

    public static class Main
    {
        public static UnityModManager.ModEntry.ModLogger Logger;
        public static bool IsEnabled;
        public static Settings Settings;
        public static double LastTiming = 0;
        public static Color LastTimingColor = Color.white;
        public static GameObject hudObject;
        public static TextUI hudInstance;
        public static List<double> SessionOffsets = new List<double>();
        public static string L(string zh, string en) => Settings.Language == 0 ? zh : en;
        

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            Logger = modEntry.Logger;
            Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            modEntry.OnToggle = (entry, value) => {
                IsEnabled = value;
                if (!value) SessionOffsets.Clear();
                return true;
            };
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;

            new Harmony(modEntry.Info.Id).PatchAll();
            return true;
        }

        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            GUILayout.BeginHorizontal();
            GUIStyle zhStyle = new GUIStyle(GUI.skin.button);
            if (Settings.Language == 0) zhStyle.fontStyle = FontStyle.Bold;
            if (GUILayout.Button("简体中文", zhStyle, GUILayout.Width(100))) Settings.Language = 0;
            GUIStyle enStyle = new GUIStyle(GUI.skin.button);
            if (Settings.Language == 1) enStyle.fontStyle = FontStyle.Bold;
            if (GUILayout.Button("English", enStyle, GUILayout.Width(100))) Settings.Language = 1;

            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            DrawSettingRow(L(Locale_zh.Toggle_Title, Locale_en.Toggle_Title), ref Settings.ShowInSongTitle, ref Settings.Perc1);
            DrawSettingRow(L(Locale_zh.Toggle_Planet, Locale_en.Toggle_Planet), ref Settings.ShowOnPlanet, ref Settings.Perc2);
            if (Settings.ShowOnPlanet)
            {
                GUILayout.Label(L(Locale_zh.Setting_Title, Locale_en.Setting_Title));

                Settings.ReplaceFailOverload = GUILayout.Toggle(Settings.ReplaceFailOverload, L(Locale_zh.Toggle_FailOverload, Locale_en.Toggle_FailOverload));
                Settings.ReplaceTooEarly = GUILayout.Toggle(Settings.ReplaceTooEarly, L(Locale_zh.Toggle_TooEarly, Locale_en.Toggle_TooEarly));
                Settings.ReplaceVeryEarly = GUILayout.Toggle(Settings.ReplaceVeryEarly, L(Locale_zh.Toggle_VeryEarly, Locale_en.Toggle_VeryEarly));
                Settings.ReplaceEarlyPerfect = GUILayout.Toggle(Settings.ReplaceEarlyPerfect, L(Locale_zh.Toggle_EarlyPerfect, Locale_en.Toggle_EarlyPerfect));
                Settings.ReplacePerfect = GUILayout.Toggle(Settings.ReplacePerfect, L(Locale_zh.Toggle_Perfect, Locale_en.Toggle_Perfect));
                Settings.ReplaceLatePerfect = GUILayout.Toggle(Settings.ReplaceLatePerfect, L(Locale_zh.Toggle_LatePerfect, Locale_en.Toggle_LatePerfect));
                Settings.ReplaceVeryLate = GUILayout.Toggle(Settings.ReplaceVeryLate, L(Locale_zh.Toggle_VeryLate, Locale_en.Toggle_VeryLate));
                Settings.ReplaceTooLate = GUILayout.Toggle(Settings.ReplaceTooLate, L(Locale_zh.Toggle_TooLate, Locale_en.Toggle_TooLate));
                Settings.ReplaceFailMiss = GUILayout.Toggle(Settings.ReplaceFailMiss, L(Locale_zh.Toggle_FailMiss, Locale_en.Toggle_FailMiss));
                Settings.ReplaceMultipress = GUILayout.Toggle(Settings.ReplaceMultipress, L(Locale_zh.Toggle_Multipress, Locale_en.Toggle_Multipress));
            }
            DrawSettingRow(L(Locale_zh.Toggle_Death, Locale_en.Toggle_Death), ref Settings.ShowOnDeath, ref Settings.Perc3);
            DrawSettingRow(L(Locale_zh.Toggle_Win, Locale_en.Toggle_Win), ref Settings.ShowInWinPage, ref Settings.Perc4);

            Settings.ShowTimingHUD = GUILayout.Toggle(Settings.ShowTimingHUD, L(Locale_zh.Toggle_TimingHUD, Locale_en.Toggle_TimingHUD));
            if (Settings.ShowTimingHUD)
            {
                GUILayout.Label(L(Locale_zh.Title_TimingHUD, Locale_en.Title_TimingHUD));

                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label(L(Locale_zh.Label_XOffset, Locale_en.Label_XOffset) + $"{Settings.HUD_x:F2}", GUILayout.Width(120));
                Settings.HUD_x = GUILayout.HorizontalSlider(Settings.HUD_x, -0.5f, 0.5f, GUILayout.Width(120));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label(L(Locale_zh.Label_YOffset, Locale_en.Label_YOffset) + $"{Settings.HUD_y:F2}", GUILayout.Width(120));
                Settings.HUD_y = GUILayout.HorizontalSlider(Settings.HUD_y, -0.5f, 0.5f, GUILayout.Width(120));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label(L(Locale_zh.Label_Scale, Locale_en.Label_Scale) + $"{Settings.HUD_scale:F2}", GUILayout.Width(120));
                Settings.HUD_scale = GUILayout.HorizontalSlider(Settings.HUD_scale, 0.2f, 3.0f, GUILayout.Width(120));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                Settings.HUD_bold = GUILayout.Toggle(Settings.HUD_bold, L(Locale_zh.Toggle_Bold, Locale_en.Toggle_Bold));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label(L(Locale_zh.Label_Align, Locale_en.Label_Align), GUILayout.Width(100));

                GUIStyle leftStyle = new GUIStyle(GUI.skin.button);
                if (Settings.HUD_align == 0) leftStyle.fontStyle = FontStyle.Bold;
                if (GUILayout.Button(L(Locale_zh.Btn_Left, Locale_en.Btn_Left), leftStyle, GUILayout.Width(60))) Settings.HUD_align = 0;

                GUIStyle centerStyle = new GUIStyle(GUI.skin.button);
                if (Settings.HUD_align == 1) centerStyle.fontStyle = FontStyle.Bold;
                if (GUILayout.Button(L(Locale_zh.Btn_Center, Locale_en.Btn_Center), centerStyle, GUILayout.Width(60))) Settings.HUD_align = 1;

                GUIStyle rightStyle = new GUIStyle(GUI.skin.button);
                if (Settings.HUD_align == 2) rightStyle.fontStyle = FontStyle.Bold;
                if (GUILayout.Button(L(Locale_zh.Btn_Right, Locale_en.Btn_Right), rightStyle, GUILayout.Width(60))) Settings.HUD_align = 2;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label(L(Locale_zh.Label_Format, Locale_en.Label_Format), GUILayout.Width(100));
                Settings.HUD_Format = GUILayout.TextField(Settings.HUD_Format, GUILayout.Width(200));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label(L(Locale_zh.Label_Precision, Locale_en.Label_Precision) + $"{Settings.PercHUD}", GUILayout.Width(120));
                Settings.PercHUD = Mathf.RoundToInt(GUILayout.HorizontalSlider(Settings.PercHUD, 0, 5, GUILayout.Width(100)));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                Settings.HUD_UseJudgeColor =GUILayout.Toggle(Settings.HUD_UseJudgeColor, L(Locale_zh.HUD_UseJudgeColor, Locale_en.HUD_UseJudgeColor));
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(15);
            if (GUILayout.Button(L(Locale_zh.Btn_Reset, Locale_en.Btn_Reset), GUILayout.Width(150)))
            {
                SessionOffsets.Clear();
                LastTiming = 0;
            }
        }

        static void DrawSettingRow(string label, ref bool toggle, ref int precision)
        {
            toggle = GUILayout.Toggle(toggle, label);
            if (toggle)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                string precisionLabel = L(Locale_zh.Label_Precision, Locale_en.Label_Precision);
                GUILayout.Label($"{precisionLabel}{precision}", GUILayout.Width(120));
                precision = Mathf.RoundToInt(GUILayout.HorizontalSlider(precision, 0, 5, GUILayout.Width(100)));
                GUILayout.EndHorizontal();
            }
        }

        static void OnSaveGUI(UnityModManager.ModEntry modEntry) => Settings.Save(modEntry);

        public static bool IsPlaying() => scrController.instance != null && scrController.instance.state == States.PlayerControl;
        public static string Format(double val, int precision) => $"{val.ToString("F" + precision)}ms";

        public static void UpdateHUD()
        {
            bool isplay = scrController.instance != null && scrConductor.instance != null && scrConductor.instance.isGameWorld && !scrController.instance.paused;

            if (!Settings.ShowTimingHUD) isplay = false;

            if (hudObject == null)
            {
                hudObject = new GameObject("TimingShow_HUD");
                Object.DontDestroyOnLoad(hudObject);
                hudInstance = hudObject.AddComponent<TextUI>();
            }
            hudObject.SetActive(isplay);

            if (!isplay) return;

            string timing = LastTiming.ToString("F" + Settings.PercHUD);
            if (Settings.HUD_UseJudgeColor)
            {
                timing = $"<color=#{ColorUtility.ToHtmlStringRGB(LastTimingColor)}>" + timing + "</color>";
            }
            hudInstance.SetText(string.Format(Settings.HUD_Format, timing));
            hudInstance.SetPosition(Settings.HUD_x,Settings.HUD_y);
            hudInstance.SetSize((int)(24 * Settings.HUD_scale));          
            hudInstance.text.alignment = hudInstance.ToAlign(Settings.HUD_align);
            hudInstance.text.fontStyle = Settings.HUD_bold? FontStyle.Bold: FontStyle.Normal;
        }

    }


}