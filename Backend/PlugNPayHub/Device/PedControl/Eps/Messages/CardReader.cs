using System;
using System.Globalization;
using PlugNPayHub.Utils;

namespace PlugNPayHub.Device.PedControl.Eps.Messages
{
    public class CardReader
    {
        public enum CardReadMethods
        {
            Chip = 0,
            Magnetic = 1,
            Nfc = 2,
            Manually = 3
        }

        public DateTime Time { get; set; }
        public string Token { get; set; }
        public Flags Flags { get; set; }
        public string CardType { get; set; }
        public CardReadMethods CardReadMethod { get; set; }

        public static CardReader Deserialize(Format0Reader reader)
        {
            Ensure.NotNull(reader, nameof(reader));

            if (reader.RootTag != "cardreader")
                throw new InvalidOperationException($"Expected CardReader message but received {reader.RootTag} message");

            CardReader result = new CardReader
            {
                Time = DateTime.ParseExact(reader.GetValue("time"), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None),
                Token = reader.GetValue("token"),
                Flags = new Flags(reader.GetValue("flags")),
                CardType = reader.GetValue("cardtype")
            };

            string cardReadMethod = reader.GetValue("crm");
            if (string.IsNullOrEmpty(cardReadMethod)) return result;

            switch (cardReadMethod.ToUpper())
            {
                case "INS":
                    result.CardReadMethod = CardReadMethods.Chip;
                    break;
                case "SWP":
                    result.CardReadMethod = CardReadMethods.Magnetic;
                    break;
                case "NFC":
                    result.CardReadMethod = CardReadMethods.Nfc;
                    break;
                case "MAN":
                    result.CardReadMethod = CardReadMethods.Manually;
                    break;
            }

            return result;
        }
    }
}
