namespace PlugNPayHub.PosControl.Messages
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

    public interface ITransactionResponse : IResponse
    {
        string TransactionId { get; set; }
    }

    public interface IReceiptResponse
    {
        string ClientReceipt { get; set; }
        string MerchantReceipt { get; set; }
        bool NeedClientSignature { get; set; }
    }

    public class AuthorizeResponse : ITransactionResponse, IReceiptResponse
    {
        public string TransactionId { get; set; }
        public string AuthorizationCode { get; set; }
        public long Amount { get; set; }
        public string Rrn { get; set; }
        public string Stan { get; set; }
        public string CardType { get; set; }

        public string ClientReceipt { get; set; }
        public string MerchantReceipt { get; set; }
        public bool NeedClientSignature { get; set; }

        public ResponseResults Result { get; set; }
        public string Text { get; set; }
    }

    public class ConfirmResponse : ITransactionResponse
    {
        public string TransactionId { get; set; }
        public ResponseResults Result { get; set; }
        public string Text { get; set; }
    }

    public class ConfirmAdjustResponse : ITransactionResponse, IReceiptResponse
    {
        public string TransactionId { get; set; }
        public ResponseResults Result { get; set; }
        public string Text { get; set; }

        public string ClientReceipt { get; set; }
        public string MerchantReceipt { get; set; }
        public bool NeedClientSignature { get; set; }
    }

    public class ReversalResponse : ITransactionResponse, IReceiptResponse
    {
        public string TransactionId { get; set; }

        public string ClientReceipt { get; set; }
        public string MerchantReceipt { get; set; }
        public bool NeedClientSignature { get; set; }

        public ResponseResults Result { get; set; }
        public string Text { get; set; }
    }

    public class Response : IResponse
    {
        public ResponseResults Result { get; set; }
        public string Text { get; set; }
    }
}
