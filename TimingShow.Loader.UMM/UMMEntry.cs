using System.Runtime.CompilerServices;
using UnityModManagerNet;

namespace TimingShow.UMM
{
    public static class UMMEntry
    {
        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            PendingUpdate.Apply(modEntry);
            return StartCore(modEntry);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool StartCore(UnityModManager.ModEntry modEntry)
        {
            return ModBootstrap.Load(modEntry);
        }
    }
}
