using System;
using Newtonsoft.Json;
using UnityEngine;
using System.IO;

namespace TimingShow
{
    public class Settings
    {
        public bool ShowInSongTitle;
        public bool Title_UseJudgeColor;
        public bool Title_ShowAngle;
        public bool Title_Bump;
        public int Title_FontSize = 100;

        public bool ShowOnPlanet;
        public bool Planet_ShowAngle;
        public int Planet_FontSize = 100;
        
        public bool ShowOnDeath;
        public bool ShowOnDeath_ShowAvgTiming = true;
        public bool ShowOnDeath_ShowUR = true;
        public bool ShowOnDeath_ShowXACC = true;
        public bool ShowOnDeath_ShowRatio = true;
        public int ShowOnDeath_FontSize = 60;
        
        public bool ShowInWinPage;
        public bool ShowInWinPage_ShowAvgTiming = true;
        public bool ShowInWinPage_ShowUR = true;
        public bool ShowInWinPage_ShowRatio = true;
        public int ShowInWinPage_FontSize = 60;
        
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
        
        public bool ReplacePerfectMinus = true;
        public bool ReplaceXPerfect = true;
        public bool ReplacePerfectPlus = true;
        public bool ReplaceAuto;
        public bool ReplaceOverPress = true;

        public bool ShowTimingHUD;
        public float HUD_x;
        public float HUD_y;
        public float HUD_scale = 1.0f;
        public bool HUD_bold;
        public int HUD_align;
        public int PercHUD = 1;
        public string HUD_Format = "Timing - {0}ms";
        public bool HUD_UseJudgeColor;
        public bool HUD_ShowAngle;
        public bool HUD_Bump;
        public bool HUD_UseCustomFont;
        public string HUD_FontPath = "";

        public bool ShowAvgHUD;
        public float AvgHUD_x;
        public float AvgHUD_y = -0.025f;
        public float AvgHUD_scale = 1.0f;
        public bool AvgHUD_bold;
        public int AvgHUD_align;
        public int PercAvgHUD = 1;
        public string AvgHUD_Format = "Avg - {0}ms";
        public bool AvgHUD_Bump;
        public bool AvgHUD_UseCustomFont;
        public string AvgHUD_FontPath = "";

        public bool ShowURHUD;
        public float URHUD_x;
        public float URHUD_y = -0.05f;
        public float URHUD_scale = 1.0f;
        public bool URHUD_bold;
        public int URHUD_align;
        public int PercURHUD = 1;
        public string URHUD_Format = "UR - {0}";
        public bool URHUD_UseCustomFont;
        public string URHUD_FontPath = "";
        public bool URHUD_Bump;

        public bool ShowRatioHUD;
        public float RatioHUD_x;
        public float RatioHUD_y = -0.10f;
        public float RatioHUD_scale = 1.0f;
        public bool RatioHUD_bold;
        public int RatioHUD_align;
        public int PercRatioHUD = 1;
        public string RatioHUD_Format = "Ratio - {0}:1";
        public bool RatioHUD_UseCustomFont;
        public string RatioHUD_FontPath = "";
        public bool RatioHUD_Bump;
        
        public bool Ratio_UseXPerfect;
        
        public int Ratio_Mode = -1;

        public const int RatioMode_NormalPerfect = 0;
        public const int RatioMode_PerfectFamily = 1;
        public const int RatioMode_XPerfect = 2;

        public bool ShowXACCGraph;
        public bool XACCGraph_ShowEnd;
        public bool XACCGraph_ShowPerfInfo;
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
        public bool XACCGraph_UseCustomFont;
        public string XACCGraph_FontPath = "";

        public bool ShowTimingScatter;
        public bool TimingScatter_ShowEnd;
        public float TimingScatter_X = 0.05f;
        public float TimingScatter_Y = 0.20f;
        public float TimingScatter_Width = 320f;
        public float TimingScatter_Height = 140f;
        public float TimingScatter_Scale = 1.0f;
        public int TimingScatter_SampleCount = 200;
        public int TimingScatter_MaxRenderPoints = 800;
        public float TimingScatter_PointSize = 3f;
        public bool TimingScatter_UseHitAxis;
        public bool TimingScatter_AutoScroll;
        public bool TimingScatter_UseJudgeColor = true;
        public bool TimingScatter_ShowZeroLine = true;
        public bool TimingScatter_ShowAvgLine = true;
        public bool TimingScatter_IgnoreOutliers;
        public bool TimingScatter_ShowPerfInfo;
        public bool TimingScatter_ShowJudgeBands = true;
        public bool TimingScatter_ShowXpBand = true;
        public bool TimingScatter_BandAutoWindow = true;
        public float TimingScatter_BandThresholdBpm = 310f;

        public Color TimingScatter_BgColor = new Color(0f, 0f, 0f, 0.60f);
        public Color TimingScatter_GridColor = new Color(1f, 1f, 1f, 0.35f);
        public Color TimingScatter_ZeroLineColor = new Color(1f, 1f, 1f, 0.65f);
        public Color TimingScatter_AvgLineColor = new Color(1f, 0.85f, 0.20f, 0.95f);
        public Color TimingScatter_PointColor = new Color(0.30f, 0.76f, 1f, 1f);
        public Color TimingScatter_AxisTextColor = new Color(0.80f, 0.80f, 0.80f, 1f);
        public Color TimingScatter_BandPerfectColor = new Color(0.25f, 0.90f, 0.35f, 0.14f);
        public Color TimingScatter_BandElPerfectColor = new Color(1f, 0.85f, 0.20f, 0.12f);
        public Color TimingScatter_BandEarlyLateColor = new Color(0.95f, 0.40f, 0.30f, 0.10f);
        // XPerfect 色带：RGB 取游戏 colourXPerfect 的回退值 Color32(77, 204, 255)，alpha 与判定色带一致
        public Color TimingScatter_BandXpColor = new Color(77f / 255f, 204f / 255f, 1f, 0.14f);
        public bool TimingScatter_UseCustomFont;
        public string TimingScatter_FontPath = "";

