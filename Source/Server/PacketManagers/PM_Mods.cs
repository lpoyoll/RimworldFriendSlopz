using GameServer.Core;
using GameServer.Managers;
using GameServer.Misc;
using Shared;
using Shared.Files.Configs.Mods;
using Shared.Misc;
using TCPNetwork;
using TCPNetwork.Files.Client;
using TCPNetwork.Packets;
using static Shared.CommonEnumerators;

namespace GameServer.PacketManager
{
    public class PM_Mods : PM_Base
    {
        [HandlesPacket(PacketHeader.ModManager)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_ModConfig data = Serializer.ConvertBytesToObject<PKT_ModConfig>(bytes);

            switch (data._stepMode)
            {
                case ModConfigStepMode.Send:
                    SaveModConfig(client, data._configFile);
                    break;
            }
        }

        private static void SaveModConfig(ServerClient client, ModsConfigFile file)
        {
            if (Master.WorldValues != null && !client.UserFile.IsAdmin)
            {
                UserManager.BanPlayerFromName(client.UserFile.Username);
                Printer.Warning($"Player {client.UserFile.Username} tried to change mod config without being admin");
            }

            else
            {
                Master.ModConfig = file;
                ModsConfigFile.Save(ModsConfigFile.SavePath, file);
                InformationDisplayer.DisplaySetMods(client);
            }
        }

        public static bool CheckIfModConflict(ServerClient client, PKT_Login loginData)
        {
            List<string> conflictingModNames = new List<string>();

            //Check if missing required mods

            foreach (ModConfig config in Master.ModConfig.ModConfigs.Where(fetch => fetch.Type == ModsConfigFile.ModType.Required))
            {
                ModConfig toFind = loginData._runningMods.ModConfigs.Find(fetch => fetch.FileName == config.FileName);
                if (toFind == null)
                {
                    conflictingModNames.Add($"[Required] > {config.FileName}");
                    continue;
                }
            }

            //Check if has mods that aren't required or optional

            foreach (ModConfig config in loginData._runningMods.ModConfigs)
            {
                ModConfig toFind = Master.ModConfig.ModConfigs.Find(fetch => fetch.FileName == config.FileName 
                    && (fetch.Type == ModsConfigFile.ModType.Required || fetch.Type == ModsConfigFile.ModType.Optional));

                if (toFind == null)
                {
                    conflictingModNames.Add($"[Disallowed] > {config.FileName}");
                    continue;
                }
            }

            //Check for final conflicting count

            if (conflictingModNames.Count == 0) return false;
            else
            {
                if (client.UserFile.IsAdmin)
                {
                    InformationDisplayer.DisplayModBypass(client.UserFile.Username);
                    return false;
                }

                else
                {
                    InformationDisplayer.DisplayModMismatch(client.UserFile.Username);
                    LoginManagerH.DenyConnectionWithReason(client, LoginResponse.Mods, conflictingModNames);
                    return true;
                }
            }
        }
    }
}
