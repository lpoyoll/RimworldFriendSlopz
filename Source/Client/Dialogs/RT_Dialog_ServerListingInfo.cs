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
using Shared;
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
                var downloadableMods = new Dictionary<string, ulong>();
                var missingMods = new List<string>();
                var display = new List<string>();
                var modsToEnable = new List<string>();
                for (int i = 0; i < info._config.UnsortedMods.Length; i++)
                {
                    string mod = info._config.UnsortedMods[i];
                    var foundMod = ModLister.AllInstalledMods.Where(x => x.PackageId == mod).FirstOrDefault();
                    if (foundMod != null) 
                    {
                        if (!foundMod.Active)
                        {
                            display.Add($"[Enableable]> {foundMod.Name}");
                            modsToEnable.Add(mod);
                        }
                        continue;
                    }

                    if (info._config.AllModIds[i] != 0)
                    {
                        downloadableMods.Add(mod, info._config.AllModIds[i]);
                        modsToEnable.Add(mod);
                        display.Add($"[Downloadable]> {mod}");
                    }
                    else
                    {
                        missingMods.Add(mod);
                        modsToEnable.Add(mod);
                        display.Add($"[Unavailable]> {mod}");
                    }
                }
                if (missingMods.Any() || downloadableMods.Any() || modsToEnable.Any()) {
                    Printer.Warning($"Found {downloadableMods.Count} mods to download and {missingMods} mods that cannot be downloaded.");
                    RT_Dialog_Base.PushNewDialog(new RT_Dialog_Listing("Missing mods!",
                        "Do you want to download/enable the missing mods automatically?",
                        display.ToArray(),
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
                            if (downloadableMods.Count > 0)
                            {
                                List<string> modsToReport = new List<string>();
                                modsToReport.AddRange(downloadableMods.Keys);
                                RT_Dialog_Base.PushNewDialog(new RT_Dialog_Listing("Error",
                                    "Something went wrong while downloading the following mods:",
                                    modsToReport.ToArray()));
                            }
                            else
                            {
                                List<string> modsToEnable = new List<string>();
                                modsToEnable.AddRange(info._config.RequiredMods);
                                modsToEnable.AddRange(info._config.OptionalMods);
                                foreach (string str in modsToEnable)
                                {
                                    if(!info._config.ForbiddenMods.Contains(str) && !missingMods.Contains(str))
                                        ModsConfig.SetActive(str, true);
                                }
                                ModsConfig.TrySortMods();
                                ModsConfig.Save();
                                RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("Success.", new string[] { "Game will now restart, give some time for steam to download the mods." }, 
                                () => 
                                {
                                    GenCommandLine.Restart();
                                }
                                ));
                            }
                        }
                    ));
                } else 
                {
                    Network.ip = info._ip;
                    Network.port = info._port.ToString();
                    RT_Dialog_Base.PushNewDialog(new RT_Dialog_Wait("Trying to connect to server"));
                    Threader.GenerateThread(Threader.Mode.Start);
                    Close();
                }
            }
        }
    }
}
