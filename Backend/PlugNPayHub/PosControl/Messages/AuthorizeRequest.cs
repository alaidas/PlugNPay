namespace PlugNPayHub.PosControl.Messages
{
    class AuthorizeRequest
    {
        public string TransactionId { get; set; }
        public long Amount { get; set; }
        public long Cash { get; set; }
        public int Currency { get; set; }
        public string Language { get; set; }
        public string Last4CardNumberDigits { get; set; }
        public bool PreAuthorize { get; set; }
    }
}
