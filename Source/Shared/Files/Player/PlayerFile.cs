using Newtonsoft.Json;
using Shared;
using Shared.Files;
using Shared.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using static Shared.CommonEnumerators;

namespace Shared.Files.ServerClient
{
    public class PlayerFile
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

        [JsonIgnore] public byte SynchronousClientID { get; set; } = byte.MinValue;

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

        public static PlayerFile LoadOrCreateUserFile(string username, string password)
        {
            List<PlayerFile> files = new List<PlayerFile>();
            string[] userFiles = Directory.GetFiles(CommonValues.ServerUsersPath);
            foreach (string userFile in userFiles) files.Add(Serializer.SerializeFromFile<PlayerFile>(userFile));

            PlayerFile toFind = files.FirstOrDefault(fetch => fetch.Username == username && fetch.Password == password);
            if (toFind != null) return toFind;
            else
            {
                toFind = new PlayerFile();
                toFind.Username = username;
                toFind.Password = password;
                toFind.Hash = Hasher.GetHashFromString($"{toFind.Username}:{toFind.Password}");
                toFind.SaveUserFile();

                return toFind;
            }
        }
    }
}
