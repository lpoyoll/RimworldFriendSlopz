using RimWorld;

namespace RWTSharedColony
{
    internal static class PawnDraftControllerCompatibilityExtensions
    {
        public static void SetDrafted(this Pawn_DraftController controller, bool drafted)
        {
            if (controller != null) controller.Drafted = drafted;
        }
    }
}
