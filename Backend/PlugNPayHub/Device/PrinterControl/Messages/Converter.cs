using System;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace PlugNPayHub.Device.PrinterControl.Messages
{
    public static class Converter
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };

        static Converter()
        {
            JsonSerializerSettings.Converters.Add(new CompressedDateTimeConvertor());
            JsonSerializerSettings.Converters.Add(new StringEnumConverter { CamelCaseText = false });
        }

        public static string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value, JsonSerializerSettings);
        }

        public static byte[] SerializeToArray(object value)
        {
            return Encoding.UTF8.GetBytes(Serialize(value));
        }

        public static T Deserialize<T>(string value)
        {
            return JsonConvert.DeserializeObject<T>(value, JsonSerializerSettings);
        }

        public static T Deserialize<T>(byte[] value)
        {
            return Deserialize<T>(Encoding.UTF8.GetString(value));
        }

        class CompressedDateTimeConvertor : DateTimeConverterBase
        {
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                return DateTime.ParseExact(reader.Value.ToString(), "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteValue(((DateTime)value).ToString("yyyyMMddHHmmss"));
            }
        }
    }
}
