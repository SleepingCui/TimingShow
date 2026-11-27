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
                return RepairPage.Start(modEntry, integrity);
            }

            if (!File.Exists(Path.Combine(modEntry.Path, RepairInstaller.CoreFileName))) return RepairPage.Start(modEntry, null);
            try
            {
                return StartCore(modEntry);
            }
            catch (TypeLoadException e)
            {
                modEntry.Logger.Error("[Repair] The installed TimingShow core is incompatible with this loader: " + e.Message);
                return RepairPage.StartAfterLoadFailure(modEntry, null, e);
            }
            catch (BadImageFormatException e)
            {
                modEntry.Logger.Error("[Repair] The installed TimingShow core is corrupted: " + e.Message);
                return RepairPage.StartAfterLoadFailure(modEntry, null, e);
            }
            catch (Exception e)
            {
                modEntry.Logger.Error("[Repair] TimingShow failed to start: " + e);
                return RepairPage.StartAfterLoadFailure(modEntry, null, e);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static bool StartCore(UnityModManager.ModEntry modEntry)
        {
            return ModBootstrap.Load(modEntry);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void EnableCore()
        {
            TimingShow.ModEntry.Enable();
        }
    }
}
