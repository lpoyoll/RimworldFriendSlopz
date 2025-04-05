using System;
using MessagePack;

namespace Shared
{
    [MessagePackObject]

    public class MapData
    {
        public MapFile _mapFile = new MapFile();
    }
}