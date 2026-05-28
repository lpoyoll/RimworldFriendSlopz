using Shared;
using Shared.Misc;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using TCPNetwork.Packets;

namespace TCPNetwork
{
    public class ServerClient
    {
        public string IP { get; private set; } = string.Empty;

        public byte ID { get; private set; } = byte.MinValue;

        public bool IsVerified { get; private set; } = false;

        public object ClientData { get; private set; } = null;

        public Listener Listener { get; private set; } = null;

        public TcpClient Tcp { get; private set; } = null;

        public ServerClient(TcpClient tcp, NetworkRuleset ruleset, bool createListener = true)
        {
            if (tcp == null) return;
            else
            {
                Tcp = tcp;
                ID = (byte)Network.ServerClients.Keys.Count;
                IP = ((IPEndPoint)tcp.Client.RemoteEndPoint).Address.ToString();
                if (createListener) CreateListener(ruleset);
            }
        }

        public void CreateListener(NetworkRuleset ruleset) { Listener = new Listener(this, Tcp, ruleset); }

        public void VerifyClient() 
        { 
            IsVerified = true;
            Printer.Warning($"Handshake with '{IP}' was valid", Printer.Verbosity.Extreme);
        }

        public void DisposeTCP() { Tcp.Dispose(); }

        public T GetData<T>(object obj = null)
        {
            if (ClientData == null) ClientData = obj;
            return (T)ClientData;
        }
    }
}