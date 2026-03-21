using GameServer.Hooks.TCPNetwork;
using GameServer.Managers;
using Shared;
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
    public class CMD_ForceSave : CMD_Base
    {
        public CMD_ForceSave()
        {
            Prefix = "forcesave";
            Description = "Forces a save on the selected player";
            ParameterCount = 1;
        }

        public override void Action()
        {
            ServerClient toFind = ServerNetwork.GetConnectedClientFromUsername(CMD_Base.CommandParameters[0]);
            if (toFind == null) Printer.Warning($"User '{CMD_Base.CommandParameters[0]}' was not found");
            else
            {
                PKT_Command commandData = new PKT_Command();
                commandData._commandMode = CommandMode.ForceSave;

                toFind.Listener.EnqueuePacket(PacketHeader.ConsoleManager, commandData);

                Printer.Warning($"User '{CMD_Base.CommandParameters[0]}' has been forced to save");
            }
        }
    }
}
