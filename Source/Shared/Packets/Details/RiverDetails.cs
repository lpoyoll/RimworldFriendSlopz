using System;

namespace Shared
{
    [Serializable]
    public class RiverDetails
    {
        public string? RiverDefName { get; set; }

        public int FromTile { get; set; }

        public int ToTile { get; set; }
    }
}