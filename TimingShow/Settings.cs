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

    public bool ShowURHUD = false;
    public float URHUD_x = 0f;
    public float URHUD_y = -0.05f; 
    public float URHUD_scale = 1.0f;
    public bool URHUD_bold = false;
    public int URHUD_align = 0;
    public int PercURHUD = 1;
    public string URHUD_Format = "UR - {0}";

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
    public bool UseBinaryWriter = false;

    public bool ShowDebugInfo = false;
    public int DebugUpdateIntervalMs = 500;

    public bool ShowURGraph = false;
    public float URGraph_X = 0.05f;        
    public float URGraph_Y = 0.70f;         
    public float URGraph_Width = 260f;     
    public float URGraph_Height = 100f;      
    public float URGraph_Scale = 1.0f;     
    public int URGraph_WindowSize = 30;     
    public float URGraph_MaxUR = 150f;     
    public int URGraph_MaxPoints = 100;

    public Color URGraph_BgColor = new Color(0f, 0f, 0f, 0.5f);
    public Color URGraph_LineColor = new Color(0.2f, 0.8f, 1f, 1f);
    public Color URGraph_GridColor = new Color(1f, 1f, 1f, 0.25f);
    public Color URGraph_TextColor = new Color(1f, 1f, 1f, 0.8f);



    public bool ShowXACCGraph = false;
    public float XACCGraph_X = 0.05f;    
    public float XACCGraph_Y = 0.50f;         
    public float XACCGraph_Width = 260f;     
    public float XACCGraph_Height = 100f;     
    public float XACCGraph_Scale = 1.0f;      
    public int XACCGraph_MaxPoints = 250;     

    public Color XACCGraph_TextColor = new Color(1f, 1f, 1f, 0.8f);
    public Color XACCGraph_BgColor = new Color(0f, 0f, 0f, 0.6f);
    public Color XACCGraph_LineColor = new Color(0.2f, 0.9f, 0.3f, 1f);
    public Color XACCGraph_GridColor = new Color(1f, 1f, 1f, 0.15f);     
    public Color XACCGraph_AxisTextColor = new Color(0.8f, 0.8f, 0.8f, 1f);  
    public Color XACCGraph_ValueTextColor = new Color(1f, 0.9f, 0.3f, 1f); 



    public override void Save(UnityModManager.ModEntry modEntry) => Save(this, modEntry);
}