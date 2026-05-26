using System;

namespace Shared
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class HandlesPacket : Attribute
    {
        public HandlesPacket(PacketHeader header)
        {
            this.header = header;
        }

        public readonly PacketHeader header;
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class ManagesPacket : Attribute { }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class OnSessionStart : Attribute { }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class OnSessionEnd : Attribute { }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class OnSynchronousStart : Attribute { }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class OnSynchronousEnd : Attribute { }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class OnSynchronousUpdate : Attribute { }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class OnUpdate : Attribute { }

    public enum PacketHeader : byte 
    { 
        None, Handshake, KeepAlive, Login, Transfer, Raid, Zoom, Aid, Caravan, Chat, Event, 
        GameParameter, GoodWill, Guild, Map, Mod, Road, Save, Settlement, Site, Version, World, 
        Pollution, Console, GlobalData, ResponseShortcut, PlayerRecount, Information, Leaderboard, 
        ServerBrowserTelemetry, ServerBrowserListing, Synchronous, Disconnect, VersionDownload, WorldObject 
    }
}