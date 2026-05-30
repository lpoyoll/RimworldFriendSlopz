using GameServer.Core;
using GameServer.Hooks.TCPNetwork;
using GameServer.Managers;
using GameServer.Misc;
using RTShared;
using RTShared.Files;
using RTShared.Files.Guilds;
using RTNetwork;
using RTShared.Files.ServerClient;
using RTNetwork.PacketManagers;
using RTNetwork.Packets;
using static RTShared.Files.Guilds.GuildMember;
using static RTNetwork.Packets.PKT_PlayerGuild;
using RTNetwork.Components;

namespace GameServer.PacketManager
{

    public class PM_Guilds : PM_Base
    {
        [HandlesPacket(PacketHeader.Guild)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            if (!Master.ActionConfigs.EnableFactions)
            {
                ResponseShortcutManager.SendIllegalPacket(client, "Tried to use disabled feature!");
                return;
            }

            PKT_PlayerGuild data = Serializer.ConvertBytesToObject<PKT_PlayerGuild>(bytes);

            switch (data._stepMode)
            {
                case GuildStepMode.Create:
                    CreateFaction(client, data);
                    break;

                case GuildStepMode.Delete:
                    DeleteFaction(client, data);
                    break;

                case GuildStepMode.Invite:
                    InviteMemberToFaction(client, data);
                    break;

                case GuildStepMode.AddMember:
                    AddMemberToFaction(client, data);
                    break;

                case GuildStepMode.RemoveMember:
                    RemoveMemberFromFaction(client, data);
                    break;

                case GuildStepMode.Promote:
                    PromoteMember(client, data);
                    break;

                case GuildStepMode.Demote:
                    DemoteMember(client, data);
                    break;

                case GuildStepMode.MemberList:
                    SendMemberList(client, data);
                    break;
            }
        }

        private static void CreateFaction(ServerClient client, PKT_PlayerGuild factionManifest)
        {
            if (GuildManagerH.CheckIfFactionExistsByName(factionManifest._guild.Name))
            {
                factionManifest._stepMode = GuildStepMode.NameInUse;
                client.Listener.EnqueuePacket(PacketHeader.Guild, factionManifest);
            }

            else
            {
                factionManifest._stepMode = GuildStepMode.Create;

                GuildMember member = new GuildMember();
                member.Username = client.GetData<FL_Player>().Username;
                member.Rank = GuildMember.GuildRanks.Admin;

                FL_Guild factionFile = new FL_Guild();
                factionFile.Name = factionManifest._guild.Name;
                factionFile.AddMember(member);

                client.GetData<FL_Player>().UpdateFaction(factionFile);

                foreach (FL_Site site in PM_Sites.GetAllSitesFromUsername(client.GetData<FL_Player>().Username)) site.UpdateFaction(factionFile);

                client.Listener.EnqueuePacket(PacketHeader.Guild, factionManifest);

                InformationDisplayer.DisplayAddFaction(factionFile.Name);
            }
        }

        private static void DeleteFaction(ServerClient client, PKT_PlayerGuild factionManifest)
        {
            FL_Guild guild = GuildManagerH.GetFactionFromName(client.GetData<FL_Player>().GuildName);

            if (GuildManagerH.GetMemberRank(guild, client.GetData<FL_Player>().Username) != GuildRanks.Admin) ResponseShortcutManager.SendNoPowerPacket(client);
            else
            {
                foreach (FL_Player userFile in GuildManagerH.GetUsersFromFactionMembers(guild)) userFile.UpdateFaction(null);

                foreach (FL_Site site in GuildManagerH.GetFactionSites(guild)) site.UpdateFaction(null);

                factionManifest._stepMode = GuildStepMode.Delete;
                foreach (ServerClient toUpdateConnected in GuildManagerH.GetConnectedFactionMembers(guild))
                {
                    toUpdateConnected.GetData<FL_Player>().UpdateFaction(null);
                    toUpdateConnected.Listener.EnqueuePacket(PacketHeader.Guild, factionManifest);
                    PM_Goodwills.UpdateClientGoodwills(toUpdateConnected);
                }

                guild.Delete();

                InformationDisplayer.DisplayRemoveFaction(guild.Name);
            }
        }

