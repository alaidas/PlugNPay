using System;

namespace PlugNPayHub.Utils
{
    public static class Hex
    {
        private static readonly char[] Hx = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        public static string Encode(byte b)
        {
            var ca = new char[2];
            ca[0] = Hx[b >> 4];
            ca[1] = Hx[b & 0x0F];
            return new string(ca);
        }

        /// <summary>
        ///     Encode hex
        /// </summary>
        /// <param name="ba">ByteArray to encode</param>
        /// <returns>Hex encoded data</returns>
        public static string Encode(byte[] ba)
        {
            if (ba == null) return null;
            var ca = new char[ba.Length << 1];
            for (var ck = 0; ck < ba.Length; ck++)
            {
                var b = ba[ck];
                ca[(ck << 1)] = Hx[b >> 4];
                ca[(ck << 1) + 1] = Hx[b & 0x0F];
            }
            return new string(ca);
        }

        public static string Encode(byte[] ba, int offset, int length)
        {
            if (ba == null) return null;
            var ca = new char[ba.Length << 1];
            for (var ck = offset; ck < offset + length; ck++)
            {
                var b = ba[ck];
                ca[(ck << 1)] = Hx[b >> 4];
                ca[(ck << 1) + 1] = Hx[b & 0x0F];
            }
            return new string(ca);
        }

        /// <summary>
        ///     Decode hex
        /// </summary>
        /// <param name="s">Hex encoded data</param>
        /// <returns>Decoded ByteArray</returns>
        public static byte[] Decode(string s)
        {
            if (s == null) return null;
            var ba = new byte[s.Length >> 1];
            var ck = 0;
            for (var i = 0; i < ba.Length; i++)
            {
                switch (s[ck++])
                {
                    case '0':
                        break;
                    case '1':
                        ba[i] = 0x10;
                        break;
                    case '2':
                        ba[i] = 0x20;
                        break;
                    case '3':
                        ba[i] = 0x30;
                        break;
                    case '4':
                        ba[i] = 0x40;
                        break;
                    case '5':
                        ba[i] = 0x50;
                        break;
                    case '6':
                        ba[i] = 0x60;
                        break;
                    case '7':
                        ba[i] = 0x70;
                        break;
                    case '8':
                        ba[i] = 0x80;
                        break;
                    case '9':
                        ba[i] = 0x90;
                        break;
                    case 'A':
                    case 'a':
                        ba[i] = 0xA0;
                        break;
                    case 'B':
                    case 'b':
                        ba[i] = 0xB0;
                        break;
                    case 'C':
                    case 'c':
                        ba[i] = 0xC0;
                        break;
                    case 'D':
                    case 'd':
                        ba[i] = 0xD0;
                        break;
                    case 'E':
                    case 'e':
                        ba[i] = 0xE0;
                        break;
                    case 'F':
                    case 'f':
                        ba[i] = 0xF0;
                        break;
                    default:
                        throw new ArgumentException("String is not hex encoded data.");
                }
                switch (s[ck++])
                {
                    case '0':
                        break;
                    case '1':
                        ba[i] |= 0x01;
                        break;
                    case '2':
                        ba[i] |= 0x02;
                        break;
                    case '3':
                        ba[i] |= 0x03;
                        break;
                    case '4':
                        ba[i] |= 0x04;
                        break;
                    case '5':
                        ba[i] |= 0x05;
                        break;
                    case '6':
                        ba[i] |= 0x06;
                        break;
                    case '7':
                        ba[i] |= 0x07;
                        break;
                    case '8':
                        ba[i] |= 0x08;
                        break;
                    case '9':
                        ba[i] |= 0x09;
                        break;
                    case 'A':
                    case 'a':
                        ba[i] |= 0x0A;
                        break;
                    case 'B':
                    case 'b':
                        ba[i] |= 0x0B;
                        break;
                    case 'C':
                    case 'c':
                        ba[i] |= 0x0C;
                        break;
                    case 'D':
                    case 'd':
                        ba[i] |= 0x0D;
                        break;
                    case 'E':
                    case 'e':
                        ba[i] |= 0x0E;
                        break;
                    case 'F':
                    case 'f':
                        ba[i] |= 0x0F;
                        break;
                    default:
                        throw new ArgumentException("String is not hex encoded data.");
                }
            }
            return ba;
        }

        public static bool TryDecode(string s, out byte[] result)
        {
            result = null;
            if (s == null) return false;
            var ba = new byte[s.Length >> 1];
            var ck = 0;
            for (var i = 0; i < ba.Length; i++)
            {
                switch (s[ck++])
                {
                    case '0':
                        break;
                    case '1':
                        ba[i] = 0x10;
                        break;
                    case '2':
                        ba[i] = 0x20;
                        break;
                    case '3':
                        ba[i] = 0x30;
                        break;
                    case '4':
                        ba[i] = 0x40;
                        break;
                    case '5':
                        ba[i] = 0x50;
                        break;
                    case '6':
                        ba[i] = 0x60;
                        break;
                    case '7':
                        ba[i] = 0x70;
                        break;
                    case '8':
                        ba[i] = 0x80;
                        break;
                    case '9':
                        ba[i] = 0x90;
                        break;
                    case 'A':
                    case 'a':
                        ba[i] = 0xA0;
                        break;
                    case 'B':
                    case 'b':
                        ba[i] = 0xB0;
                        break;
                    case 'C':
                    case 'c':
                        ba[i] = 0xC0;
                        break;
                    case 'D':
                    case 'd':
                        ba[i] = 0xD0;
                        break;
                    case 'E':
                    case 'e':
                        ba[i] = 0xE0;
                        break;
                    case 'F':
                    case 'f':
                        ba[i] = 0xF0;
                        break;
                    default:
                        return false;
                }
                switch (s[ck++])
                {
                    case '0':
                        break;
                    case '1':
                        ba[i] |= 0x01;
                        break;
                    case '2':
                        ba[i] |= 0x02;
                        break;
                    case '3':
                        ba[i] |= 0x03;
                        break;
                    case '4':
                        ba[i] |= 0x04;
                        break;
                    case '5':
                        ba[i] |= 0x05;
                        break;
                    case '6':
                        ba[i] |= 0x06;
                        break;
                    case '7':
                        ba[i] |= 0x07;
                        break;
                    case '8':
                        ba[i] |= 0x08;
                        break;
                    case '9':
                        ba[i] |= 0x09;
                        break;
                    case 'A':
                    case 'a':
                        ba[i] |= 0x0A;
                        break;
                    case 'B':
                    case 'b':
                        ba[i] |= 0x0B;
                        break;
                    case 'C':
                    case 'c':
                        ba[i] |= 0x0C;
                        break;
                    case 'D':
                    case 'd':
                        ba[i] |= 0x0D;
                        break;
                    case 'E':
                    case 'e':
                        ba[i] |= 0x0E;
                        break;
                    case 'F':
                    case 'f':
                        ba[i] |= 0x0F;
                        break;
                    default:
                        return false;
                }
            }
            result = ba;
            return true;
        }
    }
}