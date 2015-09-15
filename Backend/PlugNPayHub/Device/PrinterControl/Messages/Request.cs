namespace PlugNPayHub.Device.PrinterControl.Messages
{
    public class Request
    {
        public string PrinterId { get; set; }
        public string RequestId { get; set; }
        public string RequestType { get; set; }
        public string Content { get; set; }
    }
}
