using GameServer.Core;
using GameServer.Hooks.TCPNetwork;
using RTShared.Files.Configs;
using RTNetwork.PacketManagers;
using RTNetwork.Packets.ServerBrowser;
using static GameServer.Hooks.ServerBrowser.ServerBrowserManager;
using RTNetwork.Components;
using RTShared.Misc;

namespace GameServer.PacketManagers.ServerBrowser
{
    public class PM_Telemetry : PM_Base
    {
        [HandlesPacket(PacketHeader.ServerBrowserTelemetry)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            throw new NotImplementedException();
        }

        public static async void Send(BrowserMode mode)
        {
            if (!WasStartedOnce) ServerIPV4 = await GetPublicIP();

            PKT_ServerTelemetry telemetry = new PKT_ServerTelemetry();
            telemetry.Name = Master.ServerConfig.Name;
            telemetry.Description = Master.ServerConfig.Description;
            telemetry.DiscordURL = Master.ServerConfig.DiscordURL;
            telemetry.SteamWorkshopURL = Master.ServerConfig.SteamWorkshopURL;
            telemetry.Version = CommonValues.ExecutableVersion;
            telemetry.Endpoint = ServerIPV4;
            telemetry.Port = Master.ServerConfig.Port;
            telemetry.IsPrivate = mode == BrowserMode.Private;
            telemetry.CurrentPopulation = ServerNetwork.GetConnectedClients().Length;
            telemetry.MaxPopulation = Master.ServerConfig.MaxPlayers;
            telemetry.Mods = Master.ModConfig.ModConfigs.Where(fetch => fetch.Type != FL_ModConfig.ModType.Forbidden)
                .OrderBy(fetch => fetch.FileName).ToList();

            Network.MultipurposeEndpoint?.EnqueuePacket(PacketHeader.ServerBrowserTelemetry, telemetry);
        }
    }
}
