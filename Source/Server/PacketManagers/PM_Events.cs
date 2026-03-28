using GameServer.Core;
using GameServer.Hooks.TCPNetwork;
using GameServer.Managers;
using GameServer.Misc;
using Shared;
using Shared.Files;
using Shared.Misc;
using TCPNetwork;
using TCPNetwork.Files.Client;
using TCPNetwork.Packets;
using static TCPNetwork.Packets.PKT_Event;

namespace GameServer.PacketManager
{
    public class PM_Events : PM_Base
    {
        [HandlesPacket(PacketHeader.EventManager)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            if (!Master.ActionConfigs.EventAction.IsEnabled)
            {
                ResponseShortcutManager.SendIllegalPacket(client, "Tried to use disabled feature!");
                return;
            }

            PKT_Event data = Serializer.ConvertBytesToObject<PKT_Event>(bytes);

            switch (data._stepMode)
            {
                case EventStepMode.Send:
                    SendEvent(client, data);
                    break;

                case EventStepMode.Set:
                    SetEvents(client, data);
                    break;
            }
        }

        public static void SendEvent(ServerClient client, PKT_Event data)
        {
            if (!PM_Settlements.CheckIfTileIsInUse(data._toTile)) ResponseShortcutManager.SendIllegalPacket(client, $"Player {client.UserFile.Username} attempted to send an event to settlement at tile {data._toTile}, but it has no settlement");
            else
            {
                SettlementFile settlement = PM_Settlements.GetSettlementFileFromTile(data._toTile);
                if (!UserManagerH.CheckIfUserIsConnected(settlement.Username))
                {
                    data._stepMode = EventStepMode.Recover;
                    client.Listener.EnqueuePacket(PacketHeader.EventManager, data);
                }

                else
                {
                    ServerClient target = ServerNetwork.GetConnectedClientFromUsername(settlement.Username);

                    if (!PlayerCooldown.CheckIfCanEvent(target.UserFile, Master.ActionConfigs.EventAction.IsEnabled, Master.ActionConfigs.EventAction.Cooldown))
                    {
                        data._stepMode = EventStepMode.Recover;
                        client.Listener.EnqueuePacket(PacketHeader.EventManager, data);
                    }

                    else
                    {
                        //Back to player

                        client.Listener.EnqueuePacket(PacketHeader.EventManager, data);

                        //To the person that should receive it

                        data._stepMode = EventStepMode.Receive;

                        target.UserFile.Cooldowns.SetEventTimer(TimeConverter.GetCurrentTimeToEpoch(), target.UserFile);

                        target.Listener.EnqueuePacket(PacketHeader.EventManager, data);
                    }
                }
            }
        }

        private static void SetEvents(ServerClient client, PKT_Event data)
        {
            if (!client.UserFile.IsAdmin) ResponseShortcutManager.SendIllegalPacket(client, "Tried to modify events without being admin!");
            else
            {
                foreach (EventFile file in data._eventFiles)
                {
                    Serializer.SerializeToFile(Path.Combine(Master.EventsPath, file.DefName + CommonValues.DefaultSaveFormat), file);
                }

                EventManagerH.LoadAllEvents();
                InformationDisplayer.DisplaySetEvents(client);
                ServerNetwork.SendPacketToAllClients(PacketHeader.EventManager, data);
            }
        }
    }

    public class EventManagerH
    {
        public static string FileExtension { get; private set; } = ".mpevent";

        public static List<EventFile> LoadedEvents { get; private set; } = null;

        public static void LoadAllEvents()
        {
            List<EventFile> toLoad = new List<EventFile>();
            foreach (string str in Directory.GetFiles(Master.EventsPath))
            {
                EventFile file = Serializer.SerializeFromFile<EventFile>(str);
                toLoad.Add(file);

                Printer.Warning($"Loaded event '{file.Name}'", Printer.LogImportanceMode.Extreme);
            }

            LoadedEvents = toLoad.OrderBy(fetch => fetch.Name).ToList();
        }
    }
}
