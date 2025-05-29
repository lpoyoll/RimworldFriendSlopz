using System;
using System.Text;
using Newtonsoft.Json;
using Shared.Misc;

namespace Shared
{
    [Serializable]
    public class RiverDetails
    {
        [JsonProperty("RiverDefName")]
        [JsonConverter(typeof(Updater.StringConverter))]
        public byte[] RiverDefNameRaw { get; set; }
        [JsonIgnore]
        public string? RiverDefName 
        {
            get
            {
                if (RiverDefNameRaw == null) return null;
                return Encoding.UTF8.GetString(RiverDefNameRaw);
            }
            set
            {
                if (value == null || value.Length == 0)
                {
                    RiverDefNameRaw = null;
                    return;
                }
                RiverDefNameRaw = Encoding.UTF8.GetBytes(value);
            } 
        }

        public int FromTile { get; set; }

        public int ToTile { get; set; }
    }
}