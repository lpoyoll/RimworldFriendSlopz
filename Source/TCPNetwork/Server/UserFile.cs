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
        //DO NOT USE PRIVATE SET ON THIS VARIABLES
        //IT WILL CAUSE DESERIALIZATION TO WORK INCORRECTLY

        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string Hash { get; set; } = string.Empty;

        public string LatestIP { get; set; } = null;

        public string GuildName { get; set; } = null;

        public bool IsAdmin { get; set; } = false;

        public bool IsBanned { get; set; } = false;

        public double EventProtectionTime { get; set; } = -1;

        public double AidProtectionTime { get; set; } = -1;

        public List<string> AllyPlayers { get; set; } = new List<string>();

        public List<string> EnemyPlayers { get; set; } = new List<string>();

        public SiteConfigFile[] SiteConfigs { get; set; } = Array.Empty<SiteConfigFile>();

        private Semaphore SavingSemaphore { get; set; } = new Semaphore(1, 1);

        public void UpdateLoginDetails(LoginData data)
        {
            Username = data._username;
            Password = data._password;
        }

        public void SaveUserFile()
        {
            SavingSemaphore.WaitOne();

            try { Serializer.SerializeToFile(Path.Combine(CommonValues.ServerUsersPath, Username + CommonValues.DefaultSaveFormat), this); }
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

        public void UpdateAdmin(bool mode)
        {
            IsAdmin = mode;
            SaveUserFile();
        }

        public void UpdateBan(bool mode)
        {
            IsBanned = mode;
            SaveUserFile();
        }

        public void UpdateIP(string IP)
        {
            LatestIP = IP;
            SaveUserFile();
        }

        public void UpdateSiteConfigs(SiteConfigFile[] configs)
        {
            SiteConfigs = configs;
            SaveUserFile();
        }

        public void UpdateHash() 
        { 
            Hash = Hasher.GetHashFromString($"{Username}:{Password}");
            SaveUserFile();
        }
    }
}
