using GameClient.Dialogs;
using GameClient.Dialogs.Default;
using GameClient.Files;
using GameClient.PacketManagers;
using Shared.Misc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Verse;

namespace GameClient.Core.Configs
{
    public class ModConfigSetter : Mod
    {
        private ModConfigGetter ModConfigs { get; set; }

        public ModConfigSetter(ModContentPack content) : base(content)
        {
            ModConfigs = GetSettings<ModConfigGetter>();
        }

        public override string SettingsCategory() { return "RimWorld Together"; }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            listingStandard.Label("Debugging");
            if (listingStandard.ButtonTextLabeled("Verbosity mode", $"{ModConfigGetter.CurrentVerboseMode}")) ShowVerbosityMenu();
            if (listingStandard.ButtonTextLabeled("Open logs folder", "Open")) StartProcess(Master.AppdataPath);
            listingStandard.CheckboxLabeled("Bypass mod compatibility check", ref ModConfigGetter.BypassModCompatibilityCheck, "Bypass");

            listingStandard.GapLine();
            listingStandard.Label("Tweaks");
            if (listingStandard.ButtonTextLabeled("Change mod version [Windows only]", "Change")) { PM_Version.PromptChangeVersion(); }
            if (listingStandard.ButtonTextLabeled("Export account", "Export")) { ShowExportAccountQuestion(); }

            listingStandard.GapLine();
            listingStandard.Label("DANGEROUS");
            GUI.color = Color.red;
            if (listingStandard.ButtonTextLabeled("Reset account", "Reset")) { ShowResetAccountQuestion(); }
            GUI.color = Color.white;

            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        private void ShowVerbosityMenu()
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();

            List<Tuple<string, Printer.LogImportanceMode>> modes = new List<Tuple<string, Printer.LogImportanceMode>>()
            {
                Tuple.Create("None", Printer.LogImportanceMode.Normal),
                Tuple.Create("Verbose", Printer.LogImportanceMode.Verbose),
                Tuple.Create("Extreme", Printer.LogImportanceMode.Extreme),
                Tuple.Create("Ludicrous", Printer.LogImportanceMode.Ludicrous)
            };

            foreach (Tuple<string, Printer.LogImportanceMode> tuple in modes)
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
                    string path = Path.Combine(Master.AppdataRTPath, "LoginData.json");
                    string destination = Path.Combine(DLG_Inputs.DialogInputResults[0], Path.GetFileName(path));
                    File.Copy(path, destination);

                    string[] messages = new string[]
                    {
                        "Account file was exported correctly!",
                        "Put it inside the \"RimWorld Together\" AppData folder of the new machine"
                    };

                    DLG_Base.PushNewDialog(new DLG_Message("MESSAGE", messages));
                }
                catch { DLG_Base.PushNewDialog(new DLG_Message("MESSAGE", new string[] { "Path couldn't be found!" })); }
            };

            DLG_Base.PushNewDialog(new DLG_Inputs("Choose where to export the file at", new string[] { "Path" }, new bool[] { false }, toDo));
        }

        private void ShowResetAccountQuestion()
        {
            DLG_YesNo dialog = new DLG_YesNo("Are you sure you want to RESET your ACCOUNT?",
                delegate
                {
                    PersistentSettings settings = PersistentSettings.Load();
                    settings.UserSettings.Reset();
                    settings.Save();

                    DLG_Base.PushNewDialog(new DLG_Message("MESSAGE", new string[] { "Account has been reset" }));
                });

            DLG_Base.PushNewDialog(dialog);
        }

        private void StartProcess(string processPath)
        {
            try { Process.Start(processPath); }
            catch { Printer.Warning($"Failed to start process {processPath}"); }
        }
    }
}
