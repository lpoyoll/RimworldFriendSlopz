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

namespace GameServer.Hooks.ServerBrowser
{
    public static class ServerBrowserManager
    {
        private static Action<PacketHeader, byte[], ServerClient> OnReadPacket { get; set; } = delegate (PacketHeader header, byte[] buffer, ServerClient client)
        {
            MethodInfo method = (MethodInfo)MethodGatherer.ServerMethodDictionary[header][1];
            method.Invoke(MethodGatherer.ServerMethodDictionary[header][0], new object[] { client, buffer, header });
        };

        public static void StartFeature()
        {
            Printer.Title(Printer.SeparatorString);

            if (!Master.ServerBrowserConfig.EnableServerBrowser && !Master.ServerBrowserConfig.EnableServerTelemetry)
            {
                Printer.Warning("Server discovery & telemetry are DISABLED");
                Printer.Warning("Please turn the service ON in the settings if you want your server listed publicly");
            }

            else
            {
                if (Master.ServerBrowserConfig.EnableServerBrowser)
                {
                    if (ConnectToServerBrowser())
                    {
                        Printer.Warning("Server discovery is ENABLED");
                        Printer.Warning("The server details are currently being transmitted to the public browser");
                    }

                    else
                    {
                        Printer.Warning("Server discovery is failed to initialize");
                        Printer.Warning("Your server won't be listed publicly");
                    }
                }

                else
                {
                    Printer.Warning("Server discovery is DISABLED");
                    Printer.Warning("Please turn the service ON in the settings if you want your server listed publicly");
                }
            }

            Printer.Title(Printer.SeparatorString);
        }

        private static bool ConnectToServerBrowser()
        {
            try
            {
                ServerClient client = new ServerClient(new TcpClient("127.0.0.1", 7777), new NetworkRuleset(null, null, OnReadPacket, null));
                Network.BrowserEndpoint = client.Listener;
                Task.Run(delegate { SendTelemetry(); });
                return true;
            }

            catch (Exception ex) 
            { 
                Printer.Error(ex);
                return false;
            }
        }

        private static void SendTelemetry()
        {
            while (true)
            {
                PKT_BrowserTelemetry telemetry = new PKT_BrowserTelemetry();
                telemetry.Name = Master.ServerConfig.Name;
                telemetry.Description = Master.ServerConfig.Description;
                telemetry.Endpoint = null;
                telemetry.Port = Master.ServerConfig.Port;
                telemetry.Population = ServerNetwork.GetConnectedClients().Length;
                telemetry.Hash = Hasher.GetHashFromString($"{telemetry.Endpoint}:{telemetry.Port}");
                telemetry.Mods = Master.ModConfig.ModConfigs;

                Network.BrowserEndpoint.EnqueuePacket(PacketHeader.ServerBrowserTelemetry, telemetry);
                Thread.Sleep(Network.BrowserTelemetryInterval);
            }
        }
    }
}
