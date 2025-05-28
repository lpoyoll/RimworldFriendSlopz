using Shared;

namespace GameServer.Core.Configs
{
    public class WhitelistConfigFile
    {
        public bool UseWhitelist = false;

        public List<string> WhitelistedUsers = new List<string>() { };

#if SERVER
        private static string FilePath => Path.Combine(Master.ConfigsPath, "WhitelistConfig.json");

        public static WhitelistConfigFile Load()
        {
            if (File.Exists(FilePath)) return Serializer.SerializeFromFile<WhitelistConfigFile>(FilePath);
            else
            {
                WhitelistConfigFile obj = new WhitelistConfigFile();
                Serializer.SerializeToFile(FilePath, obj);
                return obj;
            }
        }

        public static bool Save()
        {
            try
            {
                Serializer.SerializeToFile(FilePath, Master.Whitelist);
                return true;
            }
            catch { return false; }
        }
#endif

    }
}
