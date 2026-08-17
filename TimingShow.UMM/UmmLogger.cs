using UnityModManagerNet;

namespace TimingShow
{
    public class UmmLogger : IModLogger
    {
        private readonly UnityModManager.ModEntry.ModLogger _logger;

        public UmmLogger(UnityModManager.ModEntry.ModLogger logger)
        {
            _logger = logger;
        }

        public void Log(string msg)
        {
            _logger.Log(msg);
        }

        public void Error(string msg)
        {
            _logger.Error(msg);
        }
    }
}
