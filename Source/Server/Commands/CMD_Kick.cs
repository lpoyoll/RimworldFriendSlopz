using GameServer.Hooks.TCPNetwork;
using Shared.Commands;
using Shared.Misc;
using TCPNetwork;
using TCPNetwork.Files.Client;

namespace GameServer.Commands
{
    public class CMD_Kick : CMD_Base
    {
        public CMD_Kick()
        {
            Prefix = "kick";
            Description = "Kicks the selected player from the server";
            ParameterCount = 1;
        }

        public override void Action()
        {
            ServerClient toFind = ServerNetwork.GetConnectedClientFromUsername(CMD_Base.CommandParameters[0]);
            if (toFind == null) Printer.Warning($"User '{CMD_Base.CommandParameters[0]}' was not found");
            else
            {
                toFind.Listener.MarkForDisconnect();
                Printer.Warning($"User '{toFind.GetData<UserFile>().Username}' has been kicked from the server");
            }
        }
    }
}
