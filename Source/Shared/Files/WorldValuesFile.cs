using System;
using System.Text;
using Newtonsoft.Json;
using Shared.Misc;

#if SERVER
using GameServer.Misc;
using GameServer.Core;
#endif

namespace Shared
{
    [Serializable]
    public class WorldValuesFile
    {
        public int PersistentRandomValue { get; set; } = -1;

        public string SeedString { get; set; } = string.Empty;

        public float PlanetCoverage { get; set; } = -1f;

        public int Rainfall { get; set; } = -1;

        public int Temperature { get; set; } = -1;

        public int Population { get; set; } = -1;

        public int LandmarkDensity { get; set; } = -1;

        public float Pollution { get; set; } = -1f;

        public WorldTilesFile Tiles { get; set; } = null;

        public PlanetFeatureDetails[] Features { get; set; } = null;

        public RoadDetails[] Roads { get; set; } = null;

        public RiverDetails[] Rivers { get; set; } = null;

        public PollutionDetails[] PollutedTiles { get; set; } = null;

        public PlanetNPCFactionDetails[] NPCFactions { get; set; } = null;

        public PlanetNPCSettlementDetails[] NPCSettlements { get; set; } = null;

        public override string ToString()
        {
            return $"WorldValuesFile:|{PersistentRandomValue}|{SeedString}";
        }

#if SERVER
        public static string FilePath => Path.Combine(Master.WorldPath, "WorldValuesFile.json");

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
