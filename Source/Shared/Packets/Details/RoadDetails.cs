using System;

namespace Shared
{
    [Serializable]
    public class RoadDetails
    {
        public string? RoadDefName { get; set; }

        public int FromTile { get; set; }

        public int ToTile { get; set; }
    }
}