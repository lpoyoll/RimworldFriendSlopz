using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameServer.Core;
using GameServer.Misc;
using GameServer.TCP;
using Shared;
using Shared.MasterServer;

namespace GameServer.Managers
{
    [RTManager]
    public static class ServerBrowserManager
    {
        private const string MasterServer = "https://rimworldtogether.eragon.dev";
        private static HttpClient Client = new HttpClient();
        private const int DelayBetweenRequest = 5000; //Temporary testing timer, should be 5 minutes, aka 520000 miliseconds
        private const int DelayBetweenErrors = 18000000;
        public static void StartLoops()
        {
            Task.Run(async () =>
            {
                if(Master.serverConfig.EnableServerBrowser) 
                {
                    Printer.Warning($"You have enabled the server browser feature. By doing so, you understand that:" +
                        $"\n- Your server's information (name, description, player count, ect... will be shared to possibly all Rimworld Together's users." +
                        $"\n- Your server's contact information (public ip adress and port) will be shared to possibly all Rimworld Together's users." +
                        $"\n If you do not want to share this information, you can disable the server browser in:\n{Path.Combine(Master.configsPath, "ServerConfig.json")} "+ 
                        "\nunder the `EnableServerBrowser` setting and then restart the server.");
                    while (true)
                    {
                        bool result = await SendServerInfo();
                        if (result)
                        {
                            await Task.Delay(DelayBetweenRequest); 
                        }
                        else
                        {
                            await Task.Delay(DelayBetweenErrors);
                        }
                    }
                }
                else 
                {
                    Printer.Warning($"The server browser is currently disabled. If you want to advertise your server to all Rimworld Together's users, you can turn on the server browser");
                    while (true)
                    {
                        bool result = await SendServerPlayerCount();
                        if (result)
                        {
                            await Task.Delay(DelayBetweenRequest);
                        }
                        else
                        {
                            await Task.Delay(DelayBetweenErrors);
                        }
                    }
                }
            });
            Console.CancelKeyPress += SendClosureSignalFromConsole;
            AppDomain.CurrentDomain.ProcessExit += SendClosureSignalFromApplicationShutdown;
        }

        private static async Task<bool> SendServerInfo()
        {
            try
            {
                Client.DefaultRequestHeaders.Clear();
                Client.DefaultRequestHeaders.Add("action", "Add-Server-Browser");
                ServerInfo info = new ServerInfo()
                {
                    _port = int.Parse(Master.serverConfig.Port),
                    _name = Master.serverConfig.Name,
                    _description = Master.serverConfig.Description,
                    _maximumPlayerCount = int.Parse(Master.serverConfig.MaxPlayers),
                    _currentPlayerCount = Network.connectedClients.Count,
                    _config = Master.modConfig
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
                    new StringContent(Network.connectedClients.Count.ToString()));

                response.EnsureSuccessStatusCode();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void SendClosureSignalFromConsole(object sender, ConsoleCancelEventArgs e) 
        {
            e.Cancel = true;
            SendClosureSignal().Wait();
        }
        private static void SendClosureSignalFromApplicationShutdown(object? sender, EventArgs e)
        {
            SendClosureSignal().Wait();
        }
        public static async Task SendClosureSignal() 
        {
            Client.DefaultRequestHeaders.Clear();
            Client.DefaultRequestHeaders.Add("action", "Remove-Server-Browser");
            HttpResponseMessage response = await Client.PostAsync(MasterServer,
                new StringContent(""));
        }
    }
}
