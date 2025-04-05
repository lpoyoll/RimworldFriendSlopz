using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GameClient.Misc;
using HarmonyLib;
using RimWorld;
using Shared;
using Shared.MasterServer;
using Steamworks;
using Verse;

namespace GameClient.Managers
{
    [RTManager]
    public class ServerBrowserManager
    {
        private static WebClient client = new WebClient(); //For some reason Rimworld doesn't have HttpClient #fuck my life
        private const string MasterServer = "https://rimworldtogether.eragon.dev";
        public static ServerInfo[] GetAllServersAvailable()
        {
            try
            {
                client.Headers.Clear();
                client.Headers.Add("action", "Server-Infos");
                string response = client.DownloadString(MasterServer);
                if (string.IsNullOrEmpty(response))
                {
                    Printer.Warning($"response was null");
                }
                return Serializer.SerializeFromString<AllServersPacket>(response)._serverInfos;
            }
            catch (Exception ex)
            {
                Printer.Error($"Error while trying to fetch info from the server browser.\n{ex}");
                return null;
            }
        }

        public static bool DownloadMod(ulong steamId)
        {
            try
            {
                SteamUGC.SubscribeItem(new PublishedFileId_t(steamId));
                return true;
            }
            catch 
            {
                return false;
            }
        }
    }
}
