using Verse;

namespace GameClient.Misc;

public static class ValueParser
{
    public static int[] IntVec3ToArray(IntVec3 data) { return [data.x, data.y, data.z]; }

    public static IntVec3 ArrayToIntVec3(int[] data) { return new IntVec3(data[0], data[1], data[2]); }
}