using Shared.Details.Planet;
using Shared.Files;
using Shared.Files.Actions;
using Shared.Files.Configs;
using Shared.Files.Configs.Mods;
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

        public List<EventFile> _eventValues { get; set; } = new List<EventFile>();

        public ActionsConfigFile _actionValues { get; set; } = null;

        public RoadsConfigFile _roadValues { get; set; } = null;

        public ScenarioConfigFile _scenarioValues { get; set; } = null;

        public StorytellerConfigFile _storytellerValues { get; set; } = null;

        public DifficultyConfigFile _difficultyValues { get; set; } = null;

        public List<NPCSettlementDetail> _npcSettlements { get; set; } = new List<NPCSettlementDetail>();

        public List<SettlementFile> _playerSettlements { get; set; } = new List<SettlementFile>();

        public List<SiteFile> _playerSites { get; set; } = new List<SiteFile>();

        public List<RoadDetail> _roads { get; set; } = new List<RoadDetail>();

        public List<PollutionDetail> _pollutedTiles { get; set; } = new List<PollutionDetail>();

        public List<ModConfig> _modConfigs { get; set; } = new List<ModConfig>();
    }
}