namespace GameServer.Files
{
    [Serializable]
    public class ServerConfigFile
    {
        public string Name = "RimWorld Together Server";

        public string IP = "0.0.0.0";

        public string Port = "25555";

        public string MaxPlayers = "100";

        public string MaxTimeoutInMS = "30000";

        public bool VerboseLogs = false;

        public bool ExtremeVerboseLogs = false;

        public bool DisplayChatInConsole = false;

        public bool UseUPnP = false;

        public bool SyncLocalSave = true;

        public bool TemporalActivityProtection = false;

        public bool TemporalEventProtection = false;

        public bool TemporalAidProtection = false;
    }
}
