using PlugNPayHub.Utils;

namespace PlugNPayHub.Device.PedControl.Eps
{
    public class Receipt
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public Flags Flags { get; set; }
        public bool IsReversalReceipt { get; set; }

        public Receipt()
        {
            Flags = new Flags();
        }
    }
}
