namespace Shared.MasterServer
{
    //These classes should not be touched without notifying me first. The fields need to exactly match in both name and type to the master server.
    public class ServerInfo
    {
        public string _ip;
        public string _name;
        public string _description;
        public string[] _runningModsByNameRequired;
        public string[] _runningModsByNameOptional;
        public string[] _runningModsByNameForbidden;
        public int _maximumPlayerCount;
        public int _currentPlayerCount;
        public int _port;
    }
    public class PlayerCountPacket
    {
        public short _playerCount;
    }
    public class AllServersPacket
    {
        public ServerInfo[] _serverInfos;
    }
}