using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using GameClient.Managers;
using GameClient.Misc;
using GameClient.TCP;
using HarmonyLib;
using RimWorld;
using Shared.MasterServer;
using UnityEngine;
using Verse;
using static Mono.Security.X509.X520;

namespace GameClient.Dialogs
{
    public class RT_Dialog_ServerListingInfo : Window
    {
        private static FieldInfo ModsConfigdata;
        private static FieldInfo ModsConfigDataactiveMods;

        private Vector2 initialSize = new Vector2(600f, 250f);
        public override Vector2 InitialSize => initialSize;
        private ServerInfo info;
        public RT_Dialog_ServerListingInfo(ServerInfo info) 
        {
            this.info = info;
            ModsConfigdata = AccessTools.Field(typeof(ModsConfig), "data");
            ModsConfigDataactiveMods = AccessTools.Field(AccessTools.TypeByName("Verse.ModsConfig.ModsConfigData"), "activeMods");
        }
        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Vector2 titleSize = Text.CalcSize(info._name);
            float centeredx = inRect.width / 2;
            Rect titleRect = new Rect(centeredx - titleSize.x / 2, inRect.y, titleSize.x, titleSize.y);
            Widgets.Label(titleRect, info._name);

            Widgets.DrawLineHorizontal(0, titleSize.y + 3f, inRect.width);

            Text.Font = GameFont.Small;
            Rect descriptionRect = new Rect(inRect.x, titleSize.y + 6f, inRect.width / 3 * 2, inRect.height - 55f);
            Widgets.Label(descriptionRect, info._description);

            Rect connectRect = new Rect(inRect.width - 110f, inRect.height - 55f, 100f, 45f);

            if (Widgets.ButtonText(connectRect, "Connect"))
            {
                Dictionary<string, ulong> downloadableMods = new Dictionary<string, ulong>();
                List<string> missingMods = new List<string>();
                Dictionary<string, string> display = new Dictionary<string, string>();
                for (int i = 0; i < info._config.UnsortedMods.Length; i++)
                {
                    string mod = info._config.UnsortedMods[i];
                    if (ModLister.AllInstalledMods.Any(x => x.PackageId == mod))
                        continue;

                    if (info._config.AllModIds[i] != 0)
                    {
                        downloadableMods.Add(mod, info._config.AllModIds[i]);
                        display.Add($"Can be downloaded", mod);
                    }
                    else
                    {
                        missingMods.Add(mod);
                        display.Add($"Cannot be downloaded", mod);
                    }
                }
                if (missingMods.Any() || downloadableMods.Any()) {
                    DialogManager.PushNewDialog(new RT_Dialog_ListingWithTuple("Missing mods!",
                        "Do you want to download the missing mods automatically?",
                        display.Keys.ToArray(),
                        display.Values.ToArray(),
                        () =>
                        {
                            foreach (var value in downloadableMods)
                            {
                                if (ServerBrowserManager.DownloadMod(value.Value))
                                {
                                    try
                                    {
                                        downloadableMods.Remove(value.Key);
                                        List<string> activeMods = (List<string>)ModsConfigDataactiveMods.GetValue(ModsConfigdata.GetValue(null));
                                    }
                                    catch (Exception ex)
                                    {
                                        Printer.Error($"Error while trying to activate mod {value.Key}. You will need to manually enable / download this mod.");
                                    }
                                }
                            }
                            if (downloadableMods.Count + missingMods.Count > 0)
                            {
                                List<string> modsToReport = new List<string>();
                                modsToReport.AddRange(downloadableMods.Keys);
                                modsToReport.AddRange(missingMods);
                                DialogManager.PushNewDialog(new RT_Dialog_Listing("Error",
                                    "Something went wrong while downloading the following mods:",
                                    modsToReport.ToArray()));
                            }
                            else
                            {
                                DialogManager.PushNewDialog(new RT_Dialog_Message("Success.", new string[] { "Please restart your game." }));
                            }
                        }
                    ));
                } else 
                {
                    Network.ip = info._ip;
                    Network.port = info._port.ToString();
                    DialogManager.PushNewDialog(new RT_Dialog_Wait("Trying to connect to server"));
                    Threader.GenerateThread(Threader.Mode.Start);
                    Close();
                }
            }
        }
    }
}
