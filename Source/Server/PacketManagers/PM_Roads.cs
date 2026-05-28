using GameServer.Core;
using GameServer.Hooks.TCPNetwork;
using GameServer.Managers;
using GameServer.Misc;
using RTShared;
using RTShared.Details.Planet;
using RTShared.Files.Configs;
using RTNetwork;
using RTShared.Files.ServerClient;
using RTNetwork.PacketManagers;
using RTNetwork.Packets;
using static RTNetwork.Packets.PKT_Road;
using RTShared.Files.Player;

namespace GameServer.PacketManager
{
    public class PM_Roads : PM_Base
    {
        [HandlesPacket(PacketHeader.Road)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            if (!FL_PlayerCooldown.CheckIfCanRoad(client.GetData<FL_Player>(), Master.ActionConfigs.RoadAction)) ResponseShortcutManager.SendUnavailablePacket(client);
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
            ServerNetwork.SendPacketToAllClients(PacketHeader.Road, data);
            client.GetData<FL_Player>().Cooldowns.SetRoadTimer(client.GetData<FL_Player>());
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
                ServerNetwork.SendPacketToAllClients(PacketHeader.Road, data);
                client.GetData<FL_Player>().Cooldowns.SetRoadTimer(client.GetData<FL_Player>());
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
