using System;
using System.IO;
using System.Runtime.CompilerServices;
using TimingShow.Bootstrap;
using UnityModManagerNet;

namespace TimingShow.UMM
{
    public static class UMMEntry
    {
        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            PendingUpdate.Apply(modEntry);
            if (!File.Exists(Path.Combine(modEntry.Path, MissingCoreInstaller.CoreFileName))) return MissingCoreFallback.Start(modEntry);
            try
            {
                return StartCore(modEntry);
            }
            catch (TypeLoadException e)
            {
                modEntry.Logger.Error("[MissingCore] The installed TimingShow core is incompatible with this loader: " + e.Message);
                return MissingCoreFallback.Start(modEntry);
            }
            catch (BadImageFormatException e)
            {
                modEntry.Logger.Error("[MissingCore] The installed TimingShow core is corrupted: " + e.Message);
                return MissingCoreFallback.Start(modEntry);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static bool StartCore(UnityModManager.ModEntry modEntry)
        {
            return ModBootstrap.Load(modEntry);
        }
    }
}
