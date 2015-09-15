using System;
using NLog;

namespace PlugNPay.Utils.Logs
{
    public class Log : ILog
    {
        private static readonly Logger CurrentLogger = LogManager.GetCurrentClassLogger();

        public void LogError(Exception ex)
        {
            CurrentLogger.Error("\"{0}\" {1}", ex.Message, ex.StackTrace);
        }

        public void LogError(string message)
        {
            CurrentLogger.Error(message);
        }

        public void LogWarning(string message)
        {
            CurrentLogger.Warn(message);
        }

        public void LogMessage(string message)
        {
            CurrentLogger.Info(message);
        }
    }
}
