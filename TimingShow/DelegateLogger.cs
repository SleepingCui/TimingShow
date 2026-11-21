using System;

namespace TimingShow
{
    internal sealed class DelegateLogger : IModLogger
    {
        private readonly Action<string> _log;
        private readonly Action<string> _error;

        public DelegateLogger(Action<string> log, Action<string> error)
        {
            _log = log;
            _error = error;
        }

        public void Log(string message)
        {
            if (_log != null) _log(message);
        }

        public void Error(string message)
        {
            if (_error != null) _error(message);
        }
    }
}