        private static void InviteMemberToFaction(ServerClient client, PKT_PlayerGuild guildManifest)
        {
            FL_Guild guild = GuildManagerH.GetFactionFromName(client.GetData<FL_Player>().GuildName);
            FL_Settlement settlement = PM_Settlements.GetSettlementFileFromTile(guildManifest._dataInt);
            ServerClient toAdd = ServerNetwork.GetConnectedClientFromUsername(settlement.Username);

            if (GuildManagerH.GetMemberRank(guild, client.GetData<FL_Player>().Username) == GuildRanks.Member) ResponseShortcutManager.SendNoPowerPacket(client);
            else
            {
                if (toAdd.GetData<FL_Player>().GuildName != null) return;
                else
                {
                    guildManifest._guild.Name = guild.Name;
                    toAdd.Listener.EnqueuePacket(PacketHeader.Guild, guildManifest);
                }
            }
        }

        private static void AddMemberToFaction(ServerClient client, PKT_PlayerGuild factionManifest)
        {
            FL_Guild guild = GuildManagerH.GetFactionFromName(factionManifest._guild.Name);

            if (!GuildManagerH.CheckIfUserIsInFaction(guild, client.GetData<FL_Player>().Username))
            {
                GuildMember member = new GuildMember();
                member.Username = client.GetData<FL_Player>().Username;
                member.Rank = GuildRanks.Member;
                guild.AddMember(member);

                client.GetData<FL_Player>().UpdateFaction(guild);

                foreach (FL_Site site in PM_Sites.GetAllSitesFromUsername(client.GetData<FL_Player>().Username)) site.UpdateFaction(guild);

                foreach (ServerClient sc in GuildManagerH.GetConnectedFactionMembers(guild)) PM_Goodwills.UpdateClientGoodwills(sc);
            }
        }

        private static void RemoveMemberFromFaction(ServerClient client, PKT_PlayerGuild guildManifest)
        {
            FL_Guild guild = GuildManagerH.GetFactionFromName(client.GetData<FL_Player>().GuildName);
            FL_Settlement settlement = PM_Settlements.GetSettlementFileFromTile(guildManifest._dataInt);
            FL_Player toRemoveOffline = UserManagerH.GetUserFileFromName(settlement.Username);
            ServerClient toRemoveOnline = ServerNetwork.GetConnectedClientFromUsername(settlement.Username);

            GuildRanks userRank = GuildManagerH.GetMemberRank(guild, client.GetData<FL_Player>().Username);

            if (settlement.Username == client.GetData<FL_Player>().Username)
            {
                if (userRank != GuildRanks.Admin) Remove();
                else
                {
                    guildManifest._stepMode = GuildStepMode.AdminProtection;
                    client.Listener.EnqueuePacket(PacketHeader.Guild, guildManifest);
                }
            }

            else
            {
                if (userRank == GuildRanks.Member || userRank == GuildRanks.Moderator) ResponseShortcutManager.SendNoPowerPacket(client);
                else Remove();
            }

            void Remove()
            {
                if (toRemoveOnline != null)
                {
                    toRemoveOnline.GetData<FL_Player>().UpdateFaction(null);

                    PM_Goodwills.UpdateClientGoodwills(toRemoveOnline);

                    toRemoveOnline.Listener.EnqueuePacket(PacketHeader.Guild, guildManifest);
                }

                toRemoveOffline.UpdateFaction(null);

                guild.RemoveMember(guild.GuildMembers.First(fetch => fetch.Username == toRemoveOffline.Username));

                foreach (FL_Site site in PM_Sites.GetAllSitesFromUsername(toRemoveOffline.Username)) site.UpdateFaction(null);

                foreach (ServerClient member in GuildManagerH.GetConnectedFactionMembers(guild)) PM_Goodwills.UpdateClientGoodwills(member);
            }
        }

