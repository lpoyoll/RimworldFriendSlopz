using System;
using System.Threading.Tasks;
using GameClient.Managers;
using Shared.Network.Client;

namespace GameClient.Misc
{
    public static class Threader
    {
        public enum Mode { Start, Chat }

        public static Task GenerateThread(Mode mode, Listener listener = null)
        {
            return mode switch
            {
                Mode.Start => Task.Run(Network.StartConnection),
                Mode.Chat => Task.Run(ChatManager.ChatClock),
                _ => throw new NotImplementedException()
            };
        }
    }
}