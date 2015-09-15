using System.Collections.Generic;
using System.IO;
using System.Xml;
using NLog;
using PlugNPayHub.Utils;

namespace PlugNPayHub.Device.PedControl.Eps
{
    class Languages
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        static readonly Dictionary<string, Dictionary<string, string>> Vals = new Dictionary<string, Dictionary<string, string>>();

        const string DefLang = "EN";

        public Languages()
        {
            byte[] data = null;

            System.Reflection.Assembly thisExe = System.Reflection.Assembly.GetExecutingAssembly();
            using (Stream file = thisExe.GetManifestResourceStream($"{typeof(Languages).Namespace}.Languages.xml"))
            {
                if (file != null)
                {
                    data = new byte[file.Length];
                    file.Read(data, 0, data.Length);
                }
            }

            if (data == null) return;

            XmlDocument x = new XmlDocument();
            x.Load(new MemoryStream(data));

            XmlElement root = x.DocumentElement;
            if (root == null) return;

            foreach (XmlNode n in root.ChildNodes)
            {
                if (n.NodeType != XmlNodeType.Element || n.Name != "e") continue;

                string key = null;
                string val = "";

                if (n.Attributes != null && n.Attributes["k"] != null)
                    key = n.Attributes["k"].Value;

                if (string.IsNullOrEmpty(key))
                {
                    Log.Error("Empty key found in languages file, skipping...");
                    continue;
                }

                if (n.Attributes["v"] != null)
                    val = n.Attributes["v"].Value;

                Dictionary<string, string> dict = Vals[key] = new Dictionary<string, string>();
                dict[DefLang] = val;

                foreach (XmlNode child in n.ChildNodes)
                {
                    if (child.NodeType != XmlNodeType.Element) continue;

                    key = child.Name;
                    val = "";
                    if (child.Attributes != null && child.Attributes["v"] != null)
                        val = child.Attributes["v"].Value;

                    if (!string.IsNullOrEmpty(key))
                        dict[key] = val;
                }
            }
        }

        public string Get(string key, string lang)
        {
            Ensure.NotNull(key, "key");

            if (lang == null)
                lang = DefLang;

            string res = key;

            Dictionary<string, string> d;
            if (Vals.TryGetValue(key, out d))
            {
                string val;
                if (d.TryGetValue(lang, out val))
                    res = val;
                else if (d.TryGetValue(DefLang, out val))
                    res = val;
                else
                    Log.Warn("WARNING: key {0} with lan {1} not found in languages dict", key, lang);
            }
            else
                Log.Warn("WARNING: key {0} not found in languages dict", key);

            return res;
        }
    }
}
