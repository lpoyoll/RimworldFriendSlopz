using System;
#if SERVER
using GameServer.Core;
#endif
namespace Shared
{
    [Serializable]
    public class RoadValuesFile
    {
        //Allowance of the roads

        public bool AllowDirtPath = true;

        public bool AllowDirtRoad = true;

        public bool AllowStoneRoad = true;

        public bool AllowAsphaltPath = true;

        public bool AllowAsphaltHighway = true;

        //Cost of the roads

        public int DirtPathCost = 10;

        public int DirtRoadCost = 20;

        public int StoneRoadCost = 25;

        public int AsphaltPathCost = 30;

        public int AsphaltHighwayCost = 50;

        public override string ToString()
        {
            return $"RoadValuesFile:|{AllowDirtPath}|{AllowDirtRoad}|{AllowStoneRoad}|{AllowAsphaltPath}|{AllowAsphaltHighway}" +
                $"|{DirtPathCost}|{DirtRoadCost}|{StoneRoadCost}|{AsphaltPathCost}|{AsphaltHighwayCost}";
        }
        
#if SERVER
        private static string FilePath => Path.Combine(Master.ConfigsPath, "RoadConfig.json");

        public static RoadValuesFile Load()
        {
            if (File.Exists(FilePath))
            {
                return Serializer.SerializeFromFile<RoadValuesFile>(FilePath);
            }
            else
            {
                var obj = new RoadValuesFile();
                Serializer.SerializeToFile(FilePath, obj);
                return obj;
            }
        }

        public static bool Save()
        {
            try
            {
                Serializer.SerializeToFile(FilePath, Master.RoadValues);
                return true;
            }
            catch
            {
                return false;
            }
        }
#endif
    }
}
