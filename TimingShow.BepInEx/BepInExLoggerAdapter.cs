using BepInEx.Logging;

namespace TimingShow
{
    public class BepInExLoggerAdapter : IModLogger
    {
        private readonly ManualLogSource _logger;

        public BepInExLoggerAdapter(ManualLogSource logger)
        {
            _logger = logger;
        }

        public void Log(string msg)
        {
            _logger.LogInfo(msg);
        }

        public void Error(string msg)
        {
            _logger.LogError(msg);
        }
    }
}
