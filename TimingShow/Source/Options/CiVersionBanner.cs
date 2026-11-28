using TimingShow.Update;

namespace TimingShow.Options
{
    internal static class CiVersionBanner
    {
        internal static string Text()
        {
#if CI_VERSION
            string version = UpdateBridge.ModVersion;
            if (!string.IsNullOrEmpty(version)) return string.Format(i18n.T("Ci_VersionBanner"), version);
#endif
            return null;
        }
    }
}
