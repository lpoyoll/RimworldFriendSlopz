using GameClient.Files;
using Shared;
using System.IO;

namespace GameClient.Core.Preferences
{
    public static class ConnectionDataHandler
    {
        public static void SaveConnectionData(string ip, string port)
        {
            ConnectionDataFile newConnectionData;
            if (File.Exists(Master.ConnectionDataPath)) newConnectionData = Serializer.SerializeFromFile<ConnectionDataFile>(Master.ConnectionDataPath);
            else newConnectionData = new ConnectionDataFile();

            newConnectionData.IP = ip;
            newConnectionData.Port = port;

            Serializer.SerializeToFile(Master.ConnectionDataPath, newConnectionData);
        }

        public static ConnectionDataFile LoadConnectionData()
        {
            if (File.Exists(Master.ConnectionDataPath)) return Serializer.SerializeFromFile<ConnectionDataFile>(Master.ConnectionDataPath);
            else return new ConnectionDataFile();
        }
    }
}