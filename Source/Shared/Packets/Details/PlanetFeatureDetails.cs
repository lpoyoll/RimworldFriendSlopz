using System;

namespace Shared
{
    [Serializable]
    public class PlanetFeatureDetails
    {
        public string name;

        public string defName;

        public float[] drawCenter;
        
        public float maxDrawSizeInTiles;
    }
}