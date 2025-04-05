using System;
using MessagePack;
using static Shared.CommonEnumerators;

namespace Shared
{
    [MessagePackObject]
    public class NPCSettlementData
    {
        public SettlementStepMode _stepMode;

        public PlanetNPCSettlementDetails _settlementData = new PlanetNPCSettlementDetails();
    }
}