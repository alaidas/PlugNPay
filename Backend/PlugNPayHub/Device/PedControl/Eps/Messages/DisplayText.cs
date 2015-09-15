using System;
using System.Globalization;
using PlugNPayHub.Utils;

namespace PlugNPayHub.Device.PedControl.Eps.Messages
{
    public class DisplayText
    {
        public enum DisplayTextReasons
        {
            Unknown = 0,
            InfoMessage = 1,
            CardWaiting = 2,
            CardRemoveWaiting = 3,
            BarcodeWaiting = 4,
            TimeoutWaiting = 5
        }

        public enum BeepTypes
        {
            NoBeep = -1,
            SingleShort = 0,
            DoubleShort = 1,
            SingleLong = 2,
            DoubleLong = 3
        }

        public string Text { get; set; }
        public DisplayTextReasons Reason { get; set; }
        public Flags Flags { get; set; }
        public BeepTypes Beep { get; set; }

        public DisplayText()
        {
            Flags = new Flags();
        }

        public static DisplayText Create(string text, DisplayTextReasons reason, BeepTypes beep)
        {
            Ensure.NotNull(text, nameof(text));

            return new DisplayText
            {
                Text = text,
                Reason = reason,
                Beep = beep,
                Flags = new Flags(beep != BeepTypes.NoBeep ? "BEEP" : "")
            };
        }

        public static DisplayText Create(string text, DisplayTextReasons reason)
        {
            Ensure.NotNull(text, nameof(text));

            return new DisplayText
            {
                Text = text,
                Reason = reason
            };
        }

        public static DisplayText Create(string text)
        {
            return Create(text, DisplayTextReasons.Unknown);
        }

        public byte[] Serialize(byte[] apak, string posId)
        {
            Format0Writer cw = new Format0Writer();
            cw.WriteStart("DisplayText");
            cw.WriteData("txt", Text);
            cw.WriteData("reason", ((int)Reason).ToString(CultureInfo.InvariantCulture));

            string flags = Flags.ToString();
            if (!string.IsNullOrEmpty(flags))
                cw.WriteData("flags", flags);

            if (Beep != BeepTypes.NoBeep)
                cw.WriteData("beep", ((int)Beep).ToString(CultureInfo.InvariantCulture));

            cw.WriteEnd();

            byte[] message = AsyncPosPacket.PrependHeader(0x00, AsyncPosPacket.GenRandomId(8), posId, cw.ToArray());
            AsyncPosPacket.SetSignature(message, 0, apak);

            return message;
        }

        public static DisplayText Deserialize(Format0Reader reader)
        {
            Ensure.NotNull(reader, nameof(reader));

            if (reader.RootTag != "displaytext")
                throw new InvalidOperationException($"Expected DisplayText message but received {reader.RootTag} message");

            DisplayText result = new DisplayText { Text = reader.GetValue("txt") };

            string reason = reader.GetValue("reason");
            if (string.IsNullOrEmpty(reason)) return result;

            int reasonNum;
            if (!int.TryParse(reason, out reasonNum)) return result;

            result.Reason = (DisplayTextReasons)reasonNum;
            return result;
        }
    }
}
