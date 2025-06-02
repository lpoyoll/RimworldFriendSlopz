using System;
using System.Text;
using GameServer.Core;
using GameServer.Files;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shared;

namespace GameServer.Misc
{
    public static class Updater
    {
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
                HasUpdated = true;
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