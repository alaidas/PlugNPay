using System;
using PlugNPayHub.Utils;

namespace PlugNPayHub.Device.PedControl.Eps.Messages
{
    public class LoginRequest
    {
        public DateTime Time { get; private set; }
        public string IdleText { get; private set; }
        public Flags Flags { get; private set; }
        public string Lang { get; private set; }

        public LoginRequest()
        {
            Time = DateTime.Now;
            Flags = new Flags("CRM|N|CR");
        }

        public static LoginRequest Create(string languageCode, string idleText)
        {
            if (string.IsNullOrEmpty(languageCode))
                languageCode = "EN";

            return new LoginRequest
            {
                Lang = languageCode,
                IdleText = idleText
            };
        }

        public byte[] Serialize(byte[] apak, string posId)
        {
            Format0Writer cw = new Format0Writer();
            cw.WriteStart("Login");
            cw.WriteData("time", Time.ToString("yyyy-MM-dd HH:mm:ss"));
            cw.WriteData("idletext", IdleText);

            string flags = Flags.ToString();
            if (!string.IsNullOrEmpty(flags))
                cw.WriteData("flags", flags);

            cw.WriteData("lang", Lang);
            cw.WriteEnd();

            byte[] message = AsyncPosPacket.PrependHeader(0x00, AsyncPosPacket.GenRandomId(8), posId, cw.ToArray());
            AsyncPosPacket.SetSignature(message, 0, apak);

            return message;
        }

        public static LoginRequest Deserialize(Format0Reader reader)
        {
            Ensure.NotNull(reader, nameof(reader));

            if (reader.RootTag != "login")
                throw new InvalidOperationException($"Expected Login message but received {reader.RootTag} message");

            LoginRequest login = Create(reader.GetValue("lang"), reader.GetValue("idletext"));
            login.Flags.AddFlagsList(reader.GetValue("flags"));

            return login;
        }
    }
}
