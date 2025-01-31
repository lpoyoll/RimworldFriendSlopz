using GameClient.Core;
using GameClient.Dialogs;
using GameClient.Misc;
using GameClient.TCP;
using GameClient.Values;
using RimWorld;
using Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;
using static Shared.CommonEnumerators;

namespace GameClient.Managers
{
    [RTManager]
    public static class ModManager
    {
        public static void ParsePacket(Packet packet)
        {
            ModConfigData data = Serializer.ConvertBytesToObject<ModConfigData>(packet.contents);

            switch (data._stepMode)
            {
                case ModConfigStepMode.Ask:
                    OpenModManagerMenu(false);
                    break;
            }
        }

        public static void OpenModManagerMenu(bool isFirstEdit)
        {
            Action toDo = delegate
            {
                AskForSyncConfigs(isFirstEdit);

                if (isFirstEdit) return;
                else DialogManager.PushNewDialog(new RT_Dialog_Message("MESSAGE", new string[] { "Mod configuration has been changed!" }));
            };

            string[] keys = ModManagerH.GetRunningModList().UnsortedMods;
            string[] values = new string[] { "Required", "Optional", "Forbidden" };
            RT_Dialog_ListingWithTuple dialog = new RT_Dialog_ListingWithTuple("Mod Manager", "Manage mods for the server", keys, values, toDo);
            DialogManager.PushNewDialog(dialog);
        }

        public static void ReceiveMods(ServerGlobalData data)
        {
            SessionValues.configFile = data._modConfigs;

            if (!SessionValues.configFile.EnforcedConfigs) return;
            else
            {
                Printer.Warning("Receiving mod configs from server", LogImportanceMode.Verbose);

                for (int i = 0; i < SessionValues.configFile.ModFileNames.Length; i++)
                {
                    string filePath = GenFilePaths.ConfigFolderPath + Path.DirectorySeparatorChar + SessionValues.configFile.ModFileNames[i];
                    if (File.Exists(filePath)) File.Delete(filePath);
                    File.WriteAllText(filePath, SessionValues.configFile.ModConfigs[i]);

                    Printer.Warning($"Loaded > {SessionValues.configFile.ModFileNames[i]}", LogImportanceMode.Verbose);
                }
            }
        }

        private static void AskForSyncConfigs(bool isFirstEdit)
        {
            ModConfigData data = new ModConfigData();
            data._stepMode = ModConfigStepMode.Send;
            data._configFile = ModManagerH.SortModsIntoCategories(DialogManager.dialogTupleListingResultString, 
                DialogManager.dialogTupleListingResultInt);

            Action toDoYes = delegate 
            { 
                SendModConfigs(data);
                if (isFirstEdit) OnFirstEdit(); 
            };

            Action toDoNo = delegate
            {
                Packet packet = Packet.CreatePacketFromObject(nameof(ModManager), data);
                Network.listener.EnqueuePacket(packet);
                if (isFirstEdit) OnFirstEdit();
            };

            DialogManager.PushNewDialog(new RT_Dialog_YesNo("Do you want to enforce the mod settings?",
                toDoYes, toDoNo));
        }

        public static void SendModConfigs(ModConfigData data)
        {
            List<string> modFileNames = new List<string>();
            List<string> modConfigs = new List<string>();
            foreach (string str in ModManagerH.GetAllModConfigs())
            {
                modFileNames.Add(Path.GetFileName(str));
                modConfigs.Add(File.ReadAllText(str));
            }
            data._configFile.ModFileNames = modFileNames.ToArray();
            data._configFile.ModConfigs = modConfigs.ToArray();
            data._configFile.EnforcedConfigs = true;

            Packet packet = Packet.CreatePacketFromObject(nameof(ModManager), data);
            Network.listener.EnqueuePacket(packet);
        }

        public static void OnFirstEdit()
        {
            Page toUse = new Page_SelectScenario();
            toUse.next = new Page_SelectStartingSite();
            DialogManager.PushNewDialog(toUse);
        }
    }

    public static class ModManagerH
    {
        public static string[] GetAllModConfigs()
        {
            return Directory.GetFiles(GenFilePaths.ConfigFolderPath)
                .Where(fetch => Path.GetFileName(fetch).StartsWith("Mod_")).ToArray();
        }

        public static ModConfigFile GetRunningModList()
        {
            List<string> loadedMods = new List<string>();
            ModContentPack[] runningMods = LoadedModManager.RunningMods.ToArray();
            foreach (ModContentPack mod in runningMods) loadedMods.Add(mod.PackageId);
            loadedMods.Sort();

            ModConfigFile configFile = new ModConfigFile();
            configFile.UnsortedMods = loadedMods.ToArray();
            return configFile;
        }

        public static void GetConflictingMods(Packet packet)
        {
            LoginData loginData = Serializer.ConvertBytesToObject<LoginData>(packet.contents);

            DialogManager.PushNewDialog(new RT_Dialog_Listing("Mod Conflicts", "The following mods are conflicting with the server",
                loginData._extraDetails.ToArray()));
        }

        public static ModConfigFile SortModsIntoCategories(string[] modNames, int[] categoryIndexes)
        {
            ModConfigFile configFile = new ModConfigFile();
            List<string> requiredMods = new List<string>();
            List<string> optionalMods = new List<string>();
            List<string> forbiddenMods = new List<string>();

            for (int i = 0; i < modNames.Length; i++)
            {
                switch ((ModType)categoryIndexes[i])
                {
                    case ModType.Required:
                        requiredMods.Add(modNames[i]);
                        break;

                    case ModType.Optional:
                        optionalMods.Add(modNames[i]);
                        break;

                    case ModType.Forbidden:
                        forbiddenMods.Add(modNames[i]);
                        break;
                }
            }

            configFile.UnsortedMods = modNames;
            configFile.RequiredMods = requiredMods.ToArray();
            configFile.OptionalMods = optionalMods.ToArray();
            configFile.ForbiddenMods = forbiddenMods.ToArray();

            return configFile;
        }
    }
}
