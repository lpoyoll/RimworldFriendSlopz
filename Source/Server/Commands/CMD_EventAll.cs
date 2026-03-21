using GameServer.Hooks.TCPNetwork;
using GameServer.Managers;
using GameServer.PacketManager;
using Shared;
using Shared.Files;
using Shared.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCPNetwork.Files.Client;
using TCPNetwork.Packets;
using static Shared.CommonEnumerators;

namespace GameServer.Commands
{
    public class CMD_EventAll : CMD_Base
    {
        public CMD_EventAll()
        {
            Prefix = "eventall";
            Description = "Sends an event to all connected players";
            ParameterCount = 2;
        }

        public override void Action()
        {
            EventFile toFind = EventManagerH.LoadedEvents.FirstOrDefault(fetch => fetch.DefName == CMD_Base.CommandParameters[0]);
            if (toFind == null) Printer.Warning($"Event '{CMD_Base.CommandParameters[0]}' was not found");
            else
            {
                foreach (ServerClient client in ServerNetwork.GetConnectedClients())
                {
                    PKT_Event eventData = new PKT_Event();
                    eventData._stepMode = EventStepMode.Receive;
                    eventData._eventFile = toFind;

                    //We set it to -1 to let the client know it will fall at any settlement
                    eventData._toTile = -1;

                    client.Listener.EnqueuePacket(PacketHeader.EventManager, eventData);
                }

                Printer.Title($"Sent event '{CMD_Base.CommandParameters[0]}' to every connected player");
            }
        }
    }
}
