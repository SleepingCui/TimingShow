using UnityModManagerNet;

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

    public bool ReplaceTooEarly = false;
    public bool ReplaceVeryEarly = false;
    public bool ReplaceEarlyPerfect = false;
    public bool ReplacePerfect = true;
    public bool ReplaceLatePerfect = false;
    public bool ReplaceVeryLate = false;
    public bool ReplaceTooLate = false;
    public bool ReplaceMultipress = false;
    public bool ReplaceFailMiss = false;
    public bool ReplaceFailOverload = false;

    public bool ShowTimingHUD = false;
    public float HUD_x = 0f;
    public float HUD_y = 0f;
    public float HUD_scale = 1.0f;
    public bool HUD_bold = false;
    public int HUD_align = 0;
    public int PercHUD = 1;
    public string HUD_Format = "Timing - {0}ms";

    public override void Save(UnityModManager.ModEntry modEntry) => Save(this, modEntry);
}