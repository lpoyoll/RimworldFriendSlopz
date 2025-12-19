using RimWorld;
// ReSharper disable UnassignedField.Global

namespace GameClient.Defs;

[DefOf]
public static class RTFactionDefOf
{
    public static FactionDef RTNeutral;

    public static FactionDef RTAlly;

    public static FactionDef RTEnemy;

    public static FactionDef RTFaction;

    static RTFactionDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(FactionDefOf));
}