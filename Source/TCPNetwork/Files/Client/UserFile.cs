using Newtonsoft.Json;
using Shared;
using Shared.Files;
using Shared.Files.Sites;
using Shared.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using TCPNetwork.Packets;
using static Shared.CommonEnumerators;

namespace TCPNetwork.Files.Client
{
    public class UserFile
    {
        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string Hash { get; set; } = string.Empty;

        public string LatestIP { get; set; } = null;

        public string GuildName { get; set; } = null;

        public bool IsAdmin { get; set; } = false;

        public bool IsBanned { get; set; } = false;

        public PlayerCooldown Cooldowns { get; set; } = new PlayerCooldown();

        public List<PlayerGoodwill> Goodwills { get; set; } = new List<PlayerGoodwill>();

        public List<PlayerSiteConfig> SiteConfigs { get; set; } = new List<PlayerSiteConfig>();

        [JsonIgnore] public ServerClient SynchronousClient { get; set; } = null;

        private Semaphore SavingSemaphore { get; set; } = new Semaphore(1, 1);

        public void SaveUserFile()
        {
            SavingSemaphore.WaitOne();

            try { Serializer.SerializeToFile(Path.Combine(CommonValues.ServerUsersPath, Username + CommonValues.DefaultSaveFormat), this); }
            catch (Exception e) { throw new Exception(e.ToString()); }

            SavingSemaphore.Release();
        }

        public void UpdateFaction(FL_Guild toUpdateWith)
        {
            if (toUpdateWith == null) GuildName = null;
            else GuildName = toUpdateWith.Name;

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

        public void UpdateGoodwill(string username, Goodwill goodwill)
        {
            PlayerGoodwill toFind = Goodwills.FirstOrDefault(fetch => fetch.Name == username);
            if (toFind != null) toFind.Goodwill = goodwill;
            else
            {
                PlayerGoodwill newGoodwill = new PlayerGoodwill();
                newGoodwill.Name = username;
                newGoodwill.Goodwill = goodwill;

                Goodwills.Add(newGoodwill);
            }

            SaveUserFile();
        }

        public void UpdateSiteConfigs(List<FL_SiteType> configs)
        {
            foreach (FL_SiteType type in configs)
            {
                if (SiteConfigs.FirstOrDefault(fetch => fetch.DefName == type.DefName) == null)
                {
                    PlayerSiteConfig newConfig = new PlayerSiteConfig();
                    newConfig.DefName = type.DefName;
                    newConfig.Reward = type.Rewards[0];
                    SiteConfigs.Add(newConfig);
                }
            }

            SaveUserFile();
        }

        public static UserFile LoadOrCreateUserFile(ServerClient client, PKT_Login data)
        {
            List<UserFile> files = new List<UserFile>();
            string[] userFiles = Directory.GetFiles(CommonValues.ServerUsersPath);
            foreach (string userFile in userFiles) files.Add(Serializer.SerializeFromFile<UserFile>(userFile));

            UserFile toFind = files.FirstOrDefault(fetch => fetch.Username == data._username && fetch.Password == data._password);
            if (toFind != null) return toFind;
            else
            {
                toFind = new UserFile();
                toFind.Username = data._username;
                toFind.Password = data._password;
                toFind.Hash = Hasher.GetHashFromString($"{toFind.Username}:{toFind.Password}");
                toFind.SaveUserFile();

                return toFind;
            }
        }
    }
}
