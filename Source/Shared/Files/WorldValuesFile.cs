using System;
#if SERVER
using GameServer.Core;
#endif
namespace Shared
{
    [Serializable]
    public class WorldValuesFile
    {
        //Misc

        public int PersistentRandomValue;

        //World Values

        public string SeedString;

        public float PlanetCoverage;

        public int Rainfall;

        public int Temperature;

        public int Population;
        
        public float Pollution;

        //World features

        public string[] Tiles;

        public PlanetFeatureDetails[] Features;

        public RoadDetails[] Roads;

        public RiverDetails[] Rivers;

        public PollutionDetails[] PollutedTiles;

        public PlanetNPCFactionDetails[] NPCFactions;

        public PlanetNPCSettlementDetails[] NPCSettlements;

        public override string ToString()
        {
            return $"WorldValuesFile:|{PersistentRandomValue}|{SeedString}";
        }
#if SERVER
        private static string FilePath => Path.Combine(Master.ConfigsPath, "WorldConfig.json");

        public static WorldValuesFile Load()
        {
            //We don't want to generate the world if it doesn't exist, this task is for the first player to do

            if (File.Exists(FilePath)) return Serializer.FileBytesToObject<WorldValuesFile>(FilePath);
            else return null;
        }

        public static bool Save()
        {
            try
            {
                Serializer.ObjectBytesToFile(FilePath, Master.WorldValues);
                return true;
            }
            catch { return false; }
        }
#endif
    }
}
