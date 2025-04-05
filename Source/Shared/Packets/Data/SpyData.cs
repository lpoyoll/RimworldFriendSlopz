using MessagePack;
using static Shared.CommonEnumerators;

namespace Shared
{
    [MessagePackObject]
    public class SpyData
    {
        public SpyStepMode _stepMode;

        public WorldObjectMode _worldObjectMode;

        public int _mapTile;

        public MapFile _mapFile;
    }
}