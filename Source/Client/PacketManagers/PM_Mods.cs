using GameClient.Dialogs;
using GameClient.Misc;
using Shared;
using Shared.Files.Configs;
using Shared.Files.Mods;
using Shared.Misc;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TCPNetwork;
using TCPNetwork.PacketManagers;
using TCPNetwork.Packets;
using Verse;
using static Shared.Files.Configs.FL_ModConfig;
using static Shared.Misc.Printer;
using static TCPNetwork.Packets.PKT_ModConfig;

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
                case ModConfigStepMode.Send:
                    SetValues(data._configFile.ModConfigs);
                    break;
            }
        }

        public static void OpenModManagerMenu() 
        { 
            if (SessionHandler.CurrentMods.Count > 0) DLG_Base.PushNewDialog(new DLG_ModConfig(SessionHandler.CurrentMods));
            else DLG_Base.PushNewDialog(new DLG_ModConfig(ModManagerH.GetRunningModList().ModConfigs));
        }

        public static void SetValues(List<ModConfig> mods) { SessionHandler.CurrentMods = mods; }
    }

    public class ModManagerH
    {
        public static FL_ModConfig GetRunningModList()
        {
            FL_ModConfig configFile = new FL_ModConfig();

            ModContentPack[] runningMods = LoadedModManager.RunningMods.ToArray();
            foreach (ModContentPack mod in runningMods)
            {
                ModConfig newConfig = new ModConfig();
                newConfig.FileName = mod.Name.Replace("steam_", "");
                newConfig.Type = ModType.Required;

                configFile.ModConfigs.Add(newConfig);
            }

            return configFile;
        }

        public static void GetConflictingMods(PKT_Login data) { DLG_Base.PushNewDialog(new DLG_ModRejection(data._extraDetails)); }

        public static FL_ModConfig SortModsIntoCategories(List<ModConfig> mods, List<int> categoryIndexes)
        {
            FL_ModConfig configFile = new FL_ModConfig();

            for (int i = 0; i < mods.Count; i++)
            {
                ModConfig newConfig = new ModConfig();
                newConfig.FileName = mods[i].FileName.Replace("steam_", "");
                newConfig.Type = (ModType)categoryIndexes[i];

                configFile.ModConfigs.Add(newConfig);
            }

            return configFile;
        }
    }
}
