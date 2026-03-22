using GameClient.Dialogs;
using GameClient.Managers;
using GameClient.Misc;
using RimWorld;
using Shared;
using Shared.Files.Configs.Mods;
using Shared.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TCPNetwork;
using TCPNetwork.Files.Client;
using TCPNetwork.Packets;
using Verse;
using Verse.Steam;
using static Shared.CommonEnumerators;
using static Shared.Files.Configs.Mods.ModsConfigFile;
using static Shared.Misc.Printer;
using static TCPNetwork.Packets.PKT_ModConfig;
using static UnityEngine.GraphicsBuffer;

namespace GameClient.PacketManagers
{
    public class PM_Mods : PM_Base
    {
        [HandlesPacket(PacketHeader.ModManager)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_ModConfig data = Serializer.ConvertBytesToObject<PKT_ModConfig>(bytes);

            switch (data._stepMode)
            {
                case ModConfigStepMode.Ask:
                    OpenModManagerMenu();
                    break;
            }
        }

        public static void OpenModManagerMenu(bool isFirstEdit = false)
        {
            Action toDo = delegate 
            {
                GameParameterManager.SendCurrentModConfigs(false);
                if (isFirstEdit) GameParameterManager.SetFirstTimeSetup();
            };

            List<string> modNames = new List<string>();
            foreach (ModConfig config in ModManagerH.GetRunningModList().ModConfigs) modNames.Add(config.FileName);

            string[] keys = modNames.ToArray();
            string[] values = new string[] { "Required", "Optional", "Forbidden" };

            DLG_ListingWithTuple dialog = new DLG_ListingWithTuple("Mod Manager", "Manage mods for the server", 
                keys, values, null, toDo);

            DLG_Base.PushNewDialog(dialog);
        }

        public static void ReceiveModConfigs(PKT_ServerGlobalData data)
        {
            SessionHandler.CurrentModConfig = data._modConfigs;

            if (!SessionHandler.CurrentModConfig.IsEnforced) return;
            else
            {
                Printer.Warning("Receiving mod configs from server", LogImportanceMode.Verbose);
                Printer.Warning("Currently doing nothing with the configs", LogImportanceMode.Verbose);
            }
        }
    }

    public class ModManagerH
    {
        public static ModsConfigFile GetRunningModList()
        {
            ModsConfigFile configFile = new ModsConfigFile();

            ModContentPack[] runningMods = LoadedModManager.RunningMods.ToArray();
            foreach (ModContentPack mod in runningMods)
            {
                ModConfig newConfig = new ModConfig();
                newConfig.FileName = mod.Name.Replace("steam_", "");

                configFile.ModConfigs.Add(newConfig);
            }

            return configFile;
        }

        public static void GetConflictingMods(PKT_Login data)
        {
            DLG_Base.PushNewDialog(new DLG_Listing("Mod Conflicts", "The following mods are conflicting with the server",
                data._extraDetails.ToArray()));
        }

        public static ModsConfigFile SortModsIntoCategories(string[] modNames, int[] categoryIndexes)
        {
            ModsConfigFile configFile = new ModsConfigFile();

            for (int i = 0; i < modNames.Length; i++)
            {
                ModConfig newConfig = new ModConfig();
                newConfig.FileName = modNames[i].Replace("steam_", "");
                newConfig.Type = (ModType)categoryIndexes[i];

                configFile.ModConfigs.Add(newConfig);
            }

            return configFile;
        }
    }
}
