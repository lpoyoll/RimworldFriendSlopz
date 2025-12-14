using System.Net;
using System.Text;
using GameServer.Core;
using GameServer.Misc;
using Rimworld_Together_Master_Server.Data;
using Shared;
using static Shared.CommonEnumerators;
using TCPNetwork.Packets;
using Shared.Files.Configs;

namespace GameServer.Managers
{
    public static class ServerBrowserManager
    {
        private const string MasterServer = "https://rimworldtogether.eragon.dev";

        private const string GetPublicIpAddressURL = "https://api.ipify.org";
        
        private const int MaxDescriptionLength = 200;

        private const int MaxNameLength = 40;

        private const int DelayBetweenRequest = 520000;

        private const int DelayBetweenErrors = 18000000;

        private static HttpClientHandler handler = new HttpClientHandler() { UseProxy = false };

        private static HttpClient Client = new HttpClient(handler) { DefaultRequestVersion = HttpVersion.Version11 };

        public static void StartFeature()
        {
            if (Master.ServerBrowserConfig.EnableServerBrowser)
            {
                if (ValidateServerInformation())
                {
                    Printer.Warning("Server discovery is ENABLED");
                    Printer.Warning("The server details are currently being transmitted to the public browser");

                    Task.Run(async () =>
                    {
                        while (true)
                        {
                            bool result = await SendServerInformation();
                            if (result) await Task.Delay(DelayBetweenRequest);
                            else await Task.Delay(DelayBetweenErrors);
                        }
                    });
                }
            }

            else
            {
                Printer.Warning("Server discovery is DISABLED");
                Printer.Warning("Please turn the service ON in the settings if you want your server listed publicly");
                Printer.Title($"----------------------------------------");

                if (Master.ServerBrowserConfig.EnableServerTelemetry)
                {
                    Task.Run(async () =>
                    {
                        while (true)
                        {
                            bool result = await SendServerPlayerCount();

                            if (result) await Task.Delay(DelayBetweenRequest);
                            else await Task.Delay(DelayBetweenErrors);
                        }
                    });
                }

                else
                {
                    Printer.Warning("Server telemetry is DISABLED");
                    Printer.Warning("No diagnostics details will be send to the master server");
                    Printer.Warning("Please consider ENABLING this feature! It helps the development of the mod!");
                    Printer.Title($"----------------------------------------");
                }
            }
        }

        private static bool ValidateServerInformation() 
        {
            ServerConfigFile serverInfo = Master.ServerConfig;
            ServerBrowserConfigFile serverBrowserInfo = Master.ServerBrowserConfig;

            if (serverInfo.Description.Length > MaxDescriptionLength) 
            {
                Printer.Error($"Server description is above {MaxDescriptionLength} characters, please shorten it. Server browser features have been turned off.");
                return false;
            }

            if (!IPAddress.TryParse(serverBrowserInfo.PublicEndPoint, out _))
            {
                serverBrowserInfo.PublicEndPoint = "";
                serverBrowserInfo.Save();
                Printer.Error($"Public endpoint \"{serverBrowserInfo.PublicEndPoint}\" is not a valid ip address. Server browser features have been turned off and faulty entry has been removed.");
            }
            
            if (string.IsNullOrEmpty(serverBrowserInfo.PublicEndPoint))
            {
                if(!GetPublicIpAddressAsync().Result)
                    Printer.Error($"Public endpoint is empty. Please set your public ip address or domain. Server browser features have been turned off.");
                return false;
            }
            
            if (serverInfo.Name.Length > MaxNameLength)
            {
                Printer.Error($"Server name is above {MaxNameLength} characters, please shorten it. Server browser features have been turned off.");
                return false;
            }

            if (serverInfo.Name == "RimWorld-Together-Server") 
            {
                Printer.Error($"Server name is the default name of {serverInfo.Name}. Please change the server name to something unique!. Server browser features have been turned off.");
                return false;
            }

            return true;
        }

        private static async Task<bool> GetPublicIpAddressAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

                var ip = await Client.GetStringAsync(GetPublicIpAddressURL, cts.Token);

                ip = ip.Trim();

                if (!IPAddress.TryParse(ip, out _))
                    return false;

                Master.ServerBrowserConfig.PublicEndPoint = ip;
                Master.ServerBrowserConfig.Save();
                Printer.Warning($"Public endpoint was empty for the server browser, but the server managed to automatically fetch the ip {ip}. If this is not the correct ip, make sure to change it in the config file!");
                return true;
            }
            catch (Exception ex)
            {
                Printer.Warning($"Failed to automatically resolve public IP address: {ex.Message}", LogImportanceMode.Verbose);
                return false;
            }
        }
        
        private static async Task<bool> SendServerInformation()
        {
            try
            {
                Client.DefaultRequestHeaders.Clear();
                Client.DefaultRequestHeaders.Add("action", "Add-Server-Browser");
                ServerInfo info = new ServerInfo()
                {
                    _ip = Master.ServerBrowserConfig.PublicEndPoint,
                    _port = int.Parse(Master.ServerConfig.Port),
                    _name = Master.ServerConfig.Name,
                    _description = Master.ServerConfig.Description,
                    _maximumPlayerCount = int.Parse(Master.ServerConfig.MaxPlayers),
                    _currentPlayerCount = ServerNetwork.Instance.ServerClients.Count,
                    _version = CommonValues.ExecutableVersion,
                    _config = Master.ModConfig
                };

                HttpResponseMessage response = await Client.PostAsync(MasterServer, 
                    new StringContent(Serializer.SerializeToString(info), Encoding.UTF8, "application/json"));

                response.EnsureSuccessStatusCode();
                return true;
            }

            catch (Exception ex)
            {
                Printer.Error($"Error while notifying the Master Server\n {ex}");
                Printer.Error($"Will retry in 30 minutes");
                return false;
            }
        }

        private static async Task<bool> SendServerPlayerCount() 
        {
            try
            {
                Client.DefaultRequestHeaders.Clear();
                Client.DefaultRequestHeaders.Add("action", "Player-Count");

                HttpResponseMessage response = await Client.PostAsync(MasterServer,
                    new StringContent(ServerNetwork.Instance.ServerClients.Count.ToString()));

                response.EnsureSuccessStatusCode();
                return true;
            }

            catch(Exception ex)
            {
                Printer.Error(ex, LogImportanceMode.Verbose);
                return false;
            }
        }
    }
}
