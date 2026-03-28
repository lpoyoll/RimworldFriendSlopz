namespace Shared.Files.Configs
{
    public class RoadsConfigFile
    {
        public bool AllowDirtPath { get; set; } = true;

        public bool AllowDirtRoad { get; set; } = true;

        public bool AllowStoneRoad { get; set; } = true;

        public bool AllowAsphaltPath { get; set; } = true;

        public bool AllowAsphaltHighway { get; set; } = true;

        public int DirtPathCost { get; set; } = 10;

        public int DirtRoadCost { get; set; } = 20;

        public int StoneRoadCost { get; set; } = 25;

        public int AsphaltPathCost { get; set; } = 30;

        public int AsphaltHighwayCost { get; set; } = 50;
    }
}
