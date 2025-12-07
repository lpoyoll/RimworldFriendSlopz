using GameServer.Core;
using GameServer.Files;
using GameServer.Misc;
using Shared;
using Shared.Files;
using Shared.Files.Guild;
using TCPNetwork.Packets;
using TCPNetwork.Server;
using static Shared.CommonEnumerators;
using static Shared.Files.Guild.GuildMember;

namespace GameServer.Managers
{

    public static class GuildManager
    {
        [HandlesPacket(PacketHeader.GuildManager)]
        private static void ParsePacket(ServerClient client, byte[] bytes)
        {
            if (!Master.ActionConfigs.EnableFactions)
            {
                ResponseShortcutManager.SendIllegalPacket(client, "Tried to use disabled feature!");
                return;
            }

            PlayerGuildData data = Serializer.ConvertBytesToObject<PlayerGuildData>(bytes);

            Printer.Warning(data, LogImportanceMode.Extreme);

            switch (data._stepMode)
            {
                case GuildStepMode.Create:
                    CreateFaction(client, data);
                    break;

                case GuildStepMode.Delete:
                    DeleteFaction(client, data);
                    break;

                case GuildStepMode.AddMember:
                    AddMemberToFaction(client, data);
                    break;

                case GuildStepMode.RemoveMember:
                    RemoveMemberFromFaction(client, data);
                    break;

                case GuildStepMode.AcceptInvite:
                    ConfirmAddMemberToFaction(client, data);
                    break;

                case GuildStepMode.Promote:
                    PromoteMember(client, data);
                    break;

                case GuildStepMode.Demote:
                    DemoteMember(client, data);
                    break;

                case GuildStepMode.MemberList:
                    SendFactionMemberList(client, data);
                    break;
            }
        }

        private static void CreateFaction(ServerClient client, PlayerGuildData factionManifest)
        {
            if (GuildManagerH.CheckIfFactionExistsByName(factionManifest._file.Name))
            {
                factionManifest._stepMode = GuildStepMode.NameInUse;

                client.Listener.EnqueuePacket(PacketHeader.GuildManager, factionManifest);
            }

            else
            {
                factionManifest._stepMode = GuildStepMode.Create;

                GuildMember member = new GuildMember();
                member.Username = client.UserFile.Username;
                member.Rank = GuildMember.GuildRanks.Admin;

                GuildFile factionFile = new GuildFile();
                factionFile.Name = factionManifest._file.Name;
                factionFile.GuildMembers.Add(member);

                GuildManagerH.SaveFactionFile(factionFile);

                foreach (SiteFile site in SiteManagerHelper.GetAllSitesFromUsername(client.UserFile.Username))
                {
                    SiteManagerHelper.UpdateFaction(site, factionFile);
                }

                client.Listener.EnqueuePacket(PacketHeader.GuildManager, factionManifest);

                InformationDisplayer.DisplayAddFaction(factionFile.Name);
            }
        }

        private static void DeleteFaction(ServerClient client, PlayerGuildData factionManifest)
        {
            if (!GuildManagerH.CheckIfFactionExistsByName(client.UserFile.GuildName)) return;
            else
            {
                GuildFile factionFile = GuildManagerH.GetFactionFromFactionName(client.UserFile.GuildName);

                if (GuildManagerH.GetMemberRank(factionFile, client.UserFile.Username) != GuildRanks.Admin)
                {
                    ResponseShortcutManager.SendNoPowerPacket(client, factionManifest);
                }

                else
                {
                    factionManifest._stepMode = GuildStepMode.Delete;

                    UserFile[] toUpdateOffline = GuildManagerH.GetUsersFromFactionMembers(factionFile);
                    foreach (UserFile userFile in toUpdateOffline) userFile.UpdateFaction(null);

                    SiteFile[] factionSites = GuildManagerH.GetFactionSites(factionFile);

                    foreach (SiteFile site in factionSites)
                    {
                        SiteManagerHelper.UpdateFaction(site, null);
                    }

                    foreach (ServerClient toUpdateConnected in GuildManagerH.GetConnectedFactionMembers(factionFile))
                    {
                        toUpdateConnected.UserFile.UpdateFaction(null);
                        toUpdateConnected.Listener.EnqueuePacket(PacketHeader.GuildManager, factionManifest);
                        GoodwillManager.UpdateClientGoodwills(toUpdateConnected);
                    }
                    File.Delete(Path.Combine(Master.FactionsPath, factionFile.Name + GuildManagerH.fileExtension));

                    InformationDisplayer.DisplayRemoveFaction(factionFile.Name);
                }
            }
        }

