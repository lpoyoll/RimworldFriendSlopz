namespace Shared.Details.Planet
{
    public class FeatureDetail
    {
        public string Label { get; set; } = null;
        
        public string DefName { get; set; } = null;

        public float[] DrawCenter { get; set; } = null;

        public float MaxDrawSizeInTiles { get; set; } = -1;
    }
}