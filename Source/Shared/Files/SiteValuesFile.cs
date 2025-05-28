#if SERVER
using GameServer.Core;
#endif
namespace Shared
{
    public class SiteValuesFile
    {
        public int TimeIntervalMinutes = 30;
        
        public SiteInfoFile[] SiteInfoFiles = new SiteInfoFile[0];

        //Override ToString() once rework is done

#if SERVER
        private static string FilePath => Path.Combine(Master.ConfigsPath, "SiteConfig.json");

        public static SiteValuesFile Load()
        {
            if (File.Exists(FilePath)) return Serializer.SerializeFromFile<SiteValuesFile>(FilePath);
            else
            {
                SiteValuesFile obj = new SiteValuesFile();
                Serializer.SerializeToFile(FilePath, obj);
                return obj;
            }
        }

        public static bool Save()
        {
            try
            {
                Serializer.SerializeToFile(FilePath, Master.SiteValues);
                return true;
            }
            catch { return false; }
        }
#endif

    }
}
