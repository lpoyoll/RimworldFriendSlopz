using System;

namespace Shared
{
    // Used for instantiating managers
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class RTManager : Attribute { }

    // Used for loading in custom assemblies
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class RTStartupAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class HandlesPacket : Attribute
    {
        public HandlesPacket(PacketHeader header)
        {
            this.header = header;
        }

        public readonly PacketHeader header;
    }

    public enum PacketHeader : byte
    {
        None,
        KeepAliveManager,
        LoginManager,
        TransferManager,
        ActivityManager,
        AidManager,
        CaravanManager,
        ChatManager,
        EventManager,
        GameParameterManager,
        GoodWillManager,
        GuildManager,
        MapManager,
        ModManager,
        NPCManager,
        RoadManager,
        SaveManager,
        SettlementManager,
        SiteManager,
        VersionManager,
        WorldManager,
        PollutionManager,
        ConsoleManager,
        GlobalDataManager,
        ResponseShortcutManager,
        RecountManager
    }
}