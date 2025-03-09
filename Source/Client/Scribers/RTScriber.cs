using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using GameClient.Misc;
using GameClient.Values;
using Verse;
using static Shared.CommonEnumerators;

namespace GameClient.Scribers
{
    public static class RTScriber
    {
        public static StringWriter StringWriter;

        public static readonly string ScribeTreeName = "T";

        public static readonly string ScribeNodeName = "N";

        public static string ThingToScribe(Thing toSave, int customCount = -1)
        {
            ClientValues.ToggleUsingScriber(true);

            string scribeData = "";

            try
            {
                int originalCount = toSave.stackCount;
                if (customCount != -1) toSave.stackCount = customCount;

                Scribe.saver.InitSaving("", ScribeTreeName);

                Scribe_Deep.Look(ref toSave, ScribeNodeName);

                Scribe.saver.FinalizeSaving();

                if (customCount != -1) toSave.stackCount = originalCount;

                scribeData = new Regex(@">\s*<").Replace(StringWriter.ToString(), "><");
            }
            catch (Exception e) { Printer.Error(e.ToString(), LogImportanceMode.Verbose); };

            ClientValues.ToggleUsingScriber(false);

            return scribeData.ToString();
        }

        public static Thing ScribeToThing(string scribeData)
        {
            ClientValues.ToggleUsingScriber(true);

            Thing toLoad = null;

            try
            {
                Scribe.loader.InitLoading(scribeData);

                Scribe_Deep.Look(ref toLoad, ScribeNodeName);

                Scribe.loader.FinalizeLoading();
            }
            catch (Exception e) { Printer.Error(e.ToString(), LogImportanceMode.Verbose); };

            ClientValues.ToggleUsingScriber(false);

            return toLoad;
        }
    }

    public static class ScriberH
    {
        public static bool CheckIfThingIsHuman(Thing thing)
        {
            try
            {
                if (thing.def.defName == "Human") return true;
                else return false;
            }
            catch { return false; }
        }

        public static bool CheckIfThingIsAnimal(Thing thing)
        {
            try
            {
                PawnKindDef animal = DefDatabase<PawnKindDef>.AllDefs.FirstOrDefault(fetch => fetch.defName == thing.def.defName);
                if (animal != null) return true;
                else return false;
            }
            catch { return false; }
        }

        public static bool CheckIfThingIsCorpse(Thing thing)
        {
            try
            {
                Corpse corpse = thing as Corpse;
                if (corpse != null) return true;
                else return false;
            }
            catch { return false; }
        }
    }
}