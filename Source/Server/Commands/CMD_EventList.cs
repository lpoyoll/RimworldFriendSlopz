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
    public class CMD_EventList : CMD_Base
    {
        public CMD_EventList()
        {
            Prefix = "eventlist";
            Description = "Displays a list of all server events";
            ParameterCount = 0;
        }

        public override void Action()
        {
            Printer.Title($"Available events: [{EventManagerH.LoadedEvents.Count}]");
            Printer.Title("----------------------------------------");
            foreach (EventFile eventFile in EventManagerH.LoadedEvents) Printer.Warning($"{eventFile.DefName}");
            Printer.Title("----------------------------------------");
        }
    }
}
