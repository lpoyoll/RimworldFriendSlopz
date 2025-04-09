using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GameClient.Misc;
using GameClient.Values;
using RimWorld.Planet;
using Shared;
using Verse;
using static Shared.CommonEnumerators;

namespace GameClient.Managers
{
    public static class ScribeManager
    {
        public static StringWriter StringWriter { get; set; } = new StringWriter();

        public static string ScribeTreeName { get; private set; } = "T";

        public static string ScribeNodeName { get; private set; } = "N";

        public static string SerializeFromThing(Thing toSave, int customCount = -1)
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

        public static Thing SerializeToThing(string scribeData)
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

        public static ThingFile ThingToString(Thing thing, int thingCount)
        {
            ThingFile thingData = new ThingFile();

            thingData.ID = thing.ThingID;

            thingData.ScribeData = ScribeManager.SerializeFromThing(thing, thingCount);

            return thingData;
        }

        public static Thing StringToThing(ThingFile thingData)
        {
            return ScribeManager.SerializeToThing(thingData.ScribeData);
        }

        public static HumanFile HumanToString(Pawn human)
        {
            HumanFile humanFile = new HumanFile();

            humanFile.ID = human.ThingID;

            humanFile.ScribeData = ScribeManager.SerializeFromThing(human);

            return humanFile;
        }

        public static Pawn StringtoHuman(HumanFile file)
        {
            return (Pawn)ScribeManager.SerializeToThing(file.ScribeData);
        }

        public static AnimalFile AnimalToString(Pawn animal)
        {
            AnimalFile animalData = new AnimalFile();

            animalData.ID = animal.ThingID;

            animalData.ScribeData = ScribeManager.SerializeFromThing(animal);

            return animalData;
        }

        public static Pawn StringToAnimal(AnimalFile file)
        {
            return (Pawn)ScribeManager.SerializeToThing(file.ScribeData);
        }

        public static string TileToScribe(Tile toSave)
        {
            ClientValues.ToggleUsingScriber(true);

            string scribeData = "";

            try
            {
                Scribe.saver.InitSaving("", ScribeManager.ScribeTreeName);

                Scribe_Deep.Look(ref toSave, ScribeManager.ScribeNodeName);

                Scribe.saver.FinalizeSaving();

                scribeData = new Regex(@">\s*<").Replace(ScribeManager.StringWriter.ToString(), "><");
            }
            catch (Exception e) { Printer.Error(e.ToString(), LogImportanceMode.Verbose); }

            ClientValues.ToggleUsingScriber(false);

            return scribeData.ToString();
        }

        public static Tile ScribeToTile(string scribeData)
        {
            ClientValues.ToggleUsingScriber(true);

            Tile toLoad = null;

            try
            {
                Scribe.loader.InitLoading(scribeData);

                Scribe_Deep.Look(ref toLoad, ScribeManager.ScribeNodeName);

                Scribe.loader.FinalizeLoading();
            }
            catch (Exception e) { Printer.Error(e.ToString(), LogImportanceMode.Verbose); }

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