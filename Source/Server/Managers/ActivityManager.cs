using GameServer.Core;
using GameServer.Files;
using GameServer.Misc;
using GameServer.TCP;
using Shared;
using static Shared.CommonEnumerators;

namespace GameServer.Managers
{
    [RTManager]
    public static class ActivityManager
    {
        private static void ParsePacket(ServerClient client, Packet packet)
        {
            if (!Master.actionConfigs.EnableActivities)
            {
                ResponseShortcutManager.SendIllegalPacket(client, "Tried to use disabled feature!");
                return;
            }

            ActivityData data = Serializer.ConvertBytesToObject<ActivityData>(packet.Contents);

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
                Packet packet = Packet.CreateFromObject(nameof(ActivityManager), data);
                client.listener.EnqueuePacket(packet);
            }

            else
            {
                data._stepMode = ActivityStepMode.Request;
                data._mapFile = MapManager.GetMapFromTile(data._targetTile);

                Packet packet = Packet.CreateFromObject(nameof(ActivityManager), data);
                client.listener.EnqueuePacket(packet);
            }
        }
    }
}
