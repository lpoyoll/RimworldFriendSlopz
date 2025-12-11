using GameServer.Core;
using GameServer.Misc;
using Shared;
using static Shared.CommonEnumerators;
using Shared.Files;
using TCPNetwork.Packets;
using TCPNetwork.Files.Client;

namespace GameServer.Managers
{

    public static class ModManager
    {
        [HandlesPacket(PacketHeader.ModManager)]
        private static void ParsePacket(ServerClient client, byte[] bytes, PacketHeader header)
        {
            ModConfigData data = Serializer.ConvertBytesToObject<ModConfigData>(bytes);

            Printer.Warning(data, LogImportanceMode.Extreme);

            switch (data._stepMode)
            {
                case ModConfigStepMode.Send:
                    SaveModConfig(client, data._configFile);
                    break;
            }
        }

        private static void SaveModConfig(ServerClient client, ModConfigFile file)
        {
            if (Master.WorldValues != null && !client.UserFile.IsAdmin)
            {
                UserManager.BanPlayerFromName(client.UserFile.Username);
                Printer.Warning($"Player {client.UserFile.Username} tried to change mod config without being admin");
            }

            else
            {
                Master.ModConfig = file;
                Master.ModConfig.Save();
                InformationDisplayer.DisplaySetMods(client);
            }
        }

        public static bool CheckIfModConflict(ServerClient client, LoginData loginData)
        {
            List<string> conflictingMods = new List<string>();
            List<string> conflictingNames = new List<string>();
            string[] clientMods = loginData._runningMods.UnsortedMods;

            //Check for required mods

            if (Master.ModConfig.RequiredMods.Length > 0)
            {
                foreach (string str in Master.ModConfig.RequiredMods)
                {
                    if (!clientMods.Contains(str))
                    {
                        conflictingMods.Add($"[Required] > {str}");
                        conflictingNames.Add(str);
                        continue;
                    }
                }

                //Check for optional mods

                foreach (string str in clientMods)
                {
                    if (conflictingNames.Contains(str)) continue;
                    else if (!Master.ModConfig.RequiredMods.Contains(str) && !Master.ModConfig.OptionalMods.Contains(str))
                    {
                        conflictingMods.Add($"[Disallowed] > {str}");
                        conflictingNames.Add(str);
                        continue;
                    }
                }
            }

            //Check for forbidden mods

            if (Master.ModConfig.ForbiddenMods.Length > 0)
            {
                foreach (string str in Master.ModConfig.ForbiddenMods)
                {
                    if (conflictingNames.Contains(str)) continue;
                    else if (clientMods.Contains(str))
                    {
                        conflictingMods.Add($"[Forbidden] > {str}");
                        conflictingNames.Add(str);
                    }
                }
            }

            //Check for final conflicting count

            if (conflictingMods.Count == 0) return false;
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
                    LoginManagerH.DenyConnectionWithReason(client, LoginResponse.WrongMods, conflictingMods);
                    return true;
                }
            }
        }
    }
}
