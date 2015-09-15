using PlugNPay.Utils;
using PlugNPay.Utils.Logs;

namespace PlugNPayClient.Controller
{
    public class ControllerContext
    {
        public ILog Log { get; private set; }
        public Attributes StartupAttributes { get; private set; }

        public ControllerContext(ILog log, Attributes startupAttributes)
        {
            Ensure.NotNull(log, nameof(log));

            Log = log;
            StartupAttributes = startupAttributes ?? new Attributes();
        }
    }
}
