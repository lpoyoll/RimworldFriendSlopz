using System;
using System.IO;

namespace Shared.Files.Configs
{
    public class ServerBrowserConfigFile : BaseFile
    {
        public static string SavePath { get; set; } = string.Empty;

        public bool EnableServerBrowser { get; set; } = false;

        public bool EnableServerTelemetry { get; set; } = true;

        public string PublicEndPoint { get; set; } = string.Empty;

        public override void Save()
        {
            try { Serializer.SerializeToFile(SavePath, this); }
            catch (Exception e) { throw new Exception(e.ToString()); }
        }

        public static object Load<T>()
        {
            if (File.Exists(SavePath)) return Serializer.SerializeFromFile<T>(SavePath);
            else
            {
                ServerBrowserConfigFile file = new ServerBrowserConfigFile();
                Serializer.SerializeToFile(SavePath, file);
                return file;
            }
        }
    }
}