        private static void PromoteMember(ServerClient client, PKT_PlayerGuild factionManifest)
        {
            FL_Settlement settlement = PM_Settlements.GetSettlementFileFromTile(factionManifest._dataInt);
            FL_Guild guild = GuildManagerH.GetFactionFromName(client.GetData<FL_Player>().GuildName);

            GuildRanks rank = GuildManagerH.GetMemberRank(guild, client.GetData<FL_Player>().Username);
            if (rank == GuildRanks.Member) ResponseShortcutManager.SendNoPowerPacket(client);
            else
            {
                FL_Player toPromoteOffline = UserManagerH.GetUserFileFromName(settlement.Username);
                if (GuildManagerH.GetMemberRank(guild, toPromoteOffline.Username) != GuildRanks.Member) ResponseShortcutManager.SendNoPowerPacket(client);
                else
                {
                    GuildMember member = GuildManagerH.GetAllFactionMembers(guild).First(fetch => fetch.Username == toPromoteOffline.Username);
                    guild.PromoteMember(member);

                    ServerClient toPromoteOnline = ServerNetwork.GetConnectedClientFromUsername(toPromoteOffline.Username);
                    if (toPromoteOnline != null) toPromoteOnline.Listener.EnqueuePacket(PacketHeader.Guild, factionManifest);
                }
            }
        }

        private static void DemoteMember(ServerClient client, PKT_PlayerGuild factionManifest)
        {
            FL_Settlement settlement = PM_Settlements.GetSettlementFileFromTile(factionManifest._dataInt);
            FL_Guild guild = GuildManagerH.GetFactionFromName(client.GetData<FL_Player>().GuildName);

            GuildRanks rank = GuildManagerH.GetMemberRank(guild, client.GetData<FL_Player>().Username);
            if (rank != GuildRanks.Admin) ResponseShortcutManager.SendNoPowerPacket(client);
            else
            {
                FL_Player toDemoteOffline = UserManagerH.GetUserFileFromName(settlement.Username);
                if (GuildManagerH.GetMemberRank(guild, toDemoteOffline.Username) != GuildRanks.Moderator) ResponseShortcutManager.SendNoPowerPacket(client);
                else
                {
                    GuildMember member = GuildManagerH.GetAllFactionMembers(guild).First(fetch => fetch.Username == toDemoteOffline.Username);
                    guild.DemoteMember(member);

                    ServerClient toDemoteOnline = ServerNetwork.GetConnectedClientFromUsername(toDemoteOffline.Username);
                    if (toDemoteOnline != null) toDemoteOnline.Listener.EnqueuePacket(PacketHeader.Guild, factionManifest);
                }
            }
        }

        private static void SendMemberList(ServerClient client, PKT_PlayerGuild factionManifest)
        {
            factionManifest._guild = GuildManagerH.GetFactionFromName(client.GetData<FL_Player>().GuildName);
            client.Listener.EnqueuePacket(PacketHeader.Guild, factionManifest);
        }
    }

    public class GuildManagerH
    {
        public static FL_Guild[] GetAllFactions()
        {
            List<FL_Guild> factionFiles = new List<FL_Guild>();

            foreach (string faction in Directory.GetFiles(Master.GuildsPath)) factionFiles.Add(Serializer.SerializeFromFile<FL_Guild>(faction));

            return factionFiles.ToArray();
        }

        public static FL_Guild GetFactionFromName(string name) { return GetAllFactions().FirstOrDefault(fetch => fetch.Name == name); }

        public static GuildMember[] GetAllFactionMembers(FL_Guild file) { return file.GuildMembers.ToArray(); }

        public static bool CheckIfUserIsInFaction(FL_Guild factionFile, string usernameToCheck)
        {
            return GetAllFactionMembers(factionFile).FirstOrDefault(fetch => fetch.Username == usernameToCheck) != null;
        }

        public static GuildRanks GetMemberRank(FL_Guild factionFile, string usernameToCheck)
        {
            return GetAllFactionMembers(factionFile).First(fetch => fetch.Username == usernameToCheck).Rank;
        }

        public static FL_Site[] GetFactionSites(FL_Guild factionFile)
        {
            return PM_Sites.GetAllSites().Where(fetch => fetch.GuildName == factionFile.Name).ToArray();
        }

        public static ServerClient[] GetConnectedFactionMembers(FL_Guild factionFile)
        {
            return ServerNetwork.GetConnectedClients().Where(fetch => fetch.GetData<FL_Player>().GuildName == factionFile.Name).ToArray();
        }

        public static FL_Player[] GetUsersFromFactionMembers(FL_Guild factionFile)
        {
            return UserManagerH.GetAllUserFiles().Where(fetch => fetch.GuildName == factionFile.Name).ToArray();
        }

        public static bool CheckIfFactionExistsByName(string nameToCheck)
        {
            return GetAllFactions().FirstOrDefault(fetch => fetch.Name == nameToCheck) != null;
        }
    }
}
