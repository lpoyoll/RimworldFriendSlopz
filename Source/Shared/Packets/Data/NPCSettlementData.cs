using static Shared.CommonEnumerators;

namespace Shared
{

    public class NPCSettlementData
    {
        public SettlementStepMode _stepMode { get; set; }

        public PlanetNPCSettlementDetails _settlementData { get; set; } = new PlanetNPCSettlementDetails();
    }
}