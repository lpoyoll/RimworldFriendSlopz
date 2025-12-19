using GameClient.Core;
using Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GameClient.Files
{
    public class PersistentSettings
    {
        public ServerSettings ServerSettings { get; set; } = new ServerSettings();

        public UserSettings UserSettings { get; set; } = new UserSettings();

        public static string FilePath { get; set; } = string.Empty;

        public static void SetFilePath(string path) { FilePath = path; }

        public void Save() { Serializer.SerializeToFile(FilePath, this); }

        public static PersistentSettings Load()
        {
            if (!File.Exists(FilePath)) Regenerate();

            return Serializer.SerializeFromFile<PersistentSettings>(FilePath);
        }

        public static void Regenerate()
        {
            PersistentSettings settings = new PersistentSettings();
            settings.Save();
        }
    }

    public class ServerSettings
    {
        public string LatestIP { get; set; } = string.Empty;

        public string LatestPort { get; set; } = string.Empty;

        public void Set (string ip, string port)
        {
            LatestIP = ip;
            LatestPort = port;
        }

        public void Reset()
        {
            LatestIP = string.Empty;
            LatestPort = string.Empty;
        }
    }

    public class UserSettings
    {
        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public void Set(string username, string password)
        {
            Username = username;
            Password = password;
        }

        public void Reset()
        {
            Username = string.Empty;
            Password = string.Empty;
        }
    }
}
