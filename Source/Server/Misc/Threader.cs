using GameServer.Managers;
using Shared.Network.Server;

namespace GameServer.Misc
{
    public static class Threader
    {
        public enum ServerMode { Start, Sites, Console, Backup }

        public static Task GenerateServerThread(ServerMode mode)
        {
            return mode switch
            {
                ServerMode.Start => Task.Run(Network.ReadyServer),
                ServerMode.Sites => Task.Run(SiteManager.StartSiteTicker),
                ServerMode.Console => Task.Run(ConsoleManager.ListenForServerCommands),
                ServerMode.Backup => Task.Run(BackupManager.AutoBackup),
                _ => throw new NotImplementedException(),
            };
        }
    }
}