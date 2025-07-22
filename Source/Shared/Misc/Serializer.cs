using System;
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

        /// <summary>
        /// Serializes an object to a JSON at the path specified, deserialized by <see cref="SerializeToFile"/>
        /// </summary>
        public static void SerializeToFile(string path, object serializable)
        {
            try
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(serializable, IndentedSettings));
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to write serialized json object to path: {path}", ex);
            }
        }

        /// <summary>
        /// Deserializes an object from JSON, serialized by <see cref="SerializeFromString"/>
        /// </summary>
        public static T SerializeFromFile<T>(string path)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(File.ReadAllText(path), DefaultSettings);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to read serialized json object from path: {path}", ex);
            }
        }

        /// <summary>
        /// Serializes an object to binary to a file, deserialized by <see cref="FileBytesToObject{T}"/>
        /// </summary>
        public static void ObjectBytesToFile(string path, object serializable)
        {
            try
            {
                File.WriteAllBytes(path, ConvertObjectToBytes(serializable, true));
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to write serialized binary object to path: {path}", ex);
            }
        }

        /// <summary>
        /// Deserializes an object from binary from a file, serialize by <see cref="ObjectBytesToFile"/>
        /// </summary>
        public static T FileBytesToObject<T>(string path)
        {
            try
            {
                return ConvertBytesToObject<T>(File.ReadAllBytes(path), true);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to read serialized file bytes to path: {path}", ex);
            }
        }
    }
}