using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            if (File.Exists(FilePath))
            {
                return Serializer.SerializeFromFile<ServerBrowserConfig>(FilePath);
            }
            else
            {
                var obj = new ServerBrowserConfig();
                Serializer.SerializeToFile(FilePath, obj);
                return obj;
            }
        }

        public static void Save()
        {
            Serializer.SerializeToFile(FilePath, Master.ServerBrowserConfig);
        }
#endif

    }
}
