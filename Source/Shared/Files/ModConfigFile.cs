#if SERVER
using GameServer.Core;
#endif
namespace Shared
{
    public class ModConfigFile
    {
        public string[] UnsortedMods = new string[0];

        public ulong[] AllModIds = new ulong[0];

        public string[] RequiredMods = new string[0];

        public string[] OptionalMods = new string[0];

        public string[] ForbiddenMods = new string[0];

        public bool EnforcedConfigs = false;

        public string[] ModFileNames = new string[0];

        public string[] ModConfigs = new string[0];

        public override string ToString()
        {
            return $"ModConfigFile:|Total Mods : {UnsortedMods?.Length ?? 0}";
        }
#if SERVER
        private static string FilePath => Path.Combine(Master.ConfigsPath, "ModConfig.json");

        public static ModConfigFile Load()
        {
            if (File.Exists(FilePath)) return Serializer.SerializeFromFile<ModConfigFile>(FilePath);
            else
            {
                ModConfigFile obj = new ModConfigFile();
                Serializer.SerializeToFile(FilePath, obj);
                return obj;
            }
        }

        public static bool Save()
        {
            try
            {
                Serializer.SerializeToFile(FilePath, Master.ModConfig);
                return true;
            }
            catch { return false; }
        }
#endif
    }
}