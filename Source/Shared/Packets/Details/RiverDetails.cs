using System;
using System.Text;
using Newtonsoft.Json;
using Shared.Misc;

namespace Shared
{
    [Serializable]
    public readonly struct RiverDetails
    {
        public string RiverDefName { get; }

        public int FromTile { get; }

        public int ToTile { get; }
        [JsonConstructor]
        public RiverDetails(int fromTile, int toTile, string defname)
        {
            FromTile = fromTile;
            ToTile = toTile;
            RiverDefName = Pools.StringPool.GetOrAddString(defname);
        }
    }
}