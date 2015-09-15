using System;
using PlugNPayHub.Utils;

namespace PlugNPayHub.Device.PedControl.Eps.Messages
{
    public class PrintReceipt
    {
        public string ReceiptId { get; set; }
        public string DocumentNumber { get; set; }
        public string ReceiptText { get; set; }
        public Flags Flags { get; set; }

        public static PrintReceipt Deserialize(Format0Reader reader)
        {
            Ensure.NotNull(reader, nameof(reader));

            if (reader.RootTag != "printreceipt")
                throw new InvalidOperationException($"Expected PrintReceipt message but received {reader.RootTag} message");

            return new PrintReceipt
            {
                ReceiptId = reader.GetValue("id"),
                DocumentNumber = reader.GetValue("docnr"),
                ReceiptText = reader.GetValue("receipttext"),
                Flags = new Flags(reader.GetValue("flags"))
            };
        }

        public byte[] Serialize(byte[] apak, string posId)
        {
            Format0Writer cw = new Format0Writer();
            cw.WriteStart("PrintReceipt");
            cw.WriteData("id", ReceiptId);
            cw.WriteData("docnr", DocumentNumber);
            cw.WriteData("receipttext", ReceiptText);
            cw.WriteData("flags", Flags.ToString());
            cw.WriteEnd();

            byte[] message = AsyncPosPacket.PrependHeader(0x00, AsyncPosPacket.GenRandomId(8), posId, cw.ToArray());
            AsyncPosPacket.SetSignature(message, 0, apak);

            return message;
        }
    }
}
