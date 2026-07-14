using RTNetwork.Components;
using RTNetwork.PacketManagers;
using RTNetwork.Packets;
using RTServer.Core;
using RTServer.Hooks.TCPNetwork;
using RTServer.Managers;
using RTServer.Misc;
using RTShared.Files.Configs;
using RTShared.Files.Mods;
using RTShared.Files.Player;
using RTShared.Misc;
using static RTNetwork.Packets.PKT_Login;
using static RTNetwork.Packets.PKT_ModConfig;

namespace RTServer.PacketManagers
{
    public class PM_Mods : PM_Base
    {
        [HandlesPacket(PacketHeader.Mod)]
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
            if (Master.WorldValues != null && !client.GetData<FL_Player>().IsAdmin)
            {
                UserManager.BanPlayerFromName(client.GetData<FL_Player>().Username);
                Printer.Warning($"Player {client.GetData<FL_Player>().Username} tried to change mod config without being admin");
            }

            else
            {
                Master.ModConfig = file;
                FL_ModConfig.Save(FL_ModConfig.SavePath, file);
                InformationDisplayer.DisplaySetMods(client);

                PKT_ModConfig packet = new PKT_ModConfig();
                packet._configFile = Master.ModConfig;
                packet._stepMode = ModConfigStepMode.Send;

                ServerNetwork.SendPacketToAllClients(PacketHeader.Mod, packet);
            }
        }

        public static bool CheckIfModConflict(ServerClient client, PKT_Login loginData)
        {
            if (Master.ModConfig.BypassMods) return false;
            else
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
                    if (client.GetData<FL_Player>().IsAdmin)
                    {
                        InformationDisplayer.DisplayModBypass(client.GetData<FL_Player>().Username);
                        return false;
                    }

                    else
                    {
                        InformationDisplayer.DisplayModMismatch(client.GetData<FL_Player>().Username);
                        PM_Login.DenyConnectionWithReason(client, LoginResponse.Mods, conflictingModNames);
                        return true;
                    }
                }
            }
        }
    }
}
