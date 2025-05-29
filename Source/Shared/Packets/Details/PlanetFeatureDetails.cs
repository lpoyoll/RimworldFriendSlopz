using System;
using System.Text;
using Newtonsoft.Json;
using Shared.Misc;

namespace Shared
{
    [Serializable]
    public class PlanetFeatureDetails
    {
        [JsonProperty("Name")]
        [JsonConverter(typeof(Updater.StringConverter))]
        public byte[]? NameRaw { get; set; }
        [JsonIgnore]
        public string? Name
        {
            get
            {
                if (NameRaw == null) return null;
                return Encoding.UTF8.GetString(NameRaw);
            }
            set
            {
                if (value == null)
                {
                    NameRaw = null;
                    return;
                }

                NameRaw = Encoding.UTF8.GetBytes(value);
            }
        }
        [JsonProperty("DefName")]
        [JsonConverter(typeof(Updater.StringConverter))]
        public byte[] DefNameRaw { get; set; }
        [JsonIgnore]
        public string? DefName {           
            get
            {
                if (DefNameRaw == null) return null;
                return Encoding.UTF8.GetString(DefNameRaw);
            }
            set
            {
                if (value == null)
                {
                    DefNameRaw = null;
                    return;
                }

                DefNameRaw = Encoding.UTF8.GetBytes(value);
            }}

        public float[]? DrawCenter { get; set; }

        public float MaxDrawSizeInTiles { get; set; }
    }
}