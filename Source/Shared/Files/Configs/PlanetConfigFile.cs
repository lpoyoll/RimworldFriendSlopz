using Shared.Details.Planet;
using System.Collections.Generic;

namespace Shared.Files.Configs
{
    public class PlanetConfigFile : BaseFile
    {
        public static string SavePath { get; set; } = string.Empty;

        public int PersistentRandomValue { get; set; } = int.MaxValue;

        public string SeedString { get; set; } = string.Empty;

        public float PlanetCoverage { get; set; } = float.MaxValue;

        public int Rainfall { get; set; } = -1;

        public int Temperature { get; set; } = int.MaxValue;

        public int Population { get; set; } = int.MaxValue;

        public int LandmarkDensity { get; set; } = int.MaxValue;

        public float Pollution { get; set; } = float.MaxValue;

        public List<FeatureDetail> Features { get; set; } = new List<FeatureDetail>();

        public List<RoadDetail> Roads { get; set; } = new List<RoadDetail>();

        public List<PollutionDetail> PollutedTiles { get; set; } = new List<PollutionDetail>();

        public List<NPCFactionDetail> NPCFactions { get; set; } = new List<NPCFactionDetail>();

        public List<NPCSettlementDetail> NPCSettlements { get; set; } = new List<NPCSettlementDetail>();
    }
}
