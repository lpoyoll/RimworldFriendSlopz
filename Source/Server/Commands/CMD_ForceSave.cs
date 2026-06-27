using RTServer.Hooks.TCPNetwork;
using RTShared.Commands;
using RTShared.Misc;
using RTNetwork.Packets;
using static RTShared.Misc.CommonEnumerators;
using RTNetwork.Components;

namespace RTServer.Commands
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

                toFind.Listener.EnqueuePacket(PacketHeader.Console, commandData);

                Printer.Warning($"User '{CMD_Base.CommandParameters[0]}' has been forced to save");
            }
        }
    }
}
