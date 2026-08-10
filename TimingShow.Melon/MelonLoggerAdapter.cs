using MelonLoader;

namespace TimingShow
{
    public class MelonLoggerAdapter : IModLogger
    {
        public void Log(string msg)
        {
            MelonLogger.Msg(msg);
        }

        public void Error(string msg)
        {
            MelonLogger.Error(msg);
        }
    }
}
