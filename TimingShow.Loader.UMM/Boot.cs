using System.Runtime.CompilerServices;
using UnityModManagerNet;

namespace TimingShow.UMM
{
    internal static class Boot
    {
        public static bool Run(UnityModManager.ModEntry modEntry)
        {
            PendingUpdate.Apply(modEntry);
            return StartCore(modEntry);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool StartCore(UnityModManager.ModEntry modEntry)
        {
            return CoreStart.Load(modEntry);
        }
    }
}
