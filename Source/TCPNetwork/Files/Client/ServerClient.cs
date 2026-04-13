using Shared;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace TCPNetwork.Files.Client
{
    public class ServerClient
    {
        public string CurrentIP { get; set; } = string.Empty;

        public bool IsVerified { get; private set; } = false;

        public UserFile UserFile { get; set; } = null;

        public Listener Listener { get; set; } = null;

        public TcpClient Tcp { get; set; } = null;

        public NetworkRuleset Ruleset { get; set; } = null;

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

        public void DisposeTCP() { Tcp.Dispose(); }

        public void VerifyUser() { IsVerified = true; }

        public void LoadUserFromFile(ServerClient client) 
        { 
            string[] userFiles = Directory.GetFiles(CommonValues.ServerUsersPath);

            foreach (string userFile in userFiles)
            {
                UserFile file = Serializer.SerializeFromFile<UserFile>(userFile);
                if (file.Username == client.UserFile.Username)
                {
                    UserFile = file;
                    UserFile.UpdateIP(CurrentIP);
                    break;
                }
            }
        }
    }
}