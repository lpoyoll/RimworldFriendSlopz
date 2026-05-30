using System;
using System.Collections.Generic;
using System.Linq;
using GameClient.Dialogs;
using RimWorld;
using RTShared;
using Verse;
using RTShared.Files;
using RTNetwork.Packets;
using RTNetwork;
using GameClient.Managers;
using static RTNetwork.Packets.PKT_Event;
using GameClient.Dialogs.Default;
using RTNetwork.PacketManagers;
using RTNetwork.Components;

namespace GameClient.PacketManagers
{
    public class PM_Events : PM_Base
    {
        public static List<FL_Event> AvailableEvents { get; private set; } = new List<FL_Event>();

        public static List<FL_Event> EnabledEvents { get; private set; } = new List<FL_Event>();

        [HandlesPacket(PacketHeader.Event)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_Event data = Serializer.ConvertBytesToObject<PKT_Event>(bytes);

            switch (data._stepMode)
            {
                case EventStepMode.Send:
                    OnEventSent();
                    break;

                case EventStepMode.Receive:
                    OnEventReceived(data);
                    break;

                case EventStepMode.Recover:
                    OnRecoverEventSilver();
                    break;

                case EventStepMode.Set:
                    SetValues(data._eventFiles);
                    break;
            }
        }

        public static void SendExistingEventsToServer()
        {          
            List<FL_Event> existingEvents = new List<FL_Event>();
            foreach (IncidentDef incident in DefDatabase<IncidentDef>.AllDefs)
            {
                FL_Event file = new FL_Event();
                file.Name = incident.LabelCap;
                file.DefName = incident.defName;
                file.Cost = 500;
                file.IsEnabled = true;

                existingEvents.Add(file);
            }

            PKT_Event eventData = new PKT_Event();
            eventData._stepMode = EventStepMode.Set;
            eventData._eventFiles = existingEvents;
            Network.ServerEndpoint.EnqueuePacket(PacketHeader.Event, eventData);
        }

        public static void ShowEventMenu()
        {
            List<string> eventNames = new List<string>();
            foreach (FL_Event eventFile in EnabledEvents) eventNames.Add(eventFile.Name);

            Action a1 = delegate
            {
                DLG_YesNo d2 = new DLG_YesNo($"This event will cost you {EnabledEvents[DLG_ScrollButtons.SelectedScrollButton].Cost} " +
                    $"silver, continue?", SendEvent, null);

                DLG_Base.PushNewDialog(d2);
            };

            DLG_ScrollButtons d1 = new DLG_ScrollButtons("Event Selector", "Choose the event you want to send",
                eventNames.ToArray(), a1.Invoke, null);

            DLG_Base.PushNewDialog(d1);
        }

        public static void OpenEventManagerMenu() { DLG_Base.PushNewDialog(new DLG_EventConfig(AvailableEvents)); }

        public static void SendEvent()
        {
            DLG_ScrollButtons.Instance.Close();

            //TODO
            //MAKE IT SO ALL MAPS ARE ACCOUNTED FOR
            Map toGetSilverFrom = Find.AnyPlayerHomeMap;

            if (!RimworldManager.CheckIfHasEnoughSilverInMap(toGetSilverFrom, EnabledEvents[DLG_ScrollButtons.SelectedScrollButton].Cost))
            {
                DLG_Base.PushNewDialog(new DLG_Message("ERROR", new string[] { "You do not have enough silver for this action!" }));
            }

            else
            {
                RimworldManager.RemoveThingFromSettlement(toGetSilverFrom, ThingDefOf.Silver, EnabledEvents[DLG_ScrollButtons.SelectedScrollButton].Cost);

                PKT_Event eventData = new PKT_Event();
                eventData._stepMode = EventStepMode.Send;
                eventData._fromTile = toGetSilverFrom.Tile;
                eventData._toTile = SessionManager.ChosenSettlement.Tile;
                eventData._eventFile = EnabledEvents[DLG_ScrollButtons.SelectedScrollButton];

                Network.ServerEndpoint.EnqueuePacket(PacketHeader.Event, eventData);

                DLG_Base.PushNewDialog(new DLG_Wait());
            }
        }

        public static void SetValues(List<FL_Event> events)
        {
            AvailableEvents = events.OrderBy(fetch => fetch.Name).ToList();
            EnabledEvents = AvailableEvents.Where(fetch => fetch.IsEnabled).ToList();
        }

        public static void TriggerEvent(IncidentDef eventToTrigger, Map targetMap)
        {
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(eventToTrigger.category, targetMap);
            parms.customLetterLabel = $"Event - {eventToTrigger.LabelCap}";
            parms.faction = SessionManager.NeutralFaction;
            parms.target = targetMap;

            eventToTrigger.Worker.TryExecute(parms);

            PM_Saves.ForceSave();
        }

        public static void OnEventReceived(PKT_Event eventData)
        {
            Map targetMap;
            if (eventData._toTile != -1) targetMap = Find.WorldObjects.Settlements.FirstOrDefault(fetch => fetch.Tile == eventData._toTile).Map;
            else targetMap = Find.AnyPlayerHomeMap;

            IncidentDef eventToTrigger = DefDatabase<IncidentDef>.AllDefs.FirstOrDefault(fetch => fetch.defName == eventData._eventFile.DefName);
            if (eventToTrigger != null) TriggerEvent(eventToTrigger, targetMap);
        }

        public static void OnEventSent()
        {
            DLG_Wait.Instance.Close();

            RimworldManager.GenerateLetter("Event sent!", "Your event has been sent!",
                LetterDefOf.PositiveEvent);

            PM_Saves.ForceSave();
        }

        private static void OnRecoverEventSilver()
        {
            DLG_Wait.Instance.Close();

            //TODO
            //MAKE IT SO ALL MAPS ARE ACCOUNTED FOR
            Map toReturnTo = Find.AnyPlayerHomeMap;

            Thing silverToReturn = ThingMaker.MakeThing(ThingDefOf.Silver);
            silverToReturn.stackCount = EnabledEvents[DLG_ScrollButtons.SelectedScrollButton].Cost;

            RimworldManager.PlaceThingIntoMap(silverToReturn, toReturnTo, toReturnTo.Center, false);

            DLG_Base.PushNewDialog(new DLG_Message("ERROR", new string[] { "Player is not currently available!" }));
        }
    }
}
