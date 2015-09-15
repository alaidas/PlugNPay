using System;
using System.Globalization;
using PlugNPayHub.Utils;

namespace PlugNPayHub.Device.PedControl.Eps.Messages
{
    public class AuthorizationRequest
    {
        public long Amount { get; set; }
        public long Cash { get; set; }
        public int Currency { get; set; }
        public string DocumentNumber { get; set; }
        public DateTime Time { get; set; }
        public string Language { get; set; }
        public string CardToken { get; set; }
        public bool PreAuthorization { get; set; }

        public byte[] Serialize(byte[] apak, string posId)
        {
            Format0Writer cw = new Format0Writer();
            cw.WriteStart(PreAuthorization ? "PreAuthorize" : "Authorize");
            cw.WriteData("amount", Amount.ToString(CultureInfo.InvariantCulture));
            if (Cash > 0) cw.WriteData("cash", Cash.ToString(CultureInfo.InvariantCulture));
            cw.WriteData("currency", Currency.ToString(CultureInfo.InvariantCulture));
            cw.WriteData("docnr", DocumentNumber);
            cw.WriteData("time", Time.ToString("yyyy-MM-dd HH:mm:ss"));
            cw.WriteData("lang", Language);
            cw.WriteData("token", CardToken);
            cw.WriteEnd();

            byte[] message = AsyncPosPacket.PrependHeader(0x00, AsyncPosPacket.GenRandomId(8), posId, cw.ToArray());
            AsyncPosPacket.SetSignature(message, 0, apak);

            return message;
        }

        public static AuthorizationRequest Deserialize(Format0Reader reader)
        {
            Ensure.NotNull(reader, nameof(reader));

            if (reader.RootTag != "authorize")
                throw new InvalidOperationException($"Expected authorize message but received {reader.RootTag} message");

            string cashAmount = reader.GetValue("cash");
            return new AuthorizationRequest
            {
                Amount = Int64.Parse(reader.GetValue("amount")),
                Cash = string.IsNullOrEmpty(cashAmount) ? 0 : long.Parse(cashAmount),
                Currency = Int32.Parse(reader.GetValue("currency")),
                DocumentNumber = reader.GetValue("docnr"),
                Time = DateTime.ParseExact(reader.GetValue("time"), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                Language = reader.GetValue("lang")
            };
        }

        public static AuthorizationRequest Create(string documentNumber, long amount, long cash, string cardToken, DateTime time, int currency, string language, bool preAuthorization)
        {
            AuthorizationRequest authRequest = Create(documentNumber, amount, cash, cardToken, time, currency, language);
            authRequest.PreAuthorization = preAuthorization;

            return authRequest;
        }

        public static AuthorizationRequest Create(string documentNumber, long amount, long cash, string cardToken, DateTime time, int currency, string language)
        {
            Ensure.NotNull(documentNumber, nameof(documentNumber));

            if (amount == 0)
                throw new ArgumentNullException(nameof(amount));

            Ensure.NotNull(cardToken, nameof(cardToken));
            Ensure.NotNull(time, nameof(time));

            if (currency == 0)
                throw new ArgumentNullException(nameof(currency));

            Ensure.NotNull(language, nameof(language));

            return new AuthorizationRequest
            {
                DocumentNumber = documentNumber,
                Amount = amount,
                CardToken = cardToken,
                Time = time,
                Currency = currency,
                Language = language,
                Cash = cash
            };
        }
    }
}
