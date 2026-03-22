using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameClient.Hooks.Synchronous
{
    public class PlayerWeather
    {
        public int MapTile { get; set; } = -1;

        public byte WeatherByte { get; set; } = byte.MaxValue;
    }
}
