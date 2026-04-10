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

        public override void Save(UnityModManager.ModEntry modEntry) => Save(this, modEntry);
    }

    public static class Main
    {
        public static UnityModManager.ModEntry.ModLogger Logger;
        public static bool IsEnabled;
        public static Settings Settings;
        public static double LastTiming = 0;
        public static List<double> SessionOffsets = new List<double>();

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
            DrawSettingRow("替换标题", ref Settings.ShowInSongTitle, ref Settings.Perc1);
            DrawSettingRow("替换判定显示 (需重进编辑器)", ref Settings.ShowOnPlanet, ref Settings.Perc2);
            DrawSettingRow("在玩家死亡时显示", ref Settings.ShowOnDeath, ref Settings.Perc3);
            DrawSettingRow("在结算界面显示", ref Settings.ShowInWinPage, ref Settings.Perc4);

            GUILayout.Space(15);
            if (GUILayout.Button("重置统计数据", GUILayout.Width(150)))
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
                GUILayout.Label($"小数位数: {precision}", GUILayout.Width(120));
                precision = Mathf.RoundToInt(GUILayout.HorizontalSlider(precision, 0, 5, GUILayout.Width(100)));
                GUILayout.EndHorizontal();
            }
        }

        static void OnSaveGUI(UnityModManager.ModEntry modEntry) => Settings.Save(modEntry);

        public static bool IsPlaying() => scrController.instance != null && scrController.instance.state == States.PlayerControl;

        public static string Format(double val, int precision) => $"{val.ToString("F" + precision)}ms";
    }
}