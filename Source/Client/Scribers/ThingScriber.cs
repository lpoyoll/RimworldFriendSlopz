using Shared;
using Verse;

namespace GameClient.Scribers
{
    public static class ThingScriber
    {
        public static ThingFile ThingToString(Thing thing, int thingCount)
        {
            ThingFile thingData = new ThingFile();

            thingData.ID = thing.ThingID;

            thingData.ScribeData = RTScriber.ThingToScribe(thing, thingCount);

            return thingData;
        }

        public static Thing StringToThing(ThingFile thingData)
        {
            return RTScriber.ScribeToThing(thingData.ScribeData);
        }
    }
}
