using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityModManagerNet;

namespace TimingShow
{
    public class Settings : UnityModManager.ModSettings
    {
        public bool ShowInSongTitle = true;
        public bool ShowOnPlanet = true;
        public bool ShowOnDeath = true;
        public bool ShowInWinPage = true;
        public int Perc1 = 1;
        public int Perc2 = 1;
        public int Perc3 = 1;
        public int Perc4 = 2;
        public int Language = 0;

        public override void Save(UnityModManager.ModEntry modEntry) => Save(this, modEntry);
    }

    public static class Main
    {
        public static UnityModManager.ModEntry.ModLogger Logger;
        public static bool IsEnabled;
        public static Settings Settings;
        public static double LastTiming = 0;
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
            GUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal();
            GUILayout.Label(("LANG"),GUILayout.Width(150));
            GUIStyle zhStyle = new GUIStyle(GUI.skin.button);
            if (Settings.Language == 0) zhStyle.fontStyle = FontStyle.Bold;
            if (GUILayout.Button("简体中文", zhStyle, GUILayout.Width(100))) Settings.Language = 0;
            GUIStyle enStyle = new GUIStyle(GUI.skin.button);
            if (Settings.Language == 1) enStyle.fontStyle = FontStyle.Bold;
            if (GUILayout.Button("English", enStyle, GUILayout.Width(100))) Settings.Language = 1;

            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            DrawSettingRow(L(Locale_zh.Toggle_Title, Locale_en.Toggle_Title), ref Settings.ShowInSongTitle, ref Settings.Perc1);
            DrawSettingRow(L(Locale_zh.Toggle_Planet, Locale_en.Toggle_Planet),ref Settings.ShowOnPlanet, ref Settings.Perc2);
            DrawSettingRow(L(Locale_zh.Toggle_Death, Locale_en.Toggle_Death),ref Settings.ShowOnDeath, ref Settings.Perc3);
            DrawSettingRow(L(Locale_zh.Toggle_Win, Locale_en.Toggle_Win), ref Settings.ShowInWinPage, ref Settings.Perc4);

            GUILayout.Space(15);
            if (GUILayout.Button(L(Locale_zh.Btn_Reset, Locale_en.Btn_Reset), GUILayout.Width(150)))
            {
                SessionOffsets.Clear();
                LastTiming = 0;
            }
            GUILayout.EndVertical();
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
    }
}