using RTNetwork.Components;
using RTNetwork.PacketManagers;
using RTNetwork.Packets;
using RTServer.Core;
using RTServer.Hooks.TCPNetwork;
using RTServer.Managers;
using RTServer.Misc;
using RTShared.Details.Planet;
using RTShared.Files.Configs;
using RTShared.Files.Player;
using RTShared.Misc;
using static RTNetwork.Packets.PKT_Road;

namespace RTServer.PacketManagers
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

                switch (data.StepMode)
                {
                    case RoadStepMode.Add:
                        AddRoad(client, data);
                        break;

                    case RoadStepMode.Remove:
                        RemoveRoad(client, data);
                        break;
                }

                client.GetData<FL_Player>().Cooldowns.SetRoadTimer(client.GetData<FL_Player>());
            }
        }

        private static void AddRoad(ServerClient client, PKT_Road packet)
        {
            RoadDetail toAdd = Master.WorldValues.Roads.FirstOrDefault(fetch => fetch.Tile == packet.Roads.Tile);
            
            if (toAdd != null)
            {
                ResponseShortcutManager.SendIllegalPacket(client, $"Player {client.GetData<FL_Player>().Username} attempted to build already existing road");
            }
            
            else
            {
                SaveRoad(packet.Roads);
                ServerNetwork.SendPacketToAllClients(PacketHeader.Road, packet);
            }
        }

        private static void RemoveRoad(ServerClient client, PKT_Road packet)
        {
            RoadDetail toRemove = Master.WorldValues.Roads.FirstOrDefault(fetch => fetch.Tile == packet.Roads.Tile);
            
            if (toRemove == null)
            {
                ResponseShortcutManager.SendIllegalPacket(client, $"Player {client.GetData<FL_Player>().Username} attempted to destroy non-existing road");
            }
            
            else
            {
                DeleteRoad(toRemove);
                ServerNetwork.SendPacketToAllClients(PacketHeader.Road, packet);
            }
        }

        private static void SaveRoad(RoadDetail details)
        {
            Master.WorldValues.Roads.Add(details);
            FL_PlanetConfig.Save(FL_PlanetConfig.SavePath, Master.WorldValues);

            InformationDisplayer.DisplayAddRoad(details.Tile.ToString());
        }

        private static void DeleteRoad(RoadDetail details)
        {
            Master.WorldValues.Roads.Remove(details);
            FL_PlanetConfig.Save(FL_PlanetConfig.SavePath, Master.WorldValues);

            InformationDisplayer.DisplayRemoveRoad(details.Tile.ToString());
        }
    }
}
