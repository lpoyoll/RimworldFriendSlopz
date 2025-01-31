namespace Shared
{
    public class CommonEnumerators
    {
        public enum ClientNetworkState { Disconnected, Connecting, Connected }

        public enum GenStepMode { Scenario, Storyteller, Difficulty }

        public enum WorldObjectMode { Settlement, Site, Caravan }

        public enum ResponseStepMode { IllegalAction, UserUnavailable, Pop }

        public enum SaveStepMode { Send, Receive, Reset }

        public enum SpyStepMode { Request, Accept, Deny }

        public enum ServerFileMode { Configs, Actions, Sites, Roads, World, Whitelist, Difficulty, Scenario, Storyteller, Discord, Backup, Mods, Chat }

        public enum LogMode { Message, Warning, Error, Title, Outsider }

        public enum LogImportanceMode { Normal, Verbose, Extreme }

        public enum CommandMode { Op, Deop, Broadcast, ForceSave }

        public enum EventStepMode { Send, Receive, Recover }

        public enum AidStepMode { Send, Receive, Accept, Reject }

        public enum CaravanStepMode { Add, Remove, Move }

        public enum RoadStepMode { Add, Remove }

        public enum ModConfigStepMode { Send, Ask }

        public enum GuildStepMode { Create, Delete, NameInUse, NoPower, AddMember, RemoveMember, AcceptInvite, Promote, Demote, AdminProtection, MemberList }

        public enum FactionRanks { Member, Moderator, Admin }

        public enum Goodwill { Enemy, Neutral, Ally, Faction, Personal }

        public enum GoodwillTarget { Settlement, Site }

        public enum TransferMode { Gift, Trade, Rebound, Pod }

        public enum TransferLocation { Caravan, Settlement, Pod }

        public enum TransferStepMode { TradeRequest, TradeAccept, TradeReject, TradeReRequest, TradeReAccept, TradeReReject, Recover, Pod }

        public enum OfflineActivityStepMode { Request, Deny }

        public enum OnlineActivityStepMode { Request, Accept, Deny, Ready, Stop, Buffer }

        public enum OnlineActivityTargetFaction { Faction, NonFaction, None }

        public enum OnlineActivityApplyMode { Add, Remove }

        public enum OnlineActivityType { None, Visit, Raid }

        public enum OfflineActivityType { None, Visit, Raid }

        public enum ActionTargetType { Thing, Human, Animal, Cell, Invalid }

        public enum CreationType { Human, Animal, Corpse, Thing }

        public enum SiteStepMode { Accept, Deny, Build, Visit, Raid, Destroy, Info, Config, Rewards}

        public enum SettlementStepMode { Add, Remove }

        public enum WorldStepMode { AskFor, Required, Sent }

        public enum SaveMode { Disconnect, Autosave, Strict }

        public enum UserColor { Normal, Admin, Console, Private, Discord, Server }

        public enum MessageColor { Normal, Admin, Console, Private, Discord, Server }

        public enum ModType { Required, Optional, Forbidden };

        public enum LoginResponse { InvalidLogin, BannedLogin, RegisterError, ExtraLogin, WrongMods, WrongVersion, ServerFull, Whitelist, NoWorld }
    }
}

