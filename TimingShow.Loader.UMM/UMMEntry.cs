using UnityModManagerNet;

namespace TimingShow.UMM
{
    public static class UMMEntry
    {
        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            return Boot.Run(modEntry);
        }
    }
}
