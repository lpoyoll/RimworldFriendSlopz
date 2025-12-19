using RimWorld;
// ReSharper disable UnassignedField.Global

namespace GameClient.Defs;

[DefOf]
public static class RTWorldObjectDefOf
{
    public static WorldObjectDef RTCaravan;

    static RTWorldObjectDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(WorldObjectDefOf));
}