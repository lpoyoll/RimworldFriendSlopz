using System;
using System.Text.RegularExpressions;
using GameClient.Misc;
using GameClient.Values;
using RimWorld.Planet;
using Verse;
using static Shared.CommonEnumerators;

namespace GameClient.Scribers
{
    public static class TileScriber
    {
        public static string TileToScribe(Tile toSave)
        {
            ClientValues.ToggleUsingScriber(true);

            string scribeData = "";

            try
            {
                Scribe.saver.InitSaving("", RTScriber.ScribeTreeName);

                Scribe_Deep.Look(ref toSave, RTScriber.ScribeNodeName);

                Scribe.saver.FinalizeSaving();

                scribeData = new Regex(@">\s*<").Replace(RTScriber.StringWriter.ToString(), "><");
            }
            catch (Exception e) { Printer.Error(e.ToString(), LogImportanceMode.Verbose); };

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

                Scribe_Deep.Look(ref toLoad, RTScriber.ScribeNodeName);

                Scribe.loader.FinalizeLoading();
            }
            catch (Exception e) { Printer.Error(e.ToString(), LogImportanceMode.Verbose); };

            ClientValues.ToggleUsingScriber(false);

            return toLoad;
        }
    }
}
