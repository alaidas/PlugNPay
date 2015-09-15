using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using PlugNPayHub.Utils;

namespace PlugNPayHub.Device.PedControl.Eps
{
    internal class AsyncPosPacket
    {
        private static readonly RNGCryptoServiceProvider StrongRandom = new RNGCryptoServiceProvider();
        private static readonly Random RandomGanerator = new Random();

        public static byte[] PrependLength(byte[] packet)
        {
            var final = new byte[packet.Length + 2];
            final[0] = (byte)(packet.Length >> 8);
            final[1] = (byte)(packet.Length);
            Buffer.BlockCopy(packet, 0, final, 2, packet.Length);
            return final;
        }

        public static byte GetType(byte[] packet)
        {
            if (packet == null || packet.Length < 1)
                throw new Exception("Invalid packet length");

            return packet[0];
        }

        public static string GetId(byte[] packet)
        {
            CheckLen(packet);

            var id = new byte[8];
            Buffer.BlockCopy(packet, 1, id, 0, id.Length);
            return Hex.Encode(id);
        }

        public static string GetId(byte[] packet, int offset)
        {
            var id = new byte[8];
            Buffer.BlockCopy(packet, 1 + offset, id, 0, id.Length);
            return Hex.Encode(id);
        }

        public static string GetPosId(byte[] packet)
        {
            CheckLen(packet);

            return Encoding.ASCII.GetString(packet, 10, packet[9]);
        }

        public static byte[] GetHeader(byte[] packet)
        {
            CheckLen(packet);

            var head = new byte[10 + packet[9]];
            Buffer.BlockCopy(packet, 0, head, 0, head.Length);
            return head;
        }

        public static byte[] GetMsg(byte[] packet)
        {
            CheckLen(packet);

            // if ksn is available
            if (packet[0] == 0x11)
            {
                var msg = new byte[packet.Length - 10 - packet[9] - 20];
                Buffer.BlockCopy(packet, 10 + packet[9] + 20, msg, 0, msg.Length);
                return msg;
            }
            else
            {
                var msg = new byte[packet.Length - 10 - packet[9]];
                Buffer.BlockCopy(packet, 10 + packet[9], msg, 0, msg.Length);
                return msg;
            }
        }

        public static byte[] GetKsnMsg(byte[] packet)
        {
            var msg = new byte[packet.Length - 10 - packet[9]];
            Buffer.BlockCopy(packet, 10 + packet[9], msg, 0, msg.Length);
            return msg;
        }

        public static byte[] BuildNak(byte[] packet)
        {
            CheckLen(packet);

            var h = GetHeader(packet);
            h[0] = 0x0F;
            return h;
        }

        public static byte[] BuildAck(byte[] packet)
        {
            CheckLen(packet);

            var h = GetHeader(packet);
            h[0] = 0x0A;
            return h;
        }

        private static void CheckLen(byte[] packet)
        {
            if (packet == null)
                throw new ArgumentNullException("packet");

            if (packet.Length < 10)
                throw new Exception("Packet length is < 10");

            if (packet.Length < (packet[9] + 10))
                throw new Exception("Invalid packet length");
        }

        public static byte[] PrependHeader(byte packetType, byte[] packetID, string posID, byte[] msg)
        {
            // packet type + packet ID + posId len + posId + msg
            var ksn = packetType == 0x11 ? 20 : 0;

            var buffer = new byte[1 + 8 + 1 + Encoding.ASCII.GetByteCount(posID) + msg.Length + ksn];
            var i = 0;
            buffer[i++] = packetType;

            Buffer.BlockCopy(packetID, 0, buffer, i, 8);
            i += 8;

            var t = Encoding.ASCII.GetBytes(posID);
            buffer[i++] = (byte)t.Length;
            Buffer.BlockCopy(t, 0, buffer, i, t.Length);
            i += t.Length;

            if (ksn > 0) // copy "ksn"
                i += ksn;

            Buffer.BlockCopy(msg, 0, buffer, i, msg.Length);
            i += msg.Length;

            if (buffer.Length != i)
                throw new Exception("Invalid formed msg length");

            return buffer;
        }

        public static byte[] BuildPacket(byte[] data, string posId, byte[] apak)
        {
            var message = PrependHeader(0x00, GenRandomId(8), posId, data);
            SetSignature(message, 0, apak);

            return message;
        }

