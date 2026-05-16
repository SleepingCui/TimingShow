using UnityModManagerNet;

public class Settings : UnityModManager.ModSettings
{
    public bool ShowInSongTitle = true;
    public bool ShowOnPlanet = true;
    public bool ShowOnDeath = true;
    public bool ShowInWinPage = true;
    public bool Title_UseJudgeColor = false;
    public int Perc1 = 1;
    public int Perc2 = 1;
    public int Perc3 = 1;
    public int Perc4 = 2;
    public int Language = 0;

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

    public override void Save(UnityModManager.ModEntry modEntry) => Save(this, modEntry);
}