        private static void AddMemberToFaction(ServerClient client, PlayerGuildData factionManifest)
        {
            GuildFile factionFile = GuildManagerH.GetFactionFromFactionName(client.UserFile.GuildName);
            SettlementFile settlementFile = SettlementManager.GetSettlementFileFromTile(factionManifest._dataInt);
            ServerClient toAdd = ServerNetwork.Instance.GetConnectedClientFromUsername(settlementFile.Username);

            if (factionFile == null) return;
            if (toAdd == null) return;

            if (GuildManagerH.GetMemberRank(factionFile, client.UserFile.Username) == GuildRanks.Member) ResponseShortcutManager.SendNoPowerPacket(client, factionManifest);
            else
            {
                if (!string.IsNullOrEmpty(toAdd.UserFile.GuildName)) return;
                else
                {
                    if (GuildManagerH.CheckIfUserIsInFaction(factionFile, toAdd.UserFile.Username)) return;
                    else
                    {
                        factionManifest._file.Name = factionFile.Name;
                        toAdd.Listener.EnqueuePacket(PacketHeader.GuildManager, factionManifest);
                    }
                }
            }
        }

        private static void ConfirmAddMemberToFaction(ServerClient client, PlayerGuildData factionManifest)
        {
            GuildFile factionFile = GuildManagerH.GetFactionFromFactionName(factionManifest._file.Name);

            if (factionFile == null) return;
            else
            {    
                if (!GuildManagerH.CheckIfUserIsInFaction(factionFile, client.UserFile.Username))
                {
                    GuildMember member = new GuildMember();
                    member.Username = client.UserFile.Username;
                    member.Rank = GuildRanks.Member;

                    factionFile.GuildMembers.Add(member);

                    GuildManagerH.SaveFactionFile(factionFile);

                    GoodwillManager.ClearAllFactionMemberGoodwills(factionFile);

                    foreach (SiteFile site in SiteManagerHelper.GetAllSitesFromUsername(client.UserFile.Username))
                    {
                        SiteManagerHelper.UpdateFaction(site, factionFile);
                    }

                    ServerClient[] connectedMembers = GuildManagerH.GetConnectedFactionMembers(factionFile);
                    foreach (ServerClient sc in connectedMembers) GoodwillManager.UpdateClientGoodwills(sc);
                }
            }
        }

