using GameServer.Hooks.TCPNetwork;
using Shared;
using Shared.Misc;
using TCPNetwork.Files.Client;

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
            foreach (ServerClient client in ServerNetwork.GetConnectedClients()) Printer.Warning($"{client.CurrentIP} - {client.UserFile.Username}");
            Printer.Title("----------------------------------------");
        }
    }
}
