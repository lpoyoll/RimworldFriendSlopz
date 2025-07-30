using System.Net;
using System.Net.Sockets;
using GameServer.Files;
using GameServer.Managers;

namespace Shared.Network.Server
{
    //Class object for the client connecting into the server. Contains all important data about it

    [Serializable]
    public class ServerClient
    {
        //Contains a reference to the user file of the client

        public UserFile UserFile { get; private set; } = new UserFile();

        //Variables

        [NonSerialized] public Listener Listener;

        public ServerClient(TcpClient tcp)
        {
            if (tcp == null) return;
            else UserFile.SavedIP = ((IPEndPoint)tcp.Client.RemoteEndPoint).Address.ToString();
        }

        public void LoadUserFromFile() { UserFile = UserManagerH.GetUserFile(this); }
    }
}