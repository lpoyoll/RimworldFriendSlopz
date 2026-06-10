using GameServer.Hooks.TCPNetwork;
using GameServer.Managers;
using RTShared;
using RTShared.Commands;
using RTShared.Misc;
using RTShared.Files.ServerClient;
using RTNetwork.Packets;
using static RTShared.CommonEnumerators;
using RTNetwork.Components;

namespace GameServer.Commands
{
    public class CMD_Deop : CMD_Base
    {
        public CMD_Deop()
        {
            Prefix = "deop";
            Description = "Removes admin privileges from the selected player";
            ParameterCount = 1;
        }

        public override void Action()
        {
            FL_Player toFind = UserManagerH.GetAllUserFiles().Where(x => x.Username == CMD_Base.CommandParameters[0]).FirstOrDefault();

            if (toFind == null) Printer.Warning($"User '{CMD_Base.CommandParameters[0]}' was not found");
            else
            {
                if (!toFind.IsAdmin) Printer.Warning($"User '{toFind.Username}' was not an admin");
                else
                {
                    toFind.UpdateAdmin(false);
                    ServerClient client = ServerNetwork.GetConnectedClientFromUsername(toFind.Username);
                    if (client != null)
                    {
                        PKT_Command commandData = new PKT_Command();
                        commandData._commandMode = CommandMode.Deop;

                        client.GetData<FL_Player>().UpdateAdmin(false);
                        client.Listener.EnqueuePacket(PacketHeader.Console, commandData);
                    }

                    Printer.Warning($"User '{toFind.Username}' is no longer an admin");
                }
            }
        }
    }
}
