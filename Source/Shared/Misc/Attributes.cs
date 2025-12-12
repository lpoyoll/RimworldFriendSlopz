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

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class TriggerOnSessionStart() : Attribute { }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class TriggerOnSessionEnd() : Attribute { }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class TriggerPerFrame() : Attribute { }

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
        RecountManager,
        InformationManager,
        SPlayerDraft,
        SPlayerWeather,
        SPlayerMentalState,
        SPlayerGameSpeed,
    }
}