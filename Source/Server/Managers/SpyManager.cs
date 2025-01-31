using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameServer.Core;
using GameServer.Files;
using GameServer.Misc;
using GameServer.TCP;
using Shared;
using static Shared.CommonEnumerators;

namespace GameServer.Managers
{
    [RTManager]
    public static class SpyManager
    {
        public static void ParsePacket(ServerClient client, Packet packet)
        {
            if (!Master.actionConfigs.EnableSpying)
            {
                ResponseShortcutManager.SendIllegalPacket(client, "Tried to use disabled feature!");
                return;
            }

            SpyData data = Serializer.ConvertBytesToObject<SpyData>(packet.contents);

            switch (data._stepMode)
            {
                case SpyStepMode.Request:
                    GetMapForSpy(client, data);
                    break;
            }
        }

        private static void GetMapForSpy(ServerClient client, SpyData data)
        {
            if (!ValueChecker.CheckIfCanSpy(client.userFile)) data._stepMode = SpyStepMode.Deny;
            else
            {
                data._stepMode = SpyStepMode.Accept;

                if (MapManager.CheckIfMapExists(data._mapTile))
                {
                    data._mapFile = MapManager.GetMapFromTile(data._mapTile);
                    client.userFile.UpdateSpyTime();
                }
            }

            Packet packet = Packet.CreatePacketFromObject(nameof(SpyManager), data);
            client.listener.EnqueuePacket(packet);
        }
    }
}
