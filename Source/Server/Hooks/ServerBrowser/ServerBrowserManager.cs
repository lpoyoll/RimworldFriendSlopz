using GameServer.Core;
using GameServer.Hooks.TCPNetwork;
using Shared;
using Shared.Files.Configs.Mods;
using Shared.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TCPNetwork;
using TCPNetwork.Files.Client;
using TCPNetwork.Packets.ServerBrowser;
using static Shared.Misc.Printer;

namespace GameServer.Hooks.ServerBrowser
{
    public static class ServerBrowserManager
    {
        private static string ServerIPV4 { get; set; } = string.Empty;

        private static bool WasStartedOnce { get; set; } = false;

        public enum BrowserMode { Normal, Lite }

        private static Action<PacketHeader, byte[], ServerClient> OnReadPacket { get; set; } = delegate (PacketHeader header, byte[] buffer, ServerClient client)
        {
            MethodInfo method = (MethodInfo)MethodGatherer.ServerMethodDictionary[header][1];
            method.Invoke(MethodGatherer.ServerMethodDictionary[header][0], new object[] { client, buffer, header });
        };

        private static Action<ServerClient> OnDisconnect { get; set; } = delegate
        {
            Network.BrowserEndpoint = null;
            StartFeature();
        };

        public static void StartFeature()
        {
            if (Network.BrowserEndpoint != null) Printer.Error("Server was already connected to browser");
            else if (!Master.ServerConfig.EnableServerTelemetry) return;
            else
            {
                while (Network.BrowserEndpoint == null)
                {
                    if (!Master.ServerConfig.EnableServerBrowser) ConnectToServerBrowser(BrowserMode.Lite);
                    else ConnectToServerBrowser(BrowserMode.Normal);
                }

                if (Master.ServerConfig.EnableServerBrowser)
                {
                    if (!WasStartedOnce)
                    {
                        Printer.Title(Printer.SeparatorString);
                        Printer.Warning("Server discovery is ENABLED");
                        Printer.Warning("The server details are currently being transmitted to the public browser");
                        Printer.Title(Printer.SeparatorString);
                    }
                }

                else
                {
                    Printer.Title(Printer.SeparatorString);
                    Printer.Warning("Server discovery is DISABLED");
                    Printer.Warning("Please turn the service ON in the settings if you want your server listed publicly");
                    Printer.Title(Printer.SeparatorString);
                }
            }
        }

        private static bool ConnectToServerBrowser(BrowserMode mode)
        {
            try
            {
                ServerClient client = new ServerClient(new TcpClient(Network.BrowserIp, Network.BrowserPort), new NetworkRuleset(null, OnDisconnect, OnReadPacket, null, false));
                Network.BrowserEndpoint = client.Listener;
                SetupConnection(mode);
                return true;
            }

            catch (Exception ex) 
            { 
                Printer.Error(ex, LogImportanceMode.Ludicrous);
                return false;
            }
        }

        private static async void SetupConnection(BrowserMode mode)
        {
            if (!WasStartedOnce) ServerIPV4 = await GetPublicIP();

            PKT_ServerTelemetry telemetry = new PKT_ServerTelemetry();
            telemetry.Name = Master.ServerConfig.Name;
            telemetry.Description = Master.ServerConfig.Description;
            telemetry.Version = CommonValues.ExecutableVersion;
            telemetry.Endpoint = ServerIPV4;
            telemetry.Port = Master.ServerConfig.Port;
            telemetry.IsPrivate = mode == BrowserMode.Lite;
            telemetry.MaxPopulation = Master.ServerConfig.MaxPlayers;
            telemetry.Mods = Master.ModConfig.ModConfigs.Where(fetch => fetch.Type != ModConfigFile.ModType.Forbidden)
                .OrderBy(fetch => fetch.FileName).ToList();

            if (!WasStartedOnce) SendTelemetry(telemetry);
        }

        private static void SendTelemetry(PKT_ServerTelemetry telemetry)
        {
            WasStartedOnce = true;

            while (true)
            {
                Thread.Sleep(1);

                if (Network.BrowserEndpoint == null) continue;
                else
                {
                    telemetry.CurrentPopulation = ServerNetwork.GetConnectedClients().Length;
                    Network.BrowserEndpoint.EnqueuePacket(PacketHeader.ServerBrowserTelemetry, telemetry);
                    Thread.Sleep(Network.BrowserTelemetryInterval);
                }
            }
        }

        public static async Task<string> GetPublicIP()
        {
            using (HttpClient client = new HttpClient())
            {
                string address = await client.GetStringAsync("https://api.ipify.org");
                return address;
            }
        }
    }
}
