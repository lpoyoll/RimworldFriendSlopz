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
                IP = ((IPEndPoint)tcp.Client.RemoteEndPoint).Address.ToString();
                if (createListener) CreateListener();
            }
        }

        public void CreateListener() { Listener = new Listener(this, Tcp, Ruleset); }

        public void VerifyClient() 
        { 
            IsVerified = true;

            Printer.Message($"Handshake with '{IP}' was valid", Printer.LogImportanceMode.Verbose);
        }

        public void DisposeTCP() { Tcp.Dispose(); }

        public T GetData<T>(object obj = null)
        {
            if (ClientData == null) ClientData = obj;
            return (T)ClientData;
        }
    }
}