        private static void RemoveMemberFromFaction(ServerClient client, PlayerGuildData factionManifest)
        {
            GuildFile factionFile = GuildManagerH.GetFactionFromFactionName(client.UserFile.GuildName);
            SettlementFile settlementFile = SettlementManager.GetSettlementFileFromTile(factionManifest._dataInt);
            UserFile toUpdateOffline = UserManagerH.GetUserFileFromName(settlementFile.Username);
            ServerClient toRemoveConnected = ServerNetwork.Instance.GetConnectedClientFromUsername(settlementFile.Username);

            if (GuildManagerH.GetMemberRank(factionFile, client.UserFile.Username) == GuildRanks.Member)
            {
                if (settlementFile.Username == client.UserFile.Username) RemoveFromFaction();
                else ResponseShortcutManager.SendNoPowerPacket(client, factionManifest);
            }

            else if (GuildManagerH.GetMemberRank(factionFile, client.UserFile.Username) == GuildRanks.Moderator)
            {
                if (settlementFile.Username == client.UserFile.Username) RemoveFromFaction();
                else
                {
                    if (GuildManagerH.GetMemberRank(factionFile, settlementFile.Username) != GuildRanks.Member)
                    {
                        ResponseShortcutManager.SendNoPowerPacket(client, factionManifest);
                    }
                    else RemoveFromFaction();
                }
            }

            else if (GuildManagerH.GetMemberRank(factionFile, client.UserFile.Username) == GuildRanks.Admin)
            {
                if (settlementFile.Username == client.UserFile.Username)
                {
                    factionManifest._stepMode = GuildStepMode.AdminProtection;
                    client.Listener.EnqueuePacket(PacketHeader.GuildManager, factionManifest);
                }
                else RemoveFromFaction();
            }

            void RemoveFromFaction()
            {           
                if (!GuildManagerH.CheckIfUserIsInFaction(factionFile, client.UserFile.Username)) return;
                else
                {
                    if (toRemoveConnected != null)
                    {
                        toRemoveConnected.UserFile.UpdateFaction(null);
                        foreach (SiteFile site in SiteManagerHelper.GetAllSitesFromUsername(toRemoveConnected.UserFile.Username))
                        {
                            SiteManagerHelper.UpdateFaction(site, null);
                        }

                        toRemoveConnected.Listener.EnqueuePacket(PacketHeader.GuildManager, factionManifest);
                        GoodwillManager.UpdateClientGoodwills(toRemoveConnected);
                    }

                    if (toUpdateOffline == null) return;
                    else
                    {
                        toUpdateOffline.UpdateFaction(null);

                        foreach (SiteFile site in SiteManagerHelper.GetAllSitesFromUsername(toUpdateOffline.Username))
                        {
                            SiteManagerHelper.UpdateFaction(site, null);
                        }

                        GuildMember[] guildMembers = GuildManagerH.GetAllFactionMembers(factionFile);

                        for (int i = 0; i < guildMembers.Count(); i++)
                        {
                            if (guildMembers[i].Username == toUpdateOffline.Username)
                            {
                                factionFile.GuildMembers.Remove(guildMembers[i]);
                                GuildManagerH.SaveFactionFile(factionFile);
                                break;
                            }
                        }
                    }
                    ServerClient[] members = GuildManagerH.GetConnectedFactionMembers(factionFile);
                    foreach (ServerClient member in members) GoodwillManager.UpdateClientGoodwills(member);
                }
            }
        }

        private static void PromoteMember(ServerClient client, PlayerGuildData factionManifest)
        {
            SettlementFile settlementFile = SettlementManager.GetSettlementFileFromTile(factionManifest._dataInt);
            UserFile userFile = UserManagerH.GetUserFileFromName(settlementFile.Username);
            GuildFile factionFile = GuildManagerH.GetFactionFromFactionName(client.UserFile.GuildName);

            if (GuildManagerH.GetMemberRank(factionFile, client.UserFile.Username) == GuildRanks.Member)
            {
                ResponseShortcutManager.SendNoPowerPacket(client, factionManifest);
            }

            else if (GuildManagerH.GetMemberRank(factionFile, settlementFile.Username) != GuildRanks.Member && GuildManagerH.GetMemberRank(factionFile, client.UserFile.Username) != GuildRanks.Admin)
            {
                ResponseShortcutManager.SendNoPowerPacket(client, factionManifest);
            }

            else
            {
                if (!GuildManagerH.CheckIfUserIsInFaction(factionFile, client.UserFile.Username)) return;
                else
                {
                    GuildMember[] guildMembers = GuildManagerH.GetAllFactionMembers(factionFile);

                    for (int i = 0; i < guildMembers.Count(); i++)
                    {
                        if (guildMembers[i].Username == userFile.Username)
                        {
                            guildMembers[i].Rank = GuildRanks.Moderator;
                            GuildManagerH.SaveFactionFile(factionFile);
                            break;
                        }
                    }
                }
            }
        }

        private static void DemoteMember(ServerClient client, PlayerGuildData factionManifest)
        {
            SettlementFile settlementFile = SettlementManager.GetSettlementFileFromTile(factionManifest._dataInt);
            UserFile userFile = UserManagerH.GetUserFileFromName(settlementFile.Username);
            GuildFile factionFile = GuildManagerH.GetFactionFromFactionName(client.UserFile.GuildName);

            if (GuildManagerH.GetMemberRank(factionFile, client.UserFile.Username) != GuildRanks.Admin)
            {
                ResponseShortcutManager.SendNoPowerPacket(client, factionManifest);
            }

            else
            {
                if (!GuildManagerH.CheckIfUserIsInFaction(factionFile, client.UserFile.Username)) return;
                else
                {
                    GuildMember[] guildMembers = GuildManagerH.GetAllFactionMembers(factionFile);

                    for (int i = 0; i < guildMembers.Count(); i++)
                    {
                        if (guildMembers[i].Username == userFile.Username)
                        {
                            guildMembers[i].Rank = GuildRanks.Member;
                            GuildManagerH.SaveFactionFile(factionFile);
                            break;
                        }
                    }
                }
            }
        }

