using Shared;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using TCPNetwork.Packets;

namespace TCPNetwork.Files.Client
{
    public class ServerClient
    {
        public string CurrentIP { get; private set; } = string.Empty;

        public bool IsVerified { get; private set; } = false;

        public object ClientData { get; private set; } = null;

        public Listener Listener { get; private set; } = null;

        public TcpClient Tcp { get; private set; } = null;

        public NetworkRuleset Ruleset { get; private set; } = null;

        public ServerClient(TcpClient tcp, NetworkRuleset ruleset, bool createListener = true)
        {
            if (tcp == null) return;
            else
            {
                Tcp = tcp;
                Ruleset = ruleset;
                CurrentIP = ((IPEndPoint)tcp.Client.RemoteEndPoint).Address.ToString();
                if (createListener) CreateListener();
            }
        }

        public void CreateListener() { Listener = new Listener(this, Tcp, Ruleset); }

        public void VerifyClient() { IsVerified = true; }

        public void DisposeTCP() { Tcp.Dispose(); }

        public T GetData<T>(object obj = null)
        {
            if (ClientData == null) ClientData = obj;
            return (T)ClientData;
        }
    }
}