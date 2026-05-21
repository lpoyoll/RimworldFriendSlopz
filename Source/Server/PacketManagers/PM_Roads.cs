using GameServer.Core;
using GameServer.Hooks.TCPNetwork;
using GameServer.Managers;
using GameServer.Misc;
using Shared;
using Shared.Details.Planet;
using Shared.Files.Configs;
using TCPNetwork.Files.Client;
using TCPNetwork.PacketManagers;
using TCPNetwork.Packets;
using static TCPNetwork.Packets.PKT_Road;

namespace GameServer.PacketManager
{
    public class PM_Roads : PM_Base
    {
        [HandlesPacket(PacketHeader.RoadManager)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            if (!PlayerCooldown.CheckIfCanRoad(client.GetData<UserFile>(), Master.ActionConfigs.RoadAction)) ResponseShortcutManager.SendUnavailablePacket(client);
            else
            {
                PKT_Road data = Serializer.ConvertBytesToObject<PKT_Road>(bytes);
                switch (data._stepMode)
                {
                    case RoadStepMode.Add:
                        AddRoad(client, data);
                        break;

                    case RoadStepMode.Remove:
                        RemoveRoad(client, data);
                        break;
                }
            }
        }

        private static void AddRoad(ServerClient client, PKT_Road data)
        {
            if (RoadManagerHelper.CheckIfRoadExists(data._details))
            {
                ResponseShortcutManager.SendIllegalPacket(client, "Tried to add a road that already existed");
                return;
            }

            SaveRoad(data._details, client);
            ServerNetwork.SendPacketToAllClients(PacketHeader.RoadManager, data);
            client.GetData<UserFile>().Cooldowns.SetRoadTimer(client.GetData<UserFile>());
        }

        private static void RemoveRoad(ServerClient client, PKT_Road data)
        {
            if (!RoadManagerHelper.CheckIfRoadExists(data._details))
            {
                ResponseShortcutManager.SendIllegalPacket(client, "Tried to remove a road that didn't exist");
                return;
            }

            foreach (RoadDetail existingRoad in Master.WorldValues.Roads)
            {
                if (existingRoad.FromTile == data._details.FromTile && existingRoad.ToTile == data._details.ToTile)
                {
                    DeleteRoad(existingRoad, client);
                    PostDelete(existingRoad);
                    return;
                }

                else if (existingRoad.FromTile == data._details.ToTile && existingRoad.ToTile == data._details.FromTile)
                {
                    DeleteRoad(existingRoad, client);
                    PostDelete(existingRoad);
                    return;
                }

                else continue;
            }

            void PostDelete(RoadDetail toRemove)
            {
                ServerNetwork.SendPacketToAllClients(PacketHeader.RoadManager, data);
                client.GetData<UserFile>().Cooldowns.SetRoadTimer(client.GetData<UserFile>());
            }
        }

        private static void SaveRoad(RoadDetail details, ServerClient client = null)
        {
            List<RoadDetail> currentRoads = Master.WorldValues.Roads.ToList();
            currentRoads.Add(details);

            Master.WorldValues.Roads = currentRoads;
            FL_PlanetConfig.Save(FL_PlanetConfig.SavePath, Master.WorldValues);

            InformationDisplayer.DisplayAddRoad(details.FromTile.ToString(), details.ToTile.ToString());
        }

        private static void DeleteRoad(RoadDetail details, ServerClient client = null)
        {
            List<RoadDetail> currentRoads = Master.WorldValues.Roads.ToList();
            currentRoads.Remove(details);

            Master.WorldValues.Roads = currentRoads;
            FL_PlanetConfig.Save(FL_PlanetConfig.SavePath, Master.WorldValues);

            InformationDisplayer.DisplayRemoveRoad(details.FromTile.ToString(), details.ToTile.ToString());
        }
    }

    public class RoadManagerHelper
    {
        public static bool CheckIfRoadExists(RoadDetail details)
        {
            foreach (RoadDetail existingRoad in Master.WorldValues.Roads)
            {
                if (existingRoad.FromTile == details.FromTile && existingRoad.ToTile == details.ToTile) return true;
                else if (existingRoad.FromTile == details.ToTile && existingRoad.ToTile == details.FromTile) return true;
            }

            return false;
        }
    }
}
