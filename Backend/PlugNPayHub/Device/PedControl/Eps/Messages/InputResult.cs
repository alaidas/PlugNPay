using PlugNPayHub.Utils;

namespace PlugNPayHub.Device.PedControl.Eps.Messages
{
    public class InputResult
    {
        public enum Source
        {
            Prompt = 1,
            Selection = 2,
            MessageBox,
            Back,
            Bar,
            MSR,
        }

        public string DocumentNumber { get; set; }
        public string Input { get; set; }
        public Source InputSource { get; set; }

        public static InputResult Create(string documentNumber, string input, Source inputSource)
        {
            Ensure.NotNull(documentNumber, nameof(documentNumber));

            if (input == null) input = "";

            return new InputResult
            {
                DocumentNumber = documentNumber,
                Input = input,
                InputSource = inputSource
            };
        }

        public byte[] Serialize(byte[] apak, string posId)
        {
            Format0Writer cw = new Format0Writer();
            cw.WriteStart("InputResult");
            cw.WriteData("docnr", DocumentNumber);
            cw.WriteData("input", Input);
            cw.WriteData("source", InputSource.ToString());
            cw.WriteEnd();

            byte[] message = AsyncPosPacket.PrependHeader(0x00, AsyncPosPacket.GenRandomId(8), posId, cw.ToArray());
            AsyncPosPacket.SetSignature(message, 0, apak);

            return message;
        }
    }
}
