using GameServer.PacketManager;
using RTShared.Commands;
using RTShared.Files;
using RTShared.Misc;

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
            foreach (FL_Event eventFile in EventManagerH.LoadedEvents) Printer.Warning($"{eventFile.DefName}");
            Printer.Title("----------------------------------------");
        }
    }
}
