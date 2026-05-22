using Shared;
using System;

namespace TCPNetwork
{
    public class NetworkRuleset
    {
        public Action<ServerClient> OnConnect { get; set; } = null;

        public Action<ServerClient> OnDisconnect { get; set; } = null;

        public Action<PacketHeader, byte[], ServerClient> OnRead { get; set; } = null;

        public Action<ServerClient> OnWrite { get; set; } = null;

        public bool HandleKeepAlive { get; set; } = false;

        public NetworkRuleset(Action<ServerClient> onConnect, Action<ServerClient> onDisconnect, 
            Action<PacketHeader, byte[], ServerClient> onRead, Action<ServerClient> onWrite, bool handleKeepAlive = true)
        {
            this.OnConnect = onConnect;
            this.OnDisconnect = onDisconnect;
            this.OnRead = onRead;
            this.OnWrite = onWrite;
            this.HandleKeepAlive = handleKeepAlive;
        }
    }
}
