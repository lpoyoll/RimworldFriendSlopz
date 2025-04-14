using GameServer.Misc;
using GameServer.TCP;
using Shared;
using static Shared.CommonEnumerators;

namespace GameServer.Managers
{

    public static class ResponseShortcutManager
    {
        public static void SendIllegalPacket(ServerClient client, string message, bool shouldBroadcast = true)
        {
            ResponseShortcutData data = new ResponseShortcutData();
            data._stepMode = ResponseStepMode.IllegalAction;

            client.listener.EnqueuePacket(PacketHeader.ResponseShortcutManager, data);
            client.listener.DisconnectFlag = true;

            if (shouldBroadcast)
            {
                Printer.Warning($"[Illegal action] > {client.userFile.Uid} > {client.userFile.SavedIP}");
                Printer.Warning($"[Illegal reason] > {message}");
            }
        }

        public static void SendUnavailablePacket(ServerClient client)
        {
            ResponseShortcutData data = new ResponseShortcutData();
            data._stepMode = ResponseStepMode.UserUnavailable;

            client.listener.EnqueuePacket(PacketHeader.ResponseShortcutManager, data);
        }

        public static void SendBreakPacket(ServerClient client)
        {
            ResponseShortcutData data = new ResponseShortcutData();
            data._stepMode = ResponseStepMode.Pop;

            client.listener.EnqueuePacket(PacketHeader.ResponseShortcutManager, data);
        }

        public static void SendNoPowerPacket(ServerClient client, PlayerGuildData data)
        {
            data._stepMode = GuildStepMode.NoPower;

            client.listener.EnqueuePacket(PacketHeader.ResponseShortcutManager, data);
        }
    }
}
