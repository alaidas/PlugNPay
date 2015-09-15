using System;

namespace PlugNPayHub.Device.PedControl.Eps
{
    public class AsyncPosException : Exception
    {
        public override string Message { get; }

        public AsyncPosException(string format, params object[] args)
        {
            Message = !string.IsNullOrEmpty(format) ? string.Format(format, args) : "Unknown";
        }
    }
}
