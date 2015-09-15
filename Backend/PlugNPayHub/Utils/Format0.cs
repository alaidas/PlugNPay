using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlugNPayHub.Utils
{
    public class Format0Reader_
    {
        #region Private variables
        private byte[] _buf;
        private int _ps;
        private int _e;
        private bool _ontag = true;
        private string _tag = null;
        //private bool _c = false;
        private byte _marker = 0;
        private Stack _stack = new Stack();
        private int _origPos;
        #endregion

        #region Constructors
        public Format0Reader_(byte[] buffer) : this(buffer, 0, buffer.Length) { }

        public Format0Reader_(byte[] buffer, int offset, int count)
        {
            _buf = buffer;
            _ps = offset;
            _origPos = _ps;
            _e = offset + count;
        }
        #endregion

        #region Public methods
        public bool EOF { get { return _ps >= _e; } }
        public bool IsConstructed { get { return _ps < _e && (_marker & 0x80) == 0x80; } }
        public string Tag { get { return (_tag != null ? _tag.ToLower() : null); } }

        public bool ReadTag()
        {
            if (_ps >= _e)
                return false;
            if (!_ontag)
            {
                if ((_marker & 0x80) == 0x80)//Current element is constructed 
                {
                    _stack.Push(_tag);
                    _ontag = true;
                }
                else
                    Skip();
            }
            if (_ps >= _e)
                return false;
            _marker = _buf[_ps++];
            if (_marker == 0x00) //End of constructed element or EOF // get parent element 
            {
                _tag = (string)_stack.Pop();
                _ontag = true;
                return false;
            }
            int len = _marker & 0x7F;
            _tag = Encoding.ASCII.GetString(_buf, _ps, len);
            _ps += len;
            _ontag = false;
            return true;
        }

        public string ReadValue()
        {
            if (_ontag)
                throw new Exception("Reader error. Current possition not on value.");
            int len = _buf[_ps++] * 256 + _buf[_ps++];
            string val = Encoding.UTF8.GetString(_buf, _ps, len);
            _ps += len;
            _ontag = true;
            return val;
        }

        public void Skip()
        {
            if (!_ontag)
            {
                int len = _buf[_ps++] * 256 + _buf[_ps++];
                _ps += len;
                _ontag = true;
            }
        }

        public Format0Reader_ Reset()
        {
            _ps = _origPos;
            _ontag = true;
            _tag = null;
            _marker = 0;
            _stack = new Stack();

            return this;
        }

        #endregion

        public Format0Reader_ Clone()
        {
            return (Format0Reader_)this.MemberwiseClone();
        }
    }

    public class Format0Reader
    {
        public string RootTag
        {
            get;
            private set;
        }

        Dictionary<string, List<string>> values = new Dictionary<string, List<string>>();
        Dictionary<string, List<Format0Reader>> constructed = new Dictionary<string, List<Format0Reader>>();
        byte[] f0BA = null;

        public Format0Reader(byte[] ba)
        {
            if (ba == null)
                throw new ArgumentNullException("ba");

            f0BA = ba;
            Format0Reader_ r = new Format0Reader_(ba);
            if (r.ReadTag())
            {
                if (r.IsConstructed)
                    Import(r, r.Tag);
                else
                    throw new InvalidOperationException("First root tag is not constructed");
            }
            else
                throw new Exception("Root tag could not be read");
        }

        private Format0Reader(Format0Reader_ r, string rootTag)
        {
            Import(r, rootTag);
        }

        private void Import(Format0Reader_ reader, string rootTag)
        {
            RootTag = rootTag.ToLower();

            while (reader.ReadTag())
            {
                if (!reader.IsConstructed)
                {
                    List<string> val = null;
                    if (!values.TryGetValue(reader.Tag, out val))
                        values[reader.Tag] = val = new List<string>();

                    val.Add(reader.ReadValue());
                }
                else
                {
                    string rt = reader.Tag;

                    List<Format0Reader> cons = null;
                    if (!constructed.TryGetValue(rt, out cons))
                        constructed[rt] = cons = new List<Format0Reader>();

                    cons.Add(new Format0Reader(reader, rt));
                }
            }
        }

        public string GetValue(string key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            List<string> vals;
            if (values.TryGetValue(key.ToLower(), out vals) && vals.Count > 0)
                return vals[0];
            else
                return null;
        }

        public List<string> GetValueList(string key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            List<string> vals;
            if (values.TryGetValue(key.ToLower(), out vals))
                return vals;

            return new List<string>();
        }

        public Format0Reader GetConstructed(string key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            List<Format0Reader> con;
            if (constructed.TryGetValue(key.ToLower(), out con) && con.Count > 0)
                return con[0];
            else
                return null;
        }

        public List<Format0Reader> GetConstructedList(string key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            List<Format0Reader> con;
            if (constructed.TryGetValue(key.ToLower(), out con))
                return con;
            else
                return null;
        }

        public Format0Reader_ GetF0()
        {
            if (f0BA != null)
                return new Format0Reader_(f0BA);
            else
                throw new InvalidOperationException();
        }

        public byte[] ToArray()
        {
            return f0BA;
        }

        public bool ContainsValues(params string[] values)
        {
            foreach (string value in values)
                if (!this.values.ContainsKey(value))
                    return false;

            return true;
        }

        public string Dump()
        {
            StringBuilder sb = new StringBuilder(256);
            sb.AppendLine();
            Dump(sb, 0);
            string res = sb.ToString();
            return res;
        }

        private void Dump(StringBuilder sb, int lvl)
        {
            if (!String.IsNullOrEmpty(RootTag))
                sb.AppendLine("".PadLeft(lvl, '\t') + RootTag);

            foreach (KeyValuePair<string, List<string>> kv in values)
                foreach (string val in kv.Value)
                {
                    string printValue;
                    if (String.IsNullOrEmpty(val))
                        printValue = "<EMPTY>";
                    else
                        printValue = val;
                    sb.AppendLine("".PadLeft(lvl + 1, '\t') + kv.Key + " = " + printValue);
                }

            foreach (KeyValuePair<string, List<Format0Reader>> kv in constructed)
                foreach (Format0Reader co in kv.Value)
                    co.Dump(sb, lvl + 2);
        }

        private string ObfuscateT2IfNeeded(string valueToCheck, string obfuscatedValue)
        {
            try
            {
                string pan = ExtractPAN(valueToCheck);

                if (pan.Length > 0 && IsCardBanking(pan))
                    return obfuscatedValue;
                else
                    return valueToCheck;
            }
            catch (Exception ex)
            {
                // Cant use logging here so i will return exception message
                return ex.Message;
            }
        }

        public string this[string key]
        {
            get
            {
                string res = GetValue(key);
                if (!String.IsNullOrEmpty(res))
                    return res;
                else
                    throw new KeyNotFoundException();
            }
        }

        // Copied this from Misc because dont want to have reference to that assembly
        private string ExtractPAN(string track2)
        {
            if (track2 == null)
                throw new Exception("Track2 is empty");
            int pos = track2.IndexOfAny(new char[] { '=', 'D', 'd' });

            if (pos == -1)
                return track2.TrimStart(new char[] { ';' }).TrimEnd(new char[] { '?' });

            return track2.Substring(0, pos).TrimStart(new char[] { ';' });
        }

        // Copied this from Misc because dont want to have reference to that assembly
        private bool IsCardBanking(string pan)
        {
            return (pan[0] == '3' || pan[0] == '4' || pan[0] == '5' || pan[0] == '6');// chech if card is payment card            
        }
    }

    public class Format0Writer
    {
        private byte[] _buf = null;
        private int _allocsz = 256;
        private int _ps = 0;
        private void Realloc(int needed)
        {
            int cnt = needed / _allocsz + 1;
            Array.Resize<byte>(ref _buf, _buf.Length + cnt * _allocsz);
        }

        public Format0Writer()
            : this(256)
        {
        }

        public Format0Writer(int pagesize)
        {
            if (pagesize > 256)
                _allocsz = pagesize;
            _buf = new byte[_allocsz];
        }

        public byte[] ToArray()
        {
            byte[] rez = new byte[_ps];
            Buffer.BlockCopy(_buf, 0, rez, 0, _ps);
            return rez;
        }

        public void WriteData(string name, string value)
        {
            if (value == null)
                return;
            if (string.IsNullOrEmpty(name) || name.Length > 127)
                throw new ArgumentException("Specified tag name is either too short or too long. Valid lengths are 1 to 127.", "name");
            int vlen = Encoding.UTF8.GetByteCount(value);
            if (vlen > 65535)
                throw new ArgumentException("Value cannot exceed 64 kilobytes!", "value");
            if (name.Length + vlen + 3 >= _buf.Length - _ps)
                Realloc(name.Length + vlen + 3);
            _buf[_ps++] = (byte)name.Length;
            _ps += Encoding.ASCII.GetBytes(name, 0, name.Length, _buf, _ps);
            _buf[_ps++] = (byte)(vlen >> 8);
            _buf[_ps++] = (byte)(vlen & 0xFF);
            _ps += Encoding.UTF8.GetBytes(value, 0, value.Length, _buf, _ps);
        }

        public void WriteStart(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length > 127)
                throw new ArgumentException("Specified tag name is either too short or too long. Valid lengths are 1 to 127.", "name");
            if (name.Length + 1 >= _buf.Length - _ps)
                Realloc(name.Length + 1);
            _buf[_ps++] = (byte)(name.Length | 0x80);
            _ps += Encoding.ASCII.GetBytes(name, 0, name.Length, _buf, _ps);
        }

        public void WriteEnd()
        {
            if (_buf.Length - _ps <= 0)
                Realloc(1);
            _buf[_ps++] = 0;
        }

        public static byte[] CopyAllExcept(byte[] f0Data, params string[] skipTags)
        {
            if (skipTags == null || skipTags.Length == 0)
                return f0Data;

            Format0Reader_ f0Reader = new Format0Reader_(f0Data);
            if (!f0Reader.ReadTag() || string.IsNullOrEmpty(f0Reader.Tag))
                return f0Data;

            Format0Writer f0Writer = new Format0Writer();
            f0Writer.WriteStart(f0Reader.Tag);

            List<string> ignoreTags = new List<string>(skipTags.Select(i => i.ToLower()));
            while (f0Reader.ReadTag())
            {
                if (ignoreTags.Contains(f0Reader.Tag.ToLower()))
                {
                    if (f0Reader.IsConstructed)
                        while (f0Reader.ReadTag()) { }

                    continue;
                }

                if (f0Reader.IsConstructed)
                {
                    f0Writer.WriteStart(f0Reader.Tag);

                    while (f0Reader.ReadTag())
                        f0Writer.WriteData(f0Reader.Tag, f0Reader.ReadValue());

                    f0Writer.WriteEnd();
                }
                else
                    f0Writer.WriteData(f0Reader.Tag, f0Reader.ReadValue());
            }

            f0Writer.WriteEnd();

            return f0Writer.ToArray();
        }
    }
}
