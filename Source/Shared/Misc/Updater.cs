using System;
using System.Text;
#if SERVER
using GameServer.Core;
using GameServer.Files;
#endif
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Shared.Misc
{
    public static class Updater
    {
#if SERVER
        private static readonly string PathToVersionFile = Path.Combine(Master.MainPath, "Prev-Version.json");
        public static bool HasUpdated {get; private set;}
        public static bool ValidatePreviousVersion()
        {
            if (!File.Exists(PathToVersionFile))
            {
                Serializer.SerializeToFile(PathToVersionFile, new VersionFile());
                return false;
            }

            VersionFile prevVersion = Serializer.SerializeFromFile<VersionFile>(PathToVersionFile);
            if (prevVersion.Version != new VersionFile().Version)
            {
                return false;
            }

            return true;
        }
#endif
        // Converts strings into byte[]
        public class StringConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType) => true;

            public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
                JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null)
                {
                    return null;
                }
                if (reader.TokenType == JsonToken.String)
                {
#if SERVER
                    HasUpdated = true;
#endif
                    return Encoding.UTF8.GetBytes(reader.Value as string);
                }

                if (reader.TokenType == JsonToken.Bytes)
                {
                    return (byte[]?)reader.Value;
                }

                throw new NotImplementedException();
            }

            public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
            {
                serializer.Serialize(writer, value as byte[]);
            }
        }
        // Converts string[] into byte[]
        public class StringArrayConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(byte[]);
            }

            public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null)
                    return null;

                if (reader.TokenType == JsonToken.Bytes)
                {
                    return (byte[]?)reader.Value;
                }
#if SERVER
                HasUpdated = true;
#endif
                string[] data = serializer.Deserialize<string[]>(reader);
                return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
            }

            public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
            {
                serializer.Serialize(writer, value as byte[]);
            }
        }

    }
}