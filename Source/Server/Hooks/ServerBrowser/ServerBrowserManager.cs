using RTServer.Core;
using RTServer.PacketManagers.ServerBrowser;
using RTShared.Misc;
using System.Net.Sockets;
using System.Reflection;
using RTNetwork.PacketManagers;
using static RTShared.Misc.Printer;
using RTNetwork.Components;

namespace RTServer.Hooks.ServerBrowser
{
    public static class ServerBrowserManager
    {
        public static string ServerIPV4 { get; set; } = string.Empty;

        public static bool WasStartedOnce { get; set; } = false;

        public enum BrowserMode { Public, Private }

        private static Action<PacketHeader, byte[], ServerClient> OnReadPacket { get; set; } = delegate (PacketHeader header, byte[] buffer, ServerClient client)
        {
            MethodInfo method = (MethodInfo)PM_Base.PacketDictionary[header][1];
            method.Invoke(PM_Base.PacketDictionary[header][0], new object[] { client, buffer, header });
        };

        private static Action<ServerClient> OnDisconnect { get; set; } = delegate
        {
            Task.Run(delegate
            {
                Network.MultipurposeEndpoint = null;
                Thread.Sleep(Network.BrowserTelemetryInterval);
                StartFeature();
            });
        };

        public static void StartFeature()
        {
            if (Network.MultipurposeEndpoint != null) Printer.Error("Server was already connected to browser");
            else if (!Master.ServerConfig.EnableServerTelemetry) return;
            else
            {
                while (Network.MultipurposeEndpoint == null)
                {
                    if (!Master.ServerConfig.EnableServerBrowser) ConnectToServerBrowser(BrowserMode.Private);
                    else ConnectToServerBrowser(BrowserMode.Public);
                }

                if (Master.ServerConfig.EnableServerBrowser)
                {
                    if (!WasStartedOnce)
                    {
                        WasStartedOnce = true;

                        Printer.Title(Printer.SeparatorString);
                        Printer.Warning("Server discovery is ENABLED");
                        Printer.Warning("The server details are currently being transmitted to the public browser");
                        Printer.Title(Printer.SeparatorString);
                    }
                }

                else
                {
                    if (!WasStartedOnce)
                    {
                        WasStartedOnce = true;

                        Printer.Title(Printer.SeparatorString);
                        Printer.Warning("Server discovery is DISABLED");
                        Printer.Warning("Please turn the service ON in the settings if you want your server listed publicly");
                        Printer.Title(Printer.SeparatorString);
                    }
                }
            }
        }

        private static bool ConnectToServerBrowser(BrowserMode mode)
        {
            try
            {
                ServerClient client = new ServerClient(new TcpClient(Network.MultipurposeIP, Network.BrowserServerPort), new NetworkRuleset(null, OnDisconnect, OnReadPacket, null, false));
                Network.MultipurposeEndpoint = client.Listener;
                PM_Handshake.Send(client);
                PM_Telemetry.Send(mode);
                return true;
            }

            catch (Exception ex) 
            { 
                Printer.Error(ex, Verbosity.Extreme);
                return false;
            }
        }

        public static async Task<string> GetPublicIP()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string address = await client.GetStringAsync("https://api.ipify.org");
                    return address;
                }
            }

            catch (Exception ex)
            {
                Printer.Error(ex, Verbosity.Extreme);
                return string.Empty;
            }
        }
    }
}
