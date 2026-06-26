using GameServer.Core;
using GameServer.Hooks.TCPNetwork;
using GameServer.Managers;
using GameServer.Misc;
using RTShared.Files;
using RTShared.Misc;
using RTNetwork.PacketManagers;
using RTNetwork.Packets;
using static RTNetwork.Packets.PKT_Event;
using RTShared.Files.Player;
using RTNetwork.Components;

namespace GameServer.PacketManager
{
    public class PM_Events : PM_Base
    {
        public static List<FL_Event> LoadedEvents { get; private set; } = null;

        [HandlesPacket(PacketHeader.Event)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_Event data = Serializer.ConvertBytesToObject<PKT_Event>(bytes);

            if (!FL_PlayerCooldown.CheckIfCanEvent(client.GetData<FL_Player>(), Master.ActionConfigs.EventAction))
            {
                data._stepMode = EventStepMode.Recover;
                client.Listener.EnqueuePacket(PacketHeader.Event, data);
            }

            else
            {
                switch (data._stepMode)
                {
                    case EventStepMode.Send:
                        SendEvent(client, data);
                        break;

                    case EventStepMode.Set:
                        SetEvents(client, data);
                        break;
                }

                client.GetData<FL_Player>().Cooldowns.SetEventTimer(client.GetData<FL_Player>());
            }
        }

        public static void SendEvent(ServerClient client, PKT_Event data)
        {
            if (!PM_Settlements.CheckIfTileIsInUse(data._toTile)) ResponseShortcutManager.SendIllegalPacket(client, $"Player {client.GetData<FL_Player>().Username} attempted to send an event to settlement at tile {data._toTile}, but it has no settlement");
            else
            {
                FL_Settlement settlement = PM_Settlements.GetSettlementFileFromTile(data._toTile);
                if (!UserManagerH.CheckIfUserIsConnected(settlement.Username))
                {
                    data._stepMode = EventStepMode.Recover;
                    client.Listener.EnqueuePacket(PacketHeader.Event, data);
                }

                else
                {
                    ServerClient target = ServerNetwork.GetConnectedClientFromUsername(settlement.Username);

                    //Back to player

                    client.Listener.EnqueuePacket(PacketHeader.Event, data);

                    //To the person that should receive it

                    data._stepMode = EventStepMode.Receive;

                    target.Listener.EnqueuePacket(PacketHeader.Event, data);
                }
            }
        }

        private static void SetEvents(ServerClient client, PKT_Event data)
        {
            if (!client.GetData<FL_Player>().IsAdmin) ResponseShortcutManager.SendIllegalPacket(client, "Tried to modify events without being admin!");
            else
            {
                foreach (FL_Event file in data._eventFiles)
                {
                    Serializer.SerializeToFile(Path.Combine(Master.EventsPath, file.DefName + CommonValues.DefaultSaveFormat), file);
                }

                LoadAllEvents();
                InformationDisplayer.DisplaySetEvents(client);
                ServerNetwork.SendPacketToAllClients(PacketHeader.Event, data);
            }
        }

        public static void LoadAllEvents()
        {
            List<FL_Event> toLoad = new List<FL_Event>();
            foreach (string str in Directory.GetFiles(Master.EventsPath))
            {
                FL_Event file = Serializer.SerializeFromFile<FL_Event>(str);
                toLoad.Add(file);

                Printer.Warning($"Loaded event '{file.Name}'", Printer.Verbosity.Extreme);
            }

            LoadedEvents = toLoad.OrderBy(fetch => fetch.Name).ToList();
        }
    }
}
