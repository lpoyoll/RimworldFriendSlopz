using static Shared.CommonEnumerators;

namespace Shared
{

    public class SpyData
    {
        public SpyStepMode _stepMode { get; set; }

        public WorldObjectMode _worldObjectMode { get; set; }

        public int _mapTile { get; set; }

        public MapFile _mapFile { get; set; }
    }
}