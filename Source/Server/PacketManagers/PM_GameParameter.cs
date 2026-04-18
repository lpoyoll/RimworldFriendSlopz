using GameServer.Core;
using GameServer.Managers;
using GameServer.Misc;
using Shared;
using Shared.Files.Configs;
using TCPNetwork.Files.Client;
using TCPNetwork.PacketManagers;
using TCPNetwork.Packets;
using static TCPNetwork.Packets.GameParameterData;

namespace GameServer.PacketManager
{
    public class PM_GameParameter : PM_Base
    {
        [HandlesPacket(PacketHeader.GameParameterManager)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            GameParameterData data = Serializer.ConvertBytesToObject<GameParameterData>(bytes);

            switch (data._stepMode)
            {
                case GenStepMode.Scenario:
                    SetScenario(client, data._bytes);
                    break;

                case GenStepMode.Storyteller:
                    SetStoryteller(client, data._bytes);
                    break;

                case GenStepMode.Difficulty:
                    SetDifficulty(client, data._bytes);
                    break;
            }
        }

        private static void SetScenario(ServerClient client, byte[] bytes)
        {
            if (!client.GetOrSetClientData<UserFile>().IsAdmin && Master.WorldValues != null) ResponseShortcutManager.SendIllegalPacket(client, "Illegal setting of scenario!");
            else
            {
                FL_ScenarioConfig file = Serializer.ConvertBytesToObject<FL_ScenarioConfig>(bytes);

                Master.ScenarioValues = file;
                FL_ScenarioConfig.Save(FL_ScenarioConfig.SavePath, file);
                InformationDisplayer.DisplaySetScenario(client.GetOrSetClientData<UserFile>().Username);
            }
        }

        private static void SetStoryteller(ServerClient client, byte[] bytes)
        {
            if (!client.GetOrSetClientData<UserFile>().IsAdmin && Master.WorldValues != null) ResponseShortcutManager.SendIllegalPacket(client, "Illegal setting of storyteller!");
            else
            {
                FL_StorytellerConfig file = Serializer.ConvertBytesToObject<FL_StorytellerConfig>(bytes);

                Master.StorytellerValues = file;
                FL_StorytellerConfig.Save(FL_StorytellerConfig.SavePath, file);
                InformationDisplayer.DisplaySetStoryteller(client.GetOrSetClientData<UserFile>().Username);
            }
        }

        private static void SetDifficulty(ServerClient client, byte[] bytes)
        {
            if (!client.GetOrSetClientData<UserFile>().IsAdmin && Master.WorldValues != null) ResponseShortcutManager.SendIllegalPacket(client, "Illegal setting of difficulty!");
            else
            {
                FL_DifficultyConfig file = Serializer.ConvertBytesToObject<FL_DifficultyConfig>(bytes);

                Master.DifficultyValues = file;
                FL_DifficultyConfig.Save(FL_DifficultyConfig.SavePath, file);
                InformationDisplayer.DisplaySetDifficulty(client.GetOrSetClientData<UserFile>().Username);
            }
        }
    }
}
