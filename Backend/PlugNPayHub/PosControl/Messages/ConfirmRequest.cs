namespace PlugNPayHub.PosControl.Messages
{
    class ConfirmRequest
    {
        public string TransactionId { get; set; }
        public long Amount { get; set; }
    }
}
