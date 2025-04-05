using System;
using MessagePack;
using static Shared.CommonEnumerators;

namespace Shared
{
    [MessagePackObject]
    public class PlayerSettlementData
    {
        public SettlementStepMode _stepMode;

        public SettlementFile _settlementFile = new SettlementFile();
    }
}
