namespace Shared
{

    public class ServerGlobalData
    {
        public bool _isClientAdmin { get; set; }

        public bool _isClientFactionMember { get; set; }

        public ServerValuesFile _serverValues { get; set; }

        public SiteValuesFile _siteValues { get; set; }

        public EventFile[] _eventValues { get; set; }

        public ActionValuesFile _actionValues { get; set; }

        public RoadValuesFile _roadValues { get; set; }

        public ScenarioValuesFile _scenarioValues { get; set; }

        public StorytellerValuesFile _storytellerValues { get; set; }

        public DifficultyValuesFile _difficultyValues { get; set; }

        public PlanetNPCSettlementDetails[] _npcSettlements { get; set; }

        public SettlementFile[] _playerSettlements { get; set; }

        public SiteFile[] _playerSites { get; set; }

        public RoadDetails[] _roads { get; set; }

        public PollutionDetails[] _pollutedTiles { get; set; }

        public ModConfigFile _modConfigs { get; set; }
    }
}