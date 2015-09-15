using System;
using PlugNPayHub.Utils;

namespace PlugNPayHub.Device.PedControl.Eps.Messages
{
    public class DocClosedRequest
    {
        public string DocumentNumber { get; set; }
        public string AuthorizationId { get; set; }

        public byte[] Serialize(byte[] apak, string posId)
        {
            Format0Writer cw = new Format0Writer();
            cw.WriteStart("DocClosed");
            cw.WriteData("docnr", DocumentNumber);

            if (!string.IsNullOrEmpty(AuthorizationId))
                cw.WriteData("authid", AuthorizationId);

            cw.WriteEnd();

            byte[] message = AsyncPosPacket.PrependHeader(0x00, AsyncPosPacket.GenRandomId(8), posId, cw.ToArray());
            AsyncPosPacket.SetSignature(message, 0, apak);

            return message;
        }

        public static DocClosedRequest Create(string documentNumber, string authorizationId)
        {
            Ensure.NotNull(documentNumber, nameof(documentNumber));

            return new DocClosedRequest
            {
                DocumentNumber = documentNumber,
                AuthorizationId = authorizationId,
            };
        }

        public static DocClosedRequest Deserialize(Format0Reader reader)
        {
            Ensure.NotNull(reader, nameof(reader));

            if (reader.RootTag != "docclosed")
                throw new InvalidOperationException($"Expected DocClosed message but received {reader.RootTag} message");

            return new DocClosedRequest
            {
                DocumentNumber = reader.GetValue("docnr"),
                AuthorizationId = reader.GetValue("authid")
            };
        }
    }
}
