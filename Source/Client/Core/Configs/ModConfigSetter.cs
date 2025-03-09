using GameClient.Core.Preferences;
using GameClient.Dialogs;
using GameClient.Managers;
using GameClient.Misc;
using GameClient.Values;
using Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;
using System.Diagnostics;

namespace GameClient.Core.Configs
{
    public class ModConfigSetter : Mod
    {
        private readonly ModConfigGetter modConfigs;

        public ModConfigSetter(ModContentPack content) : base(content)
        {
            modConfigs = GetSettings<ModConfigGetter>();
        }

        public override string SettingsCategory() { return "RimWorld Together"; }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            listingStandard.GapLine();
            listingStandard.Label("Multiplayer Parameters");
            listingStandard.CheckboxLabeled("[When Playing] Deny all incoming transfers", ref ModConfigGetter.RejectTransfersBool, "Automatically denies transfers");
            listingStandard.CheckboxLabeled("[When Playing] Deny all incoming site rewards", ref ModConfigGetter.RejectSiteRewardsBool, "Automatically site rewards");
            listingStandard.CheckboxLabeled("[When Playing] Mute incomming chat messages", ref ModConfigGetter.MuteChatSoundBool, "Mute chat messages");

            listingStandard.GapLine();
            listingStandard.Label("Debugging");
            if (listingStandard.ButtonTextLabeled("Verbosity mode", $"{ModConfigGetter.CurrentVerboseMode}")) ShowVerboseFloatMenu();
            if (listingStandard.ButtonTextLabeled("Open logs folder", "Open")) StartProcess(Master.appdataPath);

            listingStandard.GapLine();
            listingStandard.Label("Tweaks");
            if (listingStandard.ButtonTextLabeled("Change mod version [Windows only]", "Change")) { VersionManager.PromptChangeVersion(); }
            if (listingStandard.ButtonTextLabeled("Export account", "Export")) { ShowExportAccountQuestion(); }

            GUI.color = Color.red;
            if (listingStandard.ButtonTextLabeled("Reset account [DANGEROUS]", "Reset")) { ShowResetAccountQuestion(); }
            GUI.color = Color.white;

            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        private void ShowVerboseFloatMenu()
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            List<Tuple<string, ClientValues.VerboseMode>> verboseModes = new List<Tuple<string, ClientValues.VerboseMode>>()
            {
                Tuple.Create("None", ClientValues.VerboseMode.None),
                Tuple.Create("Verbose", ClientValues.VerboseMode.Verbose),
                Tuple.Create("Extreme", ClientValues.VerboseMode.Extreme)
            };

            foreach (Tuple<string, ClientValues.VerboseMode> tuple in verboseModes)
            {
                FloatMenuOption item = new FloatMenuOption(tuple.Item1, delegate
                {
                    ModConfigGetter.CurrentVerboseMode = tuple.Item2;
                });

                list.Add(item);
            }

            Find.WindowStack.Add(new FloatMenu(list));
        }

        private void ShowExportAccountQuestion()
        {
            Action toDo = delegate
            {
                try
                {
                    string path = Path.Combine(Master.appdataRTPath, "LoginData.json");
                    string destination = Path.Combine(DialogManager.dialogInputResults[0], Path.GetFileName(path));
                    File.Copy(path, destination);

                    string[] messages = new string[]
                    {
                        "Account file was exported correctly!",
                        "Put it inside the \"RimWorld Together\" AppData folder of the new machine"
                    };

                    DialogManager.PushNewDialog(new RT_Dialog_Message("MESSAGE", messages));
                }
                catch { DialogManager.PushNewDialog(new RT_Dialog_Message("MESSAGE", new string[] { "Path couldn't be found!" })); }
            };

            DialogManager.PushNewDialog(new RT_Dialog_Inputs("Choose where to export the file at", new string[] { "Path" }, new bool[] { false }, toDo));
        }

        private void ShowResetAccountQuestion()
        {
            RT_Dialog_YesNo dialog = new RT_Dialog_YesNo("Are you sure you want to RESET your ACCOUNT?",
                delegate
                {
                    UserLoginHandler.DeleteLoginData();
                    DialogManager.PushNewDialog(new RT_Dialog_Message("MESSAGE", new string[] { "Account has been reset" }));
                });

            DialogManager.PushNewDialog(dialog);
        }

        private void StartProcess(string processPath)
        {
            try { Process.Start(processPath); }
            catch { Printer.Warning($"Failed to start process {processPath}"); }
        }
    }
}
