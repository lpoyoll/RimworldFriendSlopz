using System;
using System.Text;
using Newtonsoft.Json;
using Shared.Misc;

namespace Shared
{
    [Serializable]
    public class RiverDetails
    {
        [JsonIgnore] private string? CachedDefName = null;
        public string? RiverDefName 
        {
            get
            {
                return CachedDefName;
            }
            set
            {
                CachedDefName = Pools.StringPool.GetOrAddString(value);
            } 
        }

        public int FromTile { get; set; }

        public int ToTile { get; set; }
    }
}