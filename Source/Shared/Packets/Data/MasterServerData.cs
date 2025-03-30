namespace Shared.MasterServer
{
    //These classes should not be touched without notifying me first. The fields need to exactly match in both name and type to the master server.
    public class ServerInfo
    {
        public string _ip;
        public string _name;
        public string _description;
        public ModConfigFile _config;
        public int _maximumPlayerCount;
        public int _currentPlayerCount;
        public int _port;
    }
    public class AllServersPacket
    {
        public ServerInfo[] _serverInfos;
    }
}