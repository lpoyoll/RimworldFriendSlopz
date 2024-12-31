using GameServer.Core;
using GameServer.Misc;
using GameServer.TCP;
using Shared;
using static Shared.CommonEnumerators;

namespace GameServer.Managers
{
    [RTManager]
    public static class GenManager
    {
        public static void ParsePacket(ServerClient client, Packet packet)
        {
            GenData data = Serializer.ConvertBytesToObject<GenData>(packet.contents);

            switch (data._stepMode)
            {
                case GenStepMode.Scenario:
                    break;

                case GenStepMode.Storyteller:
                    break;

                case GenStepMode.Difficulty:
                    SetCustomDifficulty(client, data._difficulty);
                    break;
            }
        }

        public static void SetCustomDifficulty(ServerClient client, DifficultyValuesFile file)
        {
            if (!client.userFile.IsAdmin)
            {
                UserManager.BanPlayerFromName(client.userFile.Uid);
                Printer.Warning($"Player {client.userFile.Uid} attempted to set the custom difficulty while not being an admin");
            }

            else
            {
                Master.difficultyValues = file;
                Main_.SaveValueFile(ServerFileMode.Difficulty, true);
                Printer.Warning($"[Set difficulty] > {client.userFile.Uid}");
            }
        }
    }
}
