using MessagePack;
using MessagePack.Resolvers;
using Newtonsoft.Json;
using System.IO;

namespace Shared
{
    //Class that handles all of the mod's serialization functions

    public static class Serializer
    {
        // Variables

        private static JsonSerializerSettings DefaultSettings => new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.None };

        private static JsonSerializerSettings IndentedSettings => new JsonSerializerSettings() 
        { 
            TypeNameHandling = TypeNameHandling.None,
            Formatting = Formatting.Indented
        };

        //Serialize from and to byte arrays

        public static byte[] ConvertObjectToBytes(object toConvert, bool compression = false)
        {
            return MessagePackSerializer.Serialize(toConvert, ContractlessStandardResolver.Options);
        }

        public static T ConvertBytesToObject<T>(byte[] bytes, bool compression = false)
        {
            return MessagePackSerializer.Deserialize<T>(bytes, ContractlessStandardResolver.Options);
        }

        // Serialize from and to strings

        public static string SerializeToString(object serializable) { return JsonConvert.SerializeObject(serializable, DefaultSettings); }

        public static T SerializeFromString<T>(string serializable) { return JsonConvert.DeserializeObject<T>(serializable, DefaultSettings); }

        // Serialize from and to files text

        public static void SerializeToFile(string path, object serializable) { File.WriteAllText(path, JsonConvert.SerializeObject(serializable, IndentedSettings)); }

        public static T SerializeFromFile<T>(string path) { return JsonConvert.DeserializeObject<T>(File.ReadAllText(path), DefaultSettings); }

        // Serialize from and to file bytes

        public static void ObjectBytesToFile(string path, object serializable) { File.WriteAllBytes(path, ConvertObjectToBytes(serializable, true)); }

        public static T FileBytesToObject<T>(string path) { return ConvertBytesToObject<T>(File.ReadAllBytes(path), true); }
    }
}