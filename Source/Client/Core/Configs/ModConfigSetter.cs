using GameClient.Dialogs;
using GameClient.Dialogs.Default;
using GameClient.Files;
using GameClient.PacketManagers;
using RTShared.Misc;
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

        public ModConfigSetter(ModContentPack content) : base(content) { ModConfigs = GetSettings<ModConfigGetter>(); }

        public override string SettingsCategory() { return "RimWorld Together"; }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            listingStandard.Label("Debugging");
            if (listingStandard.ButtonTextLabeled("Verbosity mode", $"{ModConfigGetter.CurrentVerboseMode}")) ShowVerbosityMenu();

            listingStandard.GapLine();
            listingStandard.Label("Tweaks");
            if (listingStandard.ButtonTextLabeled("Change mod version [Windows only]", "Change")) { PM_Version.PromptChangeVersion(); }

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

            List<Tuple<string, Printer.Verbosity>> modes = new List<Tuple<string, Printer.Verbosity>>()
            {
                Tuple.Create("None", Printer.Verbosity.Normal),
                Tuple.Create("Verbose", Printer.Verbosity.Verbose),
                Tuple.Create("Extreme", Printer.Verbosity.Extreme),
            };

            foreach (Tuple<string, Printer.Verbosity> tuple in modes)
            {
                FloatMenuOption item = new FloatMenuOption(tuple.Item1, delegate
                {
                    ModConfigGetter.CurrentVerboseMode = tuple.Item2;
                });

                list.Add(item);
            }

            Find.WindowStack.Add(new FloatMenu(list));
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
    }
}
