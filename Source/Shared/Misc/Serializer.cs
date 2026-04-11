using System;
using MessagePack;
using MessagePack.Resolvers;
using Newtonsoft.Json;
using System.IO;
using Shared.Misc;

namespace Shared
{
    public static class Serializer
    {
        private static JsonSerializerSettings DefaultSettings => new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.None };

        private static JsonSerializerSettings IndentedSettings => new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.None,
            Formatting = Formatting.Indented
        };

        public static byte[] ConvertObjectToBytes<T>(T toConvert, bool compression = true)
        {
            if (compression) return MessagePackSerializer.Serialize(toConvert, ContractlessStandardResolver.Options.WithCompression(MessagePackCompression.Lz4Block));
            else return MessagePackSerializer.Serialize(toConvert, ContractlessStandardResolver.Options);
        }

        public static T ConvertBytesToObject<T>(byte[] bytes, bool compression = true)
        {
            if (compression) return MessagePackSerializer.Deserialize<T>(bytes, ContractlessStandardResolver.Options.WithCompression(MessagePackCompression.Lz4Block));
            else return MessagePackSerializer.Deserialize<T>(bytes, ContractlessStandardResolver.Options);
        }

        public static void SerializeToFile(string path, object serializable)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(serializable, IndentedSettings));
        }

        public static T SerializeFromFile<T>(string path)
        {
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(path), DefaultSettings);
        }
    }
}