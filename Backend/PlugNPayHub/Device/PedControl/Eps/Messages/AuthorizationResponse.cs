using System;
using System.Collections.Generic;
using System.Globalization;
using PlugNPayHub.Utils;

namespace PlugNPayHub.Device.PedControl.Eps.Messages
{
    public class AuthorizationResponse
    {
        public string AuthorizationId { get; set; }
        public string DocumentNumber { get; set; }
        public bool Approved { get; set; }
        public long Amount { get; set; }
        public string Text { get; set; }
        public string CardToken { get; set; }
        public string AuthorizationCode { get; set; }
        public string Rrn { get; set; }
        public string Stan { get; set; }
        public string CardType { get; set; }
        public List<Receipt> Receipts { get; set; }

        public AuthorizationResponse()
        {
            Receipts = new List<Receipt>();
        }

        public static AuthorizationResponse Deserialize(Format0Reader reader)
        {
            Ensure.NotNull(reader, nameof(reader));

            if (reader.RootTag != "authorizationresult")
                throw new InvalidOperationException($"Expected AuthorizationResult message but received {reader.RootTag} message");

            AuthorizationResponse result = new AuthorizationResponse
            {
                AuthorizationId = reader.GetValue("id"),
                DocumentNumber = reader.GetValue("docnr"),
                Approved = reader.GetValue("result") == "OK",
                Text = reader.GetValue("txt"),
                CardToken = reader.GetValue("token"),
                AuthorizationCode = reader.GetValue("authcode"),
                Rrn = reader.GetValue("rrn"),
                Stan = reader.GetValue("stan"),
                CardType = reader.GetValue("cardtype"),
            };

            if (!result.Approved)
                return result;

            string amount = reader.GetValue("amount");
            long amt;
            if (long.TryParse(amount, out amt))
                result.Amount = amt;

            return result;
        }

        public byte[] Serialize(byte[] apak, string posId)
        {
            Format0Writer cw = new Format0Writer();
            cw.WriteStart("AuthorizationResult");
            if (!string.IsNullOrEmpty(AuthorizationId)) cw.WriteData("id", AuthorizationId);
            cw.WriteData("docnr", DocumentNumber);
            cw.WriteData("result", Approved ? "OK" : "ERROR");
            cw.WriteData("amount", Amount.ToString(CultureInfo.InvariantCulture));
            cw.WriteData("txt", Text);
            if (!string.IsNullOrEmpty(AuthorizationCode)) cw.WriteData("authcode", AuthorizationCode);
            if (!string.IsNullOrEmpty(Rrn)) cw.WriteData("rrn", Rrn);
            if (!string.IsNullOrEmpty(Stan)) cw.WriteData("stan", Stan);
            if (!string.IsNullOrEmpty(CardType)) cw.WriteData("cardtype", CardType);
            cw.WriteEnd();

            byte[] message = AsyncPosPacket.PrependHeader(0x00, AsyncPosPacket.GenRandomId(8), posId, cw.ToArray());
            AsyncPosPacket.SetSignature(message, 0, apak);

            return message;
        }
    }
}
