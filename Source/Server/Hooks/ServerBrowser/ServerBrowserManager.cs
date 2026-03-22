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

        public enum BrowserMode { Normal, Lite }

        private static Action<PacketHeader, byte[], ServerClient> OnReadPacket { get; set; } = delegate (PacketHeader header, byte[] buffer, ServerClient client)
        {
            MethodInfo method = (MethodInfo)MethodGatherer.ServerMethodDictionary[header][1];
            method.Invoke(MethodGatherer.ServerMethodDictionary[header][0], new object[] { client, buffer, header });
        };

        public static void StartFeature()
        {
            Printer.Title(Printer.SeparatorString);

            if (!Master.ServerBrowserConfig.EnableServerBrowser)
            {
                ConnectToServerBrowser(BrowserMode.Lite);
                Printer.Warning("Server discovery is DISABLED");
                Printer.Warning("Please turn the service ON in the settings if you want your server listed publicly");
            }

            else
            {
                if (ConnectToServerBrowser(BrowserMode.Normal))
                {
                    Printer.Warning("Server discovery is ENABLED");
                    Printer.Warning("The server details are currently being transmitted to the public browser");
                }

                else
                {
                    Printer.Warning("Server discovery is currently unavailable");
                    Printer.Warning("Your server won't be listed publicly");
                }
            }

            Printer.Title(Printer.SeparatorString);
        }

        private static bool ConnectToServerBrowser(BrowserMode mode)
        {
            try
            {
                ServerClient client = new ServerClient(new TcpClient(Network.BrowserIp, Network.BrowserPort), new NetworkRuleset(null, null, OnReadPacket, null));
                Network.BrowserEndpoint = client.Listener;
                SetupConnection(mode);
                return true;
            }

            catch (Exception ex) 
            { 
                Printer.Error(ex, LogImportanceMode.Extreme);
                return false;
            }
        }

        private static async void SetupConnection(BrowserMode mode)
        {
            ServerIPV4 = await GetPublicIP();

            PKT_BrowserTelemetry telemetry = new PKT_BrowserTelemetry();
            telemetry.Name = Master.ServerConfig.Name;
            telemetry.Description = Master.ServerConfig.Description;
            telemetry.Endpoint = ServerIPV4;
            telemetry.Port = Master.ServerConfig.Port;
            telemetry.Mods = Master.ModConfig.ModConfigs;
            telemetry.IsPrivate = mode == BrowserMode.Lite;
            telemetry.Hash = Hasher.GetHashFromString($"{telemetry.Endpoint}:{telemetry.Port}");

            SendTelemetry(telemetry);
        }

        private static void SendTelemetry(PKT_BrowserTelemetry telemetry)
        {
            while (true)
            {
                telemetry.Population = ServerNetwork.GetConnectedClients().Length;
                Network.BrowserEndpoint.EnqueuePacket(PacketHeader.ServerBrowserTelemetry, telemetry);
                Thread.Sleep(Network.BrowserTelemetryInterval);
            }
        }

        private static async Task<string> GetPublicIP()
        {
            using (HttpClient client = new HttpClient())
            {
                string address = await client.GetStringAsync("https://api.ipify.org");
                return address;
            }
        }
    }
}
