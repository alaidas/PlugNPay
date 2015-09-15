using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlugNPayHub.Utils
{
    public static class Network
    {
        public static byte[] CreatePacket(string data)
        {
            return CreatePacket(Encoding.UTF8.GetBytes(data));
        }

        public static byte[] CreatePacket(byte[] data)
        {
            return PrependLength(data);
        }

        public static byte[] PrependLength(byte[] data)
        {
            byte[] final = new byte[data.Length + 2];
            final[0] = (byte)(data.Length >> 8);
            final[1] = (byte)(data.Length);
            Buffer.BlockCopy(data, 0, final, 2, data.Length);
            return final;
        }

        public static string CreateHeader(string action, string target, string transactionId)
        {
            return string.Join("", action.ToLower(), "@", target.ToLower(), "#", transactionId);
        }
    }
}
