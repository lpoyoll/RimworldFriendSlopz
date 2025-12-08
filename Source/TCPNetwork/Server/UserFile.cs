using Shared;
using Shared.Files;
using Shared.Files.Guild;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using TCPNetwork.Packets;
using static Shared.CommonEnumerators;

namespace TCPNetwork.Server
{
    public class UserFile
    {
        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public bool IsAdmin { get; set; } = false;

        public bool IsBanned { get; set; } = false;

        public string SavedIP { get; set; } = string.Empty;

        public double EventProtectionTime { get; set; } = -1;

        public double AidProtectionTime { get; set; } = -1;

        public string GuildName { get; set; } = string.Empty;

        public List<string> AllyPlayers { get; set; } = new List<string>();

        public List<string> EnemyPlayers { get; set; } = new List<string>();

        public SiteConfigFile[] SiteConfigs { get; set; } = Array.Empty<SiteConfigFile>();

        private Semaphore SavingSemaphore { get; set; } = new Semaphore(1, 1);

        public static string fileExtension { get; set; } = ".mpuser";

        public void SetLoginDetails(LoginData data)
        {
            Username = data._username;
            Password = data._password;
        }

        public void SaveUserFile()
        {
            SavingSemaphore.WaitOne();

            try { Serializer.SerializeToFile(Path.Combine(CommonValues.ServerUsersPath, Username + fileExtension), this); }
            catch (Exception e) { throw new Exception(e.ToString()); }

            SavingSemaphore.Release();
        }

        public void UpdateFaction(GuildFile toUpdateWith)
        {
            if (toUpdateWith == null) GuildName = null;
            else GuildName = toUpdateWith.Name;

            SaveUserFile();
        }

        public void UpdateEventTime()
        {
            EventProtectionTime = TimeConverter.GetCurrentTimeToEpoch();
            SaveUserFile();
        }

        public void UpdateAidTime()
        {
            AidProtectionTime = TimeConverter.GetCurrentTimeToEpoch();
            SaveUserFile();
        }

        public void UpdateAdmin(bool mode, ServerClient connectedClient = null)
        {
            if (connectedClient != null)
            {
                connectedClient.UserFile.IsAdmin = true;

                CommandData commandData = new CommandData();
                commandData._commandMode = CommandMode.Op;
                connectedClient.Listener.EnqueuePacket(PacketHeader.ConsoleManager, commandData);
            }

            IsAdmin = mode;
            SaveUserFile();
        }

        public void UpdateBan(bool mode)
        {
            IsBanned = mode;
            SaveUserFile();
        }
    }
}
