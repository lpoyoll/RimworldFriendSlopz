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

        public static UserFile LoadOrCreateUserFile(ServerClient client, PKT_Login data)
        {
            List<UserFile> files = new List<UserFile>();
            string[] userFiles = Directory.GetFiles(CommonValues.ServerUsersPath);
            foreach (string userFile in userFiles) files.Add(Serializer.SerializeFromFile<UserFile>(userFile));

            UserFile toFind = files.FirstOrDefault(fetch => fetch.Username == data._username && fetch.Password == data._password);
            if (toFind != null) return toFind;
            else
            {
                toFind = new UserFile();
                toFind.Username = data._username;
                toFind.Password = data._password;
                toFind.Hash = Hasher.GetHashFromString($"{toFind.Username}:{toFind.Password}");
                toFind.SaveUserFile();

                return toFind;
            }
        }
    }
}