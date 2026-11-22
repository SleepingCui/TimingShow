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
            StartupIntegrityReport integrity = StartupIntegrity.Check(modEntry.Path, null, false, StartupIntegrity.ResolveLoaderFileName(modEntry.Path, "TimingShow.Loader.UMM.dll"), Path.Combine(modEntry.Path, "Settings.json"));
            if (integrity.NeedsRepair)
            {
                modEntry.Logger.Warning("[Integrity] TimingShow needs a repair: " + integrity.Summary);
                return MissingCoreFallback.Start(modEntry, integrity);
            }

            if (!File.Exists(Path.Combine(modEntry.Path, MissingCoreInstaller.CoreFileName))) return MissingCoreFallback.Start(modEntry, null);
            try
            {
                return StartCore(modEntry);
            }
            catch (TypeLoadException e)
            {
                modEntry.Logger.Error("[MissingCore] The installed TimingShow core is incompatible with this loader: " + e.Message);
                return MissingCoreFallback.Start(modEntry, null);
            }
            catch (BadImageFormatException e)
            {
                modEntry.Logger.Error("[MissingCore] The installed TimingShow core is corrupted: " + e.Message);
                return MissingCoreFallback.Start(modEntry, null);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static bool StartCore(UnityModManager.ModEntry modEntry)
        {
            return ModBootstrap.Load(modEntry);
        }
    }
}
