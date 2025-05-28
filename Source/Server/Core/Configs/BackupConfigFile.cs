using Shared;

namespace GameServer.Core.Configs
{
    public class BackupConfigFile
    {
        public bool AutomaticBackups = true;

        public float IntervalHours = 24f;

        public bool AutomaticDeletion = true;

        public int Amount = 3;

#if SERVER
        private static string FilePath => Path.Combine(Master.ConfigsPath, "BackupConfig.json");

        public static BackupConfigFile Load()
        {
            if (File.Exists(FilePath)) return Serializer.SerializeFromFile<BackupConfigFile>(FilePath);
            else
            {
                BackupConfigFile obj = new BackupConfigFile();
                Serializer.SerializeToFile(FilePath, obj);
                return obj;
            }
        }

        public static bool Save()
        {
            try
            {
                Serializer.SerializeToFile(FilePath, Master.BackupConfig);
                return true;
            }
            catch { return false; }
        }
#endif
    }
}
