using System;
using PlugNPayHub.Utils;

namespace PlugNPayHub.Device.PedControl.Eps.Messages
{
    public class GetPromptInput
    {
        public enum PromptReasons
        {
            Amount = 0,
            Last4Digits = 1,
            MobilePhoneNumber = 2,
            Barcode = 3
        }

        public string Text { get; set; }
        public PromptReasons Rerason { get; set; }

        public static GetPromptInput Deserialize(Format0Reader reader)
        {
            Ensure.NotNull(reader, nameof(reader));

            if (reader.RootTag != "getpromptinput")
                throw new InvalidOperationException($"Expected GetPromptInput message but received {reader.RootTag} message");

            string reasonString = reader.GetValue("reason");
            if (string.IsNullOrEmpty(reasonString))
                throw new Exception("GetPromptInput has no reason field");

            PromptReasons reason;
            switch (reasonString.ToUpper())
            {
                case "SUM": reason = PromptReasons.Amount; break;
                case "LFD": reason = PromptReasons.Last4Digits; break;
                case "MPN": reason = PromptReasons.MobilePhoneNumber; break;
                case "BAR": reason = PromptReasons.Barcode; break;
                default: throw new NotSupportedException($"GetPromptInput has not supported reason [{reasonString}]");
            }

            return new GetPromptInput
            {
                Text = reader.GetValue("prompt"),
                Rerason = reason
            };
        }
    }
}
