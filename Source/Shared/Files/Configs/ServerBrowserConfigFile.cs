using System;
using System.IO;

namespace Shared.Files.Configs
{
    public class ServerBrowserConfigFile : BaseFile
    {
        public static string Path { get; set; } = string.Empty;

        public bool EnableServerBrowser { get; set; } = false;

        public bool EnableServerTelemetry { get; set; } = true;

        public string PublicEndPoint { get; set; } = string.Empty;

        public override void Save()
        {
            try { Serializer.SerializeToFile(Path, this); }
            catch (Exception e) { throw new Exception(e.ToString()); }
        }

        public static object Load<T>()
        {
            if (File.Exists(Path)) return Serializer.SerializeFromFile<T>(Path);
            else
            {
                ServerBrowserConfigFile file = new ServerBrowserConfigFile();
                Serializer.SerializeToFile(Path, file);
                return file;
            }
        }
    }
}
