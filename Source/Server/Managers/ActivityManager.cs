using GameServer.Core;
using GameServer.Files;
using GameServer.Misc;
using GameServer.TCP;
using Shared;
using static Shared.CommonEnumerators;

namespace GameServer.Managers
{

    public static class ActivityManager
    {
        [HandlesPacket(PacketHeader.ActivityManager)]
        private static void ParsePacket(ServerClient client, byte[] bytes)
        {
            if (!Master.actionConfigs.EnableActivities)
            {
                ResponseShortcutManager.SendIllegalPacket(client, "Tried to use disabled feature!");
                return;
            }

            ActivityData data = Serializer.ConvertBytesToObject<ActivityData>(bytes);

            switch (data._stepMode)
            {
                case ActivityStepMode.Request:
                    SendRequestedMap(client, data);
                    break;
            }
        }

        private static void SendRequestedMap(ServerClient client, ActivityData data)
        {
            if (!MapManager.CheckIfMapExists(data._targetTile))
            {
                data._stepMode = ActivityStepMode.Deny;
                client.listener.EnqueuePacket(PacketHeader.ActivityManager, data);
            }

            else
            {
                data._stepMode = ActivityStepMode.Request;
                data._mapFile = MapManager.GetMapFromTile(data._targetTile);

                client.listener.EnqueuePacket(PacketHeader.ActivityManager, data);
            }
        }
    }
}