        public bool Title_EnableXPerfect;
        public bool Planet_EnableXPerfect;
        public bool HUD_EnableXPerfect;
        public bool Logger_EnableXPerfect;

        public bool EnableLogging;
        public bool LogAutoplay;
        public bool Logger_ShowAngle;
        public string LogDirectory = Path.Combine(Application.dataPath, "../Mods/TimingShow/Logs");
        public int PercLog = 4;
        public int LogBufferSizeKB = 64;

        public bool UseHookMode;
        public bool UseJsonWriter;
        public bool AutoReloadInEditor;

        public bool AnalyzerBridgeEnabled = true;
        public int AnalyzerBridgePort;
        public int AnalyzerBridgeTimeoutSec = LogAnalyzerBridge.DefaultTimeoutSeconds;

        public int LogSort = 0;

        public const int LogSort_Time = 0;
        public const int LogSort_Size = 1;
        public const int LogSort_SongName = 2;

        
        //ml only
        public KeyCode ConfigKey = KeyCode.F9;
        
        
        
        public const int CurrentSettingsVersion = 2;
        public int SettingsVersion;

        #region cfgsettings
        
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Converters = { new ColorConverter() }
        };

        public static Settings Load(string modPath)
        {
            string filePath = Path.Combine(modPath, "Settings.json");
            try
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    var settings = JsonConvert.DeserializeObject<Settings>(json, JsonSettings);
                    if (settings != null)
                    {
                        if (settings.ConfigKey == KeyCode.None)
                            settings.ConfigKey = KeyCode.F9;
                        bool isLegacyConfig = json.IndexOf("settingsVersion", StringComparison.OrdinalIgnoreCase) < 0;
                        settings.Migrate(isLegacyConfig);
                        settings.Sanitize();
                        return settings;
                    }
                }
            }
            catch (Exception e)
            {
                ModContext.Logger?.Error($"Failed to load settings: {e.Message}");
            }
            return new Settings
            {
                SettingsVersion = CurrentSettingsVersion,
                Ratio_Mode = RatioMode_NormalPerfect
            };
        }
        
        private void Migrate(bool isLegacyConfig)
        {
            bool migrated = false;

            if (isLegacyConfig || SettingsVersion < 1)
            {
                ReplacePerfectMinus = ReplacePerfect;
                ReplacePerfectPlus = ReplacePerfect;
                ReplaceXPerfect = Planet_EnableXPerfect;
                ReplaceOverPress = ReplaceFailMiss;
                ReplaceAuto = false;

                migrated = true;
            }

            if (SettingsVersion < 2)
            {
                if (Mathf.Approximately(TimingScatter_BandXpColor.r, 77f / 255f) &&
                    Mathf.Approximately(TimingScatter_BandXpColor.g, 204f / 255f) &&
                    Mathf.Approximately(TimingScatter_BandXpColor.b, 1f) &&
                    Mathf.Approximately(TimingScatter_BandXpColor.a, 1f))
                {
                    TimingScatter_BandXpColor.a = TimingScatter_BandPerfectColor.a;
                }

                migrated = true;
            }

            if (SettingsVersion < CurrentSettingsVersion)
            {
                SettingsVersion = CurrentSettingsVersion;
                migrated = true;
            }

            if (Ratio_Mode < 0)
            {
                Ratio_Mode = Ratio_UseXPerfect ? RatioMode_XPerfect : RatioMode_NormalPerfect;
                migrated = true;
            }

            if (migrated)
            {
                ModContext.Logger?.Log("Settings migrated to ver " + CurrentSettingsVersion);
            }
        }
        
        public void Sanitize()
        {
            XACCGraph_X = Mathf.Clamp01(XACCGraph_X);
            XACCGraph_Y = Mathf.Clamp01(XACCGraph_Y);
            XACCGraph_Width = Mathf.Clamp(XACCGraph_Width, 80f, 1200f);
            XACCGraph_Height = Mathf.Clamp(XACCGraph_Height, 40f, 600f);
            XACCGraph_Scale = Mathf.Clamp(XACCGraph_Scale, 0.2f, 3f);

            TimingScatter_X = Mathf.Clamp01(TimingScatter_X);
            TimingScatter_Y = Mathf.Clamp01(TimingScatter_Y);
            TimingScatter_Width = Mathf.Clamp(TimingScatter_Width, 80f, 1200f);
            TimingScatter_Height = Mathf.Clamp(TimingScatter_Height, 40f, 600f);
            TimingScatter_Scale = Mathf.Clamp(TimingScatter_Scale, 0.2f, 3f);
            TimingScatter_SampleCount = Mathf.Clamp(TimingScatter_SampleCount, 10, 20000);
            TimingScatter_MaxRenderPoints = Mathf.Clamp(TimingScatter_MaxRenderPoints, 20, 8000);
            TimingScatter_PointSize = Mathf.Clamp(TimingScatter_PointSize, 1f, 12f);
            TimingScatter_BandThresholdBpm = Mathf.Clamp(TimingScatter_BandThresholdBpm, 100f, 600f);
        }

        public void Save(string modPath)
        {
            try
            {
                string filePath = Path.Combine(modPath, "Settings.json");
                string json = JsonConvert.SerializeObject(this, Formatting.Indented, JsonSettings);
                File.WriteAllText(filePath, json);
            }
            catch (Exception e)
            {
                ModContext.Logger?.Error($"Failed to save settings: {e.Message}");
            }
        }
            
        #endregion
    }
}

