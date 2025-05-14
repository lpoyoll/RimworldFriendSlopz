using System;

namespace Shared
{
    [Serializable]
    public class PlanetFeatureDetails
    {
        public string? Name { get; set; }

        public string? DefName { get; set; }

        public float[]? DrawCenter { get; set; }

        public float MaxDrawSizeInTiles { get; set; }
    }
}