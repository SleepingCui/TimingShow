using Newtonsoft.Json;
using UnityEngine;
using System.IO;

namespace TimingShow
{
    public class Settings
    {
        public bool ShowInSongTitle;
        public bool ShowOnPlanet;
        public bool ShowOnDeath;
        public bool ShowInWinPage;
        public bool Title_UseJudgeColor;
        public int Perc1 = 1;
        public int Perc2 = 1;
        public int Perc3 = 1;
        public int Perc4 = 1;
        public string Language = "English";

        public bool ReplaceTooEarly = true;
        public bool ReplaceVeryEarly = true;
        public bool ReplaceEarlyPerfect = true;
        public bool ReplacePerfect = true;
        public bool ReplaceLatePerfect = true;
        public bool ReplaceVeryLate = true;
        public bool ReplaceTooLate = true;
        public bool ReplaceMultipress = true;
        public bool ReplaceFailMiss = true;
        public bool ReplaceFailOverload = true;

        public bool ShowTimingHUD;
        public float HUD_x;
        public float HUD_y;
        public float HUD_scale = 1.0f;
        public bool HUD_bold;
        public int HUD_align;
        public int PercHUD = 1;
        public string HUD_Format = "Timing - {0}ms";
        public bool HUD_UseJudgeColor;

        public bool ShowURHUD;
        public float URHUD_x;
        public float URHUD_y = -0.05f;
        public float URHUD_scale = 1.0f;
        public bool URHUD_bold;
        public int URHUD_align;
        public int PercURHUD = 1;
        public string URHUD_Format = "UR - {0}";

        public bool ShowRatioHUD;
        public float RatioHUD_x;
        public float RatioHUD_y = -0.10f;
        public float RatioHUD_scale = 1.0f;
        public bool RatioHUD_bold;
        public int RatioHUD_align;
        public int PercRatioHUD = 1;
        public string RatioHUD_Format = "Ratio - {0}:1";
        public bool Ratio_UseXPerfect;

        public bool ShowXACCGraph;
        public bool XACCGraph_ShowEnd;
        public float XACCGraph_X = 0.05f;
        public float XACCGraph_Y = 0.50f;
        public float XACCGraph_Width = 260f;
        public float XACCGraph_Height = 100f;
        public float XACCGraph_Scale = 1.0f;
        public int XACCGraph_MaxPoints = 250;

        public Color XACCGraph_BgColor = new Color(0f, 0f, 0f, 0.6f);
        public Color XACCGraph_LineColor = new Color(0.2f, 0.9f, 0.3f, 1f);
        public Color XACCGraph_GridColor = new Color(1f, 1f, 1f, 1f);
        public Color XACCGraph_AxisTextColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        public Color XACCGraph_ValueTextColor = new Color(1f, 0.9f, 0.3f, 1f);

        public bool Title_EnableXPerfect;
        public bool Planet_EnableXPerfect;
        public bool HUD_EnableXPerfect;
        public bool Logger_EnableXPerfect;

        public bool EnableLogging;
        public bool LogAutoplay;
        public string LogDirectory = Path.Combine(Application.dataPath, "../Mods/TimingShow/Logs");
        public int PercLog = 4;
        public int LogBufferSizeKB = 64;

        public bool UseHookMode;
        public bool DisplayCurrMode;
        public bool UseOldJsonFormat;
        public bool UseBinaryWriter;
        public bool AutoReloadInEditor;

        public KeyCode ConfigKey = KeyCode.F9;

        public static Settings Load(string modPath)
        {
            string filePath = Path.Combine(modPath, "Settings.json");
            try
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    return JsonConvert.DeserializeObject<Settings>(json) ?? new Settings();
                }
            }
            catch
            {
                // corrupted settings, fall through to default
            }
            return new Settings();
        }

        public void Save(string modPath)
        {
            try
            {
                string filePath = Path.Combine(modPath, "Settings.json");
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch
            {
                // silently fail - settings will try again next save
            }
        }
    }
}
