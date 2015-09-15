using System;

namespace PlugNPay.Utils.Logs
{
    public interface ILog
    {
        void LogError(Exception ex);
        void LogError(string message);
        void LogWarning(string message);
        void LogMessage(string message);
    }
}
