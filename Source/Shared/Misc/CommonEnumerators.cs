namespace Shared
{
    public class CommonEnumerators
    {
        public enum AssemblyType { Client, Server }

        public enum CommandMode { Op, Deop, Broadcast, ForceSave, Disconnect }

        public enum Goodwill { Enemy, Neutral, Ally, Guild, Personal }

        public enum GoodwillTarget { Settlement, Site }

        public enum SettlementStepMode { Add, Remove }

        public enum TradeMode { None, Sending, Receiving }
    }
}

