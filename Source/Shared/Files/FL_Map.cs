using System.Collections.Generic;

namespace Shared.Files
{
    public class FL_Map
    {
        public int Tile { get; set; } = -1;

        public int[] Size { get; set; } = null;

        public int Wealth { get; set; } = -1;

        public byte WeatherByte { get; set; } = byte.MaxValue;

        public List<string> Tiles { get; set; } = new List<string>();

        public List<string> Things { get; set; } = new List<string>();

        public List<string> Pawns { get; set; } = new List<string>();

        public List<string> Roofs { get; set; } = new List<string>();

        public List<bool> Pollutions { get; set; } = new List<bool>();
    }
}