using System;
using System.Globalization;
using PlugNPayHub.Utils;

namespace PlugNPayHub.Device.PedControl.Eps.Messages
{
    public class CardRemoved
    {
        public DateTime Time { get; set; }
        public string Token { get; set; }

        public static CardRemoved Deserialize(Format0Reader reader)
        {
            Ensure.NotNull(reader, nameof(reader));

            if (reader.RootTag != "cardremoved")
                throw new InvalidOperationException($"Expected CardRemoved message but received {reader.RootTag} message");

            return new CardRemoved
            {
                Time = DateTime.ParseExact(reader.GetValue("time"), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None),
                Token = reader.GetValue("token"),
            };
        }
    }
}
