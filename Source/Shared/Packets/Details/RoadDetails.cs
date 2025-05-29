using System;
using System.Text;
using Newtonsoft.Json;

namespace Shared
{
    [Serializable]
    public class RoadDetails
    {
        [JsonProperty("Tiles")]
        [JsonConverter(typeof(Updater.StringConverter))]
        public byte[]? RoadDefNameRaw { get; set; }

        [JsonIgnore]
        public string? RoadDefName
        {
            get
            {
                if (RoadDefNameRaw == null) return null;
                return Encoding.UTF8.GetString(RoadDefNameRaw);
            }
            set
            {
                if (value == null)
                {
                    RoadDefNameRaw = null;
                    return;
                }

                RoadDefNameRaw = Encoding.UTF8.GetBytes(value);
            }
        }

        public int FromTile { get; set; }

        public int ToTile { get; set; }
    }
}