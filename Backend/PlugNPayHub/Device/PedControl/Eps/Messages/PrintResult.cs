using PlugNPayHub.Utils;

namespace PlugNPayHub.Device.PedControl.Eps.Messages
{
    public class PrintResult
    {
        public string ReceiptId { get; set; }
        public string DocumentNumber { get; set; }

        public static PrintResult Create(string receiptId, string documentNumber)
        {
            Ensure.NotNull(receiptId, nameof(receiptId));
            Ensure.NotNull(documentNumber, nameof(documentNumber));

            return new PrintResult { DocumentNumber = documentNumber, ReceiptId = receiptId };
        }

        public byte[] Serialize(byte[] apak, string posId)
        {
            Format0Writer cw = new Format0Writer();
            cw.WriteStart("PrintResult");
            cw.WriteData("id", ReceiptId);
            cw.WriteData("docnr", DocumentNumber);
            cw.WriteData("result", "OK");
            cw.WriteEnd();

            byte[] message = AsyncPosPacket.PrependHeader(0x00, AsyncPosPacket.GenRandomId(8), posId, cw.ToArray());
            AsyncPosPacket.SetSignature(message, 0, apak);

            return message;
        }
    }
}
