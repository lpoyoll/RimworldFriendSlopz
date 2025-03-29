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
        public static void StartLoops()
        {
            Task.Run(async () =>
            {
                if(Master.serverConfig.EnableServerBrowser) 
                {
                    while (true)
                    {
                        bool result = await SendServerInfo();
                        if (result)
                        {
                            await Task.Delay(5000); //Temporary testing timer, should be 5 minutes, aka 520000 miliseconds
                        }
                        else
                        {
                            await Task.Delay(1800000);
                        }
                    }
                }
                else 
                {
                    while (true)
                    {
                        bool result = await SendServerPlayerCount();
                        if (result)
                        {
                            await Task.Delay(5000); //Temporary testing timer, should be 5 minutes, aka 520000 miliseconds
                        }
                        else
                        {
                            await Task.Delay(1800000);
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
                    _runningModsByNameRequired = Master.modConfig.RequiredMods,
                    _runningModsByNameOptional = Master.modConfig.OptionalMods,
                    _runningModsByNameForbidden = Master.modConfig.ForbiddenMods
                };
                HttpResponseMessage response = await Client.PostAsync(MasterServer, 
                    new StringContent(Serializer.SerializeToString(info), Encoding.UTF8, "application/json"));

                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (Exception ex)
            {
                Printer.Warning($"Error while notifying the Master Server\n {ex}");
                Printer.Warning($"Will retry in 30 minutes");
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
            string publicIp = await Client.GetStringAsync($"https://api.ipify.org"); //Gets the public ip adress of this server.
            ServerInfo info = new ServerInfo()
            {
                _ip = publicIp,
                _port = int.Parse(Master.serverConfig.Port),
                _name = Master.serverConfig.Name,
                _description = Master.serverConfig.Description,
                _maximumPlayerCount = int.Parse(Master.serverConfig.MaxPlayers),
                _currentPlayerCount = Network.connectedClients.Count,
                _runningModsByNameRequired = Master.modConfig.RequiredMods,
                _runningModsByNameOptional = Master.modConfig.OptionalMods,
                _runningModsByNameForbidden = Master.modConfig.ForbiddenMods
            };
            HttpResponseMessage response = await Client.PostAsync(MasterServer,
                new StringContent(Serializer.SerializeToString(info), Encoding.UTF8, "application/json"));
        }
    }
}
