using GameServer.Hooks.TCPNetwork;
using RTShared.Commands;
using RTShared.Misc;
using RTNetwork.Components;

namespace GameServer.Commands
{
    public class CMD_List : CMD_Base
    {
        public CMD_List()
        {
            Prefix = "list";
            Description = "Shows all connected players";
        }

        public override void Action() 
        {
            Printer.Title($"Connected players: [{ServerNetwork.GetConnectedClients().Count()}]");

            Printer.Title("----------------------------------------");
            foreach (ServerClient client in ServerNetwork.GetConnectedClients()) Printer.Warning($"{client.IP}");
            Printer.Title("----------------------------------------");
        }
    }
}
