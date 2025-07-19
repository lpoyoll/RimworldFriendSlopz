using Shared;

namespace GameServer.Core.Configs
{
    public class ServerBrowserConfig
    {
        public bool EnableServerBrowser = false;

        public string PublicEndPoint = "";

#if SERVER
        private static string FilePath => Path.Combine(Master.ConfigsPath, "ServerBrowserConfig.json");

        public static ServerBrowserConfig Load()
        {
            if (File.Exists(FilePath)) return Serializer.SerializeFromFile<ServerBrowserConfig>(FilePath);
            else
            {
                ServerBrowserConfig obj = new ServerBrowserConfig();
                Serializer.SerializeToFile(FilePath, obj);
                return obj;
            }
        }

        public static bool Save()
        {
            try
            {
                Serializer.SerializeToFile(FilePath, Master.ServerBrowserConfig);
                return true;
            }
            catch { return false; }
        }
#endif

    }
}
