using System.Net;
using System.Text;
using GameServer.Core;
using GameServer.Misc;
using Shared;
using static Shared.CommonEnumerators;
using TCPNetwork.Packets;
using Shared.Files.Configs;

namespace GameServer.Managers
{
    public static class ServerBrowserManager
    {
        private const string MasterServer = "https://rimworldtogether.eragon.dev";

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

            if (string.IsNullOrEmpty(serverBrowserInfo.PublicEndPoint)) 
            {
                Printer.Error($"Public endpoint is empty. Please set your public ip adress or domain. Server browser features have been turned off.");
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
