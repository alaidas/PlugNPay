namespace PlugNPayHub.Device.PrinterControl.Messages
{
    public enum ResponseResults
    {
        Ok,
        Error,
    }

    public interface IResponse
    {
        ResponseResults Result { get; set; }
        string Text { get; set; }
    }

    public class Response : IResponse
    {
        public ResponseResults Result { get; set; }
        public string Text { get; set; }
    }
}
