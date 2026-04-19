using GameServer.Hooks.TCPNetwork;
using GameServer.PacketManager;
using Shared;
using Shared.Commands;
using Shared.Files;
using Shared.Misc;
using TCPNetwork.Files.Client;
using TCPNetwork.Packets;
using static TCPNetwork.Packets.PKT_Event;

namespace GameServer.Commands
{
    public class CMD_Event : CMD_Base
    {
        public CMD_Event()
        {
            Prefix = "event";
            Description = "Sends an event to the selecte player";
            ParameterCount = 2;
        }

        public override void Action() 
        {
            ServerClient client = ServerNetwork.GetConnectedClientFromUsername(CMD_Base.CommandParameters[0]);
            if (client == null) Printer.Warning($"User '{CMD_Base.CommandParameters[0]}' was not found");
            else
            {
                FL_Event toFind = EventManagerH.LoadedEvents.FirstOrDefault(fetch => fetch.DefName == CMD_Base.CommandParameters[1]);
                if (toFind == null) Printer.Warning($"Event '{CMD_Base.CommandParameters[1]}' was not found");
                else
                {
                    PKT_Event eventData = new PKT_Event();
                    eventData._stepMode = EventStepMode.Receive;
                    eventData._eventFile = toFind;

                    //We set it to -1 to let the client know it will fall at any settlement
                    eventData._toTile = -1;

                    client.Listener.EnqueuePacket(PacketHeader.EventManager, eventData);

                    Printer.Title($"Sent event '{CMD_Base.CommandParameters[1]}' to '{CMD_Base.CommandParameters[0]}'");
                }
            }
        }
    }
}
