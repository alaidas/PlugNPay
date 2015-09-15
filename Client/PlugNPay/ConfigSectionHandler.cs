using System.Configuration;
using PlugNPay.Utils;

namespace PlugNPayClient
{
    class ConfigSectionHandler : ConfigurationSection
    {
        [ConfigurationProperty("", IsRequired = true, IsDefaultCollection = true)]
        public ConfigSectionCollection Instances
        {
            get { return (ConfigSectionCollection)this[""]; }
            set { this[""] = value; }
        }
    }

    class ConfigSectionCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new ConfigSectionElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ConfigSectionElement)element).Type;
        }
    }

    class ConfigSectionElement : ConfigurationElement
    {
        public readonly Attributes Attributes = new Attributes();

        [ConfigurationProperty("type", IsRequired = true)]
        public string Type
        {
            get { return (string)base["type"]; }
            set { base["type"] = value; }
        }

        protected override bool OnDeserializeUnrecognizedAttribute(string name, string value)
        {
            Attributes.Add(name, value);
            return true;
        }
    }
}