        private static void SendFactionMemberList(ServerClient client, PlayerGuildData factionManifest)
        {
            factionManifest._file = GuildManagerH.GetFactionFromFactionName(client.UserFile.GuildName);
            client.Listener.EnqueuePacket(PacketHeader.GuildManager, factionManifest);
        }
    }

    public static class GuildManagerH
    {
        //Variables

        public readonly static string fileExtension = ".mpfaction";

        public static void SaveFactionFile(GuildFile factionFile)
        {
            factionFile.SavingSemaphore.WaitOne();

            try
            {
                string savePath = Path.Combine(Master.FactionsPath, factionFile.Name + fileExtension);
                Serializer.SerializeToFile(savePath, factionFile);

                GuildMember[] guildMembers = GetAllFactionMembers(factionFile);

                foreach (GuildMember member in guildMembers)
                {
                    ServerClient toUpdateConnected = ServerNetwork.Instance.GetConnectedClientFromUsername(member.Username);
                    toUpdateConnected?.UserFile.UpdateFaction(factionFile);

                    UserFile toUpdateOffline = UserManagerH.GetUserFileFromName(member.Username);
                    toUpdateOffline?.UpdateFaction(factionFile);
                }

                SiteFile[] factionSites = GetFactionSites(factionFile);
                foreach (SiteFile site in factionSites) SiteManagerHelper.UpdateFaction(site, factionFile);
            }
            catch (Exception e) { Printer.Error(e.ToString()); }

            factionFile.SavingSemaphore.Release();
        }

        public static bool CheckIfFactionExistsByName(string nameToCheck)
        {
            GuildFile factionFile = GetAllFactions().FirstOrDefault(fetch => fetch.Name == nameToCheck);
            if (factionFile != null) return true;
            else return false;
        }

        public static GuildFile[] GetAllFactions()
        {
            List<GuildFile> factionFiles = new List<GuildFile>();

            string[] factions = Directory.GetFiles(Master.FactionsPath);
            foreach (string faction in factions)
            {
                if (!faction.EndsWith(fileExtension)) continue;
                factionFiles.Add(Serializer.SerializeFromFile<GuildFile>(faction));
            }

            return factionFiles.ToArray();
        }

        public static GuildFile GetFactionFromFactionName(string factionName)
        {
            string[] factions = Directory.GetFiles(Master.FactionsPath);
            foreach (string faction in factions)
            {
                GuildFile factionFile = Serializer.SerializeFromFile<GuildFile>(faction);
                if (factionFile.Name == factionName) return factionFile;
            }

            return new GuildFile();
        }

        public static GuildMember[] GetAllFactionMembers(GuildFile file)
        {
            return file.GuildMembers.ToArray();
        }

        public static bool CheckIfUserIsInFaction(GuildFile factionFile, string usernameToCheck)
        {
            if (GetAllFactionMembers(factionFile).FirstOrDefault(fetch => fetch.Username == usernameToCheck) != null) return true;
            else return false;
        }

        public static GuildRanks GetMemberRank(GuildFile factionFile, string usernameToCheck)
        {
            GuildMember[] members = GetAllFactionMembers(factionFile);

            for (int i = 0; i < members.Length; i++)
            {
                if (members[i].Username == usernameToCheck)
                {
                    return (GuildRanks)members[i].Rank;
                }
            }

            return GuildRanks.Member;
        }

        public static SiteFile[] GetFactionSites(GuildFile factionFile)
        {
            return SiteManagerHelper.GetAllSites().Where(fetch => fetch.GuildName == factionFile.Name).ToArray();
        }

        public static ServerClient[] GetConnectedFactionMembers(GuildFile factionFile)
        {
            return ServerNetwork.Instance.GetConnectedClientsSafe().Where(fetch => fetch.UserFile.GuildName == factionFile.Name).ToArray();
        }

        public static UserFile[] GetUsersFromFactionMembers(GuildFile factionFile)
        {
            return UserManagerH.GetAllUserFiles().Where(fetch => fetch.GuildName == factionFile.Name).ToArray();
        }
    }
}
