using GameServer.Core;
using GameServer.Hooks.TCPNetwork;
using GameServer.Managers;
using GameServer.Misc;
using Shared;
using Shared.Files.Configs;
using Shared.Files.Mods;
using Shared.Misc;
using TCPNetwork;
using TCPNetwork.Files.Client;
using TCPNetwork.PacketManagers;
using TCPNetwork.Packets;
using static TCPNetwork.Packets.PKT_Login;
using static TCPNetwork.Packets.PKT_ModConfig;

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

        private static void SaveModConfig(ServerClient client, FL_ModConfig file)
        {
            if (Master.WorldValues != null && !client.GetData<UserFile>().IsAdmin)
            {
                UserManager.BanPlayerFromName(client.GetData<UserFile>().Username);
                Printer.Warning($"Player {client.GetData<UserFile>().Username} tried to change mod config without being admin");
            }

            else
            {
                Master.ModConfig = file;
                FL_ModConfig.Save(FL_ModConfig.SavePath, file);
                InformationDisplayer.DisplaySetMods(client);

                PKT_ModConfig packet = new PKT_ModConfig();
                packet._configFile = Master.ModConfig;
                packet._stepMode = ModConfigStepMode.Send;

                ServerNetwork.SendPacketToAllClients(PacketHeader.ModManager, packet);
            }
        }

        public static bool CheckIfModConflict(ServerClient client, PKT_Login loginData)
        {
            List<string> conflictingModNames = new List<string>();

            //Check if missing required mods

            foreach (ModConfig config in Master.ModConfig.ModConfigs.Where(fetch => fetch.Type == FL_ModConfig.ModType.Required))
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
                    && (fetch.Type == FL_ModConfig.ModType.Required || fetch.Type == FL_ModConfig.ModType.Optional));

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
                if (client.GetData<UserFile>().IsAdmin)
                {
                    InformationDisplayer.DisplayModBypass(client.GetData<UserFile>().Username);
                    return false;
                }

                else
                {
                    InformationDisplayer.DisplayModMismatch(client.GetData<UserFile>().Username);
                    PM_Login.DenyConnectionWithReason(client, LoginResponse.Mods, conflictingModNames);
                    return true;
                }
            }
        }
    }
}
