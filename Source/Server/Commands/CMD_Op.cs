using GameServer.Hooks.TCPNetwork;
using GameServer.Managers;
using Shared;
using Shared.Commands;
using Shared.Misc;
using TCPNetwork;
using TCPNetwork.Files.Client;
using TCPNetwork.Packets;
using static Shared.CommonEnumerators;

namespace GameServer.Commands
{
    public class CMD_Op : CMD_Base
    {
        public CMD_Op()
        {
            Prefix = "op";
            Description = "Gives admin privileges to the selected player";
            ParameterCount = 1;
        }

        public override void Action()
        {
            UserFile toFind = UserManagerH.GetAllUserFiles().Where(x => x.Username == CMD_Base.CommandParameters[0]).FirstOrDefault();
            if (toFind == null) Printer.Warning($"User '{CMD_Base.CommandParameters[0]}' was not found");
            else
            {
                if (toFind.IsAdmin) Printer.Warning($"User '{toFind.Username}' was already an admin");
                else
                {
                    toFind.UpdateAdmin(true);

                    ServerClient client = ServerNetwork.GetConnectedClientFromUsername(toFind.Username);
                    if (client != null)
                    {
                        PKT_Command commandData = new PKT_Command();
                        commandData._commandMode = CommandMode.Op;

                        client.GetData<UserFile>().UpdateAdmin(true);
                        client.Listener.EnqueuePacket(PacketHeader.ConsoleManager, commandData);
                    }

                    Printer.Warning($"User '{toFind.Username}' has now admin privileges");
                }
            }
        }
    }
}
