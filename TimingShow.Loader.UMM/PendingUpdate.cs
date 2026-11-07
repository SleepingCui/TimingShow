using System.IO;
using UnityModManagerNet;
using TimingShow.Update;

namespace TimingShow.UMM
{
    internal static class PendingUpdate
    {
        public static bool Apply(UnityModManager.ModEntry modEntry)
        {
            string updateRoot = Path.Combine(modEntry.Path, PendingUpdateApplier.UpdateDirectoryName);
            return PendingUpdateApplier.Apply(updateRoot, modEntry.Path, message => modEntry.Logger.Log(message), message => modEntry.Logger.Error(message));
        }
    }
}