        public static byte[] PrependHeaderAndLen(byte packetType, byte[] packetId, string posId, byte[] msg)
        {
            // packet type + packet ID + posId len + posId + msg
            var ksn = packetType == 0x11 ? 20 : 0;

            var buffer = new byte[2 + 1 + 8 + 1 + Encoding.ASCII.GetByteCount(posId) + msg.Length + ksn];
            var i = 0;
            buffer[i++] = (byte)((buffer.Length - 2) >> 8);
            buffer[i++] = (byte)(buffer.Length - 2);
            buffer[i++] = packetType;

            Buffer.BlockCopy(packetId, 0, buffer, i, 8);
            i += 8;

            var t = Encoding.ASCII.GetBytes(posId);
            buffer[i++] = (byte)t.Length;
            Buffer.BlockCopy(t, 0, buffer, i, t.Length);
            i += t.Length;

            if (ksn > 0) // copy "ksn"
                i += ksn;

            Buffer.BlockCopy(msg, 0, buffer, i, msg.Length);
            i += msg.Length;

            if (buffer.Length != i)
                throw new Exception("Invalid formed msg length");

            return buffer;
        }

        public static byte[] GenRandomId(int len)
        {
            var idByteArray = new byte[len];

            lock (RandomGanerator)
                RandomGanerator.NextBytes(idByteArray);

            return idByteArray;
        }

        public static string GenRandom(int len)
        {
            return Hex.Encode(GenRandomId(len));
        }

        public static byte[] GetKsn(byte[] packet)
        {
            if (packet[0] != 0x11)
                throw new InvalidDataException("Only packets 0x11 have ksn");

            var msgBody = GetKsnMsg(packet);
            var ksn = new byte[20];
            Buffer.BlockCopy(msgBody, 0, ksn, 0, 20);
            return ksn;
        }

        public static int GetHeaderLength(byte[] packet)
        {
            CheckLen(packet);
            return (packet[9] + 10);
        }

        public static byte[] GenKsn(uint keyId, uint counter)
        {
            var ba = new byte[20];
            ba[0] = (byte)(keyId >> 24);
            ba[1] = (byte)(keyId >> 16);
            ba[2] = (byte)(keyId >> 8);
            ba[3] = (byte)(keyId);

            ba[4] = (byte)(counter >> 16);
            ba[5] = (byte)(counter >> 8);
            ba[6] = (byte)(counter);
            var t = GetBcdEncoded(DateTime.Now.ToString("yyMMdd"));
            ba[7] = t[0];
            ba[8] = t[1];
            ba[9] = t[2];

            var rand = new byte[10];

            lock (StrongRandom)
                StrongRandom.GetBytes(rand);

            Buffer.BlockCopy(rand, 0, ba, 10, rand.Length);
            return ba;
        }

        public static byte[] GetBcdEncoded(string s)
        {
            byte b = 0;
            var ps = s.Length & 1;
            var ms = new MemoryStream();
            for (var ck = 0; ck < s.Length; ck++)
            {
                switch (char.ToUpper(s[ck]))
                {
                    default:
                        throw new Exception("Invalid character for BCD encoding");
                    case '0':
                        if (ps == 0) b = 0;
                        else ms.WriteByte(b);
                        break;
                    case '1':
                        if (ps == 0) b = 16;
                        else ms.WriteByte((byte)(b | 1));
                        break;
                    case '2':
                        if (ps == 0) b = 32;
                        else ms.WriteByte((byte)(b | 2));
                        break;
                    case '3':
                        if (ps == 0) b = 48;
                        else ms.WriteByte((byte)(b | 3));
                        break;
                    case '4':
                        if (ps == 0) b = 64;
                        else ms.WriteByte((byte)(b | 4));
                        break;
                    case '5':
                        if (ps == 0) b = 80;
                        else ms.WriteByte((byte)(b | 5));
                        break;
                    case '6':
                        if (ps == 0) b = 96;
                        else ms.WriteByte((byte)(b | 6));
                        break;
                    case '7':
                        if (ps == 0) b = 112;
                        else ms.WriteByte((byte)(b | 7));
                        break;
                    case '8':
                        if (ps == 0) b = 128;
                        else ms.WriteByte((byte)(b | 8));
                        break;
                    case '9':
                        if (ps == 0) b = 144;
                        else ms.WriteByte((byte)(b | 9));
                        break;
                    case 'A':
                        if (ps == 0) b = 160;
                        else ms.WriteByte((byte)(b | 10));
                        break;
                    case 'B':
                        if (ps == 0) b = 176;
                        else ms.WriteByte((byte)(b | 11));
                        break;
                    case 'C':
                        if (ps == 0) b = 192;
                        else ms.WriteByte((byte)(b | 12));
                        break;
                    case 'D':
                    case '=':
                        if (ps == 0) b = 208;
                        else ms.WriteByte((byte)(b | 13));
                        break;
                    case 'E':
                        if (ps == 0) b = 224;
                        else ms.WriteByte((byte)(b | 14));
                        break;
                    case 'F':
                        if (ps == 0) b = 240;
                        else ms.WriteByte((byte)(b | 15));
                        break;
                }
                ps ^= 1;
            }
            return ms.ToArray();
        }

        public static bool IdMatch(byte[] a, byte[] b)
        {
            if (a == null || b == null)
                return false;

            if (a.Length != b.Length)
                return false;

            if (a.Length != 8)
                throw new Exception("ID byte array must be 8 bytes long");

            for (var i = 0; i < a.Length; i++)
                if (a[i] != b[i])
                    return false;

            return true;
        }

