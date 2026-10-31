namespace TimingShow
{
    public interface IModLogger
    {
        void Log(string msg);
        void Error(string msg);
    }

    public static class ModLog
    {
        public static void UnityLog(string msg)
        {
            UnityEngine.Debug.Log("[TimingShow] " + msg);
        }
    }
}
