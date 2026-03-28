namespace Shared.Details.Planet
{
    public class NPCSettlementDetail
    {
        public int Tile { get; set; } = -1;

        public string Name { get; set; } = null;

        public string DefName { get; set; } = null;

        // This is only used if there are 2 factions of the same type loaded. It's not null or it would cause errors

        public string FactionName { get; set; } = string.Empty; 
    }
}