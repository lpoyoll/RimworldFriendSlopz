namespace Shared
{
    public class MapFile
    {
        public int Tile;

        public int[] Size;

        public string UID;

        public string CurWeatherDefName;

        public ModConfigFile Mods;

        public MapTileDetails[] Tiles = new MapTileDetails[0];

        public string[] FactionThings;

        public string[] NonFactionThings;

        public HumanFile[] FactionHumans;

        public HumanFile[] NonFactionHumans;

        public string[] FactionAnimals;
        
        public string[] NonFactionAnimals;

        public override string ToString()
        {
            return $"MapFile:|{Tile}|{UID}|{CurWeatherDefName}|{Mods}";
        }
    }
}