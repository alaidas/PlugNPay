using System;
using PlugNPayHub.Utils;

namespace PlugNPayHub.Device.PedControl.Eps.Messages
{
    public class AdjustResponse
    {
        public string AuthorizationId { get; set; }
        public string DocumentNumber { get; set; }
        public bool Approved { get; set; }

        public static AdjustResponse Deserialize(Format0Reader reader)
        {
            Ensure.NotNull(reader, nameof(reader));

            if (reader.RootTag != "adjustresult")
                throw new InvalidOperationException($"Expected AdjustResult message but received {reader.RootTag} message");

            AdjustResponse result = new AdjustResponse
            {
                AuthorizationId = reader.GetValue("id"),
                DocumentNumber = reader.GetValue("docnr"),
                Approved = reader.GetValue("result") == "OK",
            };

            return result;
        }
    }
}
