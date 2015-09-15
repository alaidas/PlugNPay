using System;
using System.Globalization;
using PlugNPayHub.Utils;

namespace PlugNPayHub.Device.PedControl.Eps.Messages
{
    public class AdjustRequest
    {
        public long Amount { get; set; }
        public string AuthorizationId { get; set; }
        public string DocumentNumber { get; set; }
        public DateTime Time { get; set; }

        public byte[] Serialize(byte[] apak, string posId)
        {
            Format0Writer cw = new Format0Writer();
            cw.WriteStart("Adjust");
            cw.WriteData("amount", Amount.ToString(CultureInfo.InvariantCulture));
            cw.WriteData("authid", AuthorizationId);
            cw.WriteData("docnr", DocumentNumber);
            cw.WriteData("time", Time.ToString("yyyy-MM-dd HH:mm:ss"));
            cw.WriteEnd();

            byte[] message = AsyncPosPacket.PrependHeader(0x00, AsyncPosPacket.GenRandomId(8), posId, cw.ToArray());
            AsyncPosPacket.SetSignature(message, 0, apak);

            return message;
        }

        public static AdjustRequest Create(string documentNumber, long amount, string authorizationId)
        {
            Ensure.NotNull(documentNumber, nameof(documentNumber));
            Ensure.Positive(amount, nameof(amount));
            Ensure.NotNull(authorizationId, nameof(authorizationId));

            return new AdjustRequest
            {
                Amount = amount,
                AuthorizationId = authorizationId,
                DocumentNumber = documentNumber,
                Time = DateTime.Now
            };
        }
    }
}
