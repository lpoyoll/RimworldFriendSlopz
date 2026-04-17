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
        public string _serverName { get; set; } = null;

        public bool _isClientAdmin { get; set; } = false;

        public bool _isClientFactionMember { get; set; } = false;

        public List<SiteType> _siteValues { get; set; } = new List<SiteType>();

        public List<FL_Event> _eventValues { get; set; } = new List<FL_Event>();

        public FL_ActionsConfig _actionValues { get; set; } = null;

        public FL_RoadsConfig _roadValues { get; set; } = null;

        public FL_ScenarioConfig _scenarioValues { get; set; } = null;

        public FL_StorytellerConfig _storytellerValues { get; set; } = null;

        public FL_DifficultyConfig _difficultyValues { get; set; } = null;

        public List<NPCSettlementDetail> _npcSettlements { get; set; } = new List<NPCSettlementDetail>();

        public List<FL_Settlement> _playerSettlements { get; set; } = new List<FL_Settlement>();

        public List<Site> _playerSites { get; set; } = new List<Site>();

        public List<RoadDetail> _roads { get; set; } = new List<RoadDetail>();

        public List<PollutionDetail> _pollutedTiles { get; set; } = new List<PollutionDetail>();

        public List<ModConfig> _modConfigs { get; set; } = new List<ModConfig>();
    }
}