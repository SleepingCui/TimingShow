using System;

namespace TimingShow.Patches
{
    public static class EditorReloadPatch
    {
        public static void TriggerEditorReload()
        {
            try
            {
                if (!ADOBase.isLevelEditor) return;
                ADOBase.RestartScene();
            }
            catch (Exception e)
            {
                Main.Logger.Error($"[TimingShow] Failed to reload editor level: {e.Message}");
            }
        }
    }
}