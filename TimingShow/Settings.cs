using UnityModManagerNet;
using UnityEngine;
using System.IO;

public class Settings : UnityModManager.ModSettings
{
    public bool ShowInSongTitle = false;
    public bool ShowOnPlanet = false;
    public bool ShowOnDeath = false;
    public bool ShowInWinPage = false;
    public bool Title_UseJudgeColor = false;
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

    public bool ShowTimingHUD = false;
    public float HUD_x = 0f;
    public float HUD_y = 0f;
    public float HUD_scale = 1.0f;
    public bool HUD_bold = false;
    public int HUD_align = 0;
    public int PercHUD = 1;
    public string HUD_Format = "Timing - {0}ms";
    public bool HUD_UseJudgeColor = false;

    public bool Title_EnableXPerfect = false;
    public bool Planet_EnableXPerfect = false;
    public bool HUD_EnableXPerfect = false;
    public bool Logger_EnableXPerfect = false;

    public bool EnableLogging = false;
    public bool LogAutoplay = false;
    public string LogDirectory = Path.Combine(Application.dataPath, "../Mods/TimingShow/Logs");
    public int PercLog = 4;
    public int LogBufferSizeKB = 64;

    public bool UseHookMode = false;
    public bool DisplayCurrMode = false;
    public bool UseOldJsonFormat = false;

    public override void Save(UnityModManager.ModEntry modEntry) => Save(this, modEntry);
}