using Shared.Details.Planet;
using Shared.Files;
using Shared.Files.Actions;
using Shared.Files.Configs;
using Shared.Files.Mods;
using Shared.Files.Sites;
using System.Collections.Generic;

namespace TCPNetwork.Packets
{
    public class PKT_ServerGlobalData : PKT_Base
    {
        public bool IsClientAdmin { get; set; } = false;

        public bool IsClientFactionMember { get; set; } = false;

        public List<FL_SiteType> SiteValues { get; set; } = new List<FL_SiteType>();

        public List<FL_Event> EventValues { get; set; } = new List<FL_Event>();

        public FL_ActionsConfig ActionValues { get; set; } = null;

        public FL_RoadsConfig RoadValues { get; set; } = null;

        public FL_ScenarioConfig ScenarioValues { get; set; } = null;

        public FL_StorytellerConfig StorytellerValues { get; set; } = null;

        public FL_DifficultyConfig DifficultyValues { get; set; } = null;

        public List<NPCSettlementDetail> _npcSettlements { get; set; } = new List<NPCSettlementDetail>();

        public List<FL_Settlement> PlayerSettlements { get; set; } = new List<FL_Settlement>();

        public List<FL_Site> PlayerSites { get; set; } = new List<FL_Site>();

        public List<RoadDetail> Roads { get; set; } = new List<RoadDetail>();

        public List<PollutionDetail> PollutedTiles { get; set; } = new List<PollutionDetail>();

        public List<ModConfig> ModConfigs { get; set; } = new List<ModConfig>();
    }
}