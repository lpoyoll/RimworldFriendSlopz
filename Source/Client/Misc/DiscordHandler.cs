using DiscordRPC;
using DiscordRPC.Logging;
using Shared;
using Shared.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameClient.Misc
{
    public static class DiscordHandler
    {
        private static DiscordRpcClient RPClient { get; set; } = null;

        private static readonly string PresenceID = "1505021874868981854";

        [OnSessionStart]
        private static void StartPresence()
        {
            RPClient = new DiscordRpcClient(PresenceID);
            RPClient.Logger = new ConsoleLogger() { Level = LogLevel.Warning };
            RPClient.Initialize();

            RPClient.SetPresence(new RichPresence()
            {
                Details = "Playing Multiplayer",
                Timestamps = Timestamps.Now,
                Buttons = new Button[]
                {
                    new Button()
                    {
                        Label = "Download",
                        Url = "https://steamcommunity.com/sharedfiles/filedetails/?id=3005289691"
                    }
                }
            });

            Printer.Message("Discord Rich Presence has been started", Printer.Verbosity.Verbose);
        }

        [OnSessionEnd]
        private static void StopPresence()
        {
            RPClient?.Dispose();
            Printer.Message("Discord Rich Presence has been stopped", Printer.Verbosity.Verbose);
        }
    }
}