        public static void ParseZeroTag(string zeroTag, out string opId, out string procId, out string tranId, out string nr)
        {
            if (string.IsNullOrEmpty(zeroTag))
                throw new ArgumentException("zeroTag");

            var t = zeroTag.Split('@', '#', '!');
            if (t.Length != 4)
                throw new Exception("Invalid data format");

            opId = t[0];
            procId = t[1];
            tranId = t[2];
            nr = t[3];
        }

        public static string MakeZeroTag(string opid, string procId, string tranId, string nr)
        {
            return opid + "@" + procId + "#" + tranId + "!" + nr;
        }

        public static string GenTranNr()
        {
            lock (RandomGanerator)
            {
                var nr = RandomGanerator.Next().ToString();
                if (nr.Length > 6)
                    nr = nr.Substring(0, 6);

                return nr;
            }
        }

        public static string GenTranId()
        {
            var id = new byte[8];
            lock (RandomGanerator)
                RandomGanerator.NextBytes(id);
            return Hex.Encode(id);
        }

        public static void FillId(byte[] header, int offset)
        {
            lock (RandomGanerator)
            {
                for (var i = offset; i < offset + 8; i++)
                {
                    header[i] = (byte)RandomGanerator.Next();
                }
            }
        }

        public static string ExtractTranIdNrFromZero(string zeroTag)
        {
            if (zeroTag == null)
                throw new ArgumentNullException(nameof(zeroTag));

            var rez = zeroTag.Split('#');
            if (rez.Length > 1)
                return rez[1];
            throw new InvalidDataException();
        }

        public static string ExtractTranIdFromZero(string zeroTag)
        {
            if (string.IsNullOrEmpty(zeroTag))
                throw new ArgumentException("zeroTag");

            var t = zeroTag.Split('@', '#', '!');
            if (t.Length != 4)
                throw new Exception("Invalid data format");

            return t[2];
        }

        public static string DumpF0(Format0Writer w)
        {
            return DumpF0(w.ToArray());
        }

        public static string DumpF0(byte[] ba)
        {
            return new Format0Reader(ba).Dump();
        }

        public static string DumpF0(Format0Reader msg)
        {
            return msg.Dump();
        }

        public static void SetRandomPacketId(byte[] ba, int offset)
        {
            var pos = 1 + offset;
            var id = new byte[8];

            lock (RandomGanerator)
            {
                RandomGanerator.NextBytes(id);
            }

            if (ba.Length < (pos + id.Length))
                throw new Exception("Invalid packet length");

            Buffer.BlockCopy(id, 0, ba, pos, id.Length);
        }

        public static void SetSignature(byte[] ba, int offset, byte[] key)
        {
            if (key != null)
            {
                var o = offset + 9;
                uint crc = Crc32.Calc(ba, o, ba.Length - o);
                var res = new byte[key.Length];
                res[0] = (byte)(crc >> 24);
                res[1] = (byte)(crc >> 16);
                res[2] = (byte)(crc >> 8);
                res[3] = (byte)(crc);

                for (var i = 0; i < key.Length; i++)
                    res[i] ^= key[i];

                var aes = new RijndaelManaged();
                aes.KeySize = 128;
                aes.BlockSize = 128;
                aes.Key = key;
                aes.Mode = CipherMode.ECB;
                aes.IV = new byte[16];
                aes.Padding = PaddingMode.None;
                res = aes.CreateEncryptor().TransformFinalBlock(res, 0, res.Length);
                Buffer.BlockCopy(res, 0, ba, offset + 1, 8);
            }
        }

        public static bool CheckSignature(byte[] ba, int offset, byte[] key)
        {
            if (key != null)
            {
                var o = offset + 9;
                uint crc = Crc32.Calc(ba, o, ba.Length - o);
                var res = new byte[key.Length];
                res[0] = (byte)(crc >> 24);
                res[1] = (byte)(crc >> 16);
                res[2] = (byte)(crc >> 8);
                res[3] = (byte)(crc);

                for (var i = 0; i < key.Length; i++)
                    res[i] ^= key[i];

                var aes = new RijndaelManaged();
                aes.KeySize = 128;
                aes.BlockSize = 128;
                aes.Key = key;
                aes.Mode = CipherMode.ECB;
                aes.IV = new byte[16];
                aes.Padding = PaddingMode.None;
                res = aes.CreateEncryptor().TransformFinalBlock(res, 0, res.Length);

                var res2 = new byte[8];
                Buffer.BlockCopy(ba, offset + 1, res2, 0, 8);

                for (var i = 0; i < res2.Length; i++)
                    if (res[i] != res2[i])
                        return false;

                return true;
            }
            return true;
        }

        public static void ChangeType(byte[] ba, int offset, byte type)
        {
            if (ba != null)
                ba[offset] = type;
        }
    }
}