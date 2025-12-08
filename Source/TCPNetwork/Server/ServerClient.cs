using Shared;
using Shared.Files;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace TCPNetwork.Server
{
    [Serializable]
    public class ServerClient
    {
        public string ConnectedIP { get; set; } = string.Empty;

        public UserFile UserFile { get; set; } = null;

        public Listener Listener { get; set; } = null;

        public ServerClient(TcpClient tcp)
        {
            if (tcp == null) return;
            else ConnectedIP = ((IPEndPoint)tcp.Client.RemoteEndPoint).Address.ToString();
        }

        public void LoadUserFromFile(ServerClient client) 
        { 
            string[] userFiles = Directory.GetFiles(CommonValues.ServerUsersPath);

            foreach (string userFile in userFiles)
            {
                UserFile file = Serializer.SerializeFromFile<UserFile>(userFile);
                if (file.Username == client.UserFile.Username)
                {
                    UserFile = file;
                    UserFile.SavedIP = ConnectedIP;
                    break;
                }
            }
        }
    }
}