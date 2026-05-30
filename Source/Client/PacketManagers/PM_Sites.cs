using GameClient.Defs;
using GameClient.Dialogs;
using GameClient.Dialogs.Default;
using GameClient.Managers;
using GameClient.Misc;
using GameClient.WorldObjects;
using RimWorld;
using RimWorld.Planet;
using RTShared;
using RTShared.Files;
using RTShared.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RTNetwork;
using RTNetwork.PacketManagers;
using RTNetwork.Packets;
using Verse;
using static RTShared.CommonEnumerators;
using static RTShared.Misc.Printer;
using static RTNetwork.Packets.PKT_Site;
using RTNetwork.Components;


namespace GameClient.PacketManagers
{
    public class PM_Sites : PM_Base
    {
        public static List<WO_Site> PlayerSites { get; set; } = new List<WO_Site>();

        private static CancellationTokenSource Token { get; set; } = new CancellationTokenSource();

        [HandlesPacket(PacketHeader.Site)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_Site data = Serializer.ConvertBytesToObject<PKT_Site>(bytes);

            switch (data.StepMode)
            {
                case SiteStepMode.Accept:
                    OnSiteAccept();
                    break;

                case SiteStepMode.Worker:
                    OnSiteWorkerInfo(data.File);
                    break;

                case SiteStepMode.Build:
                    OnSiteBuild(data.File);
                    break;

                case SiteStepMode.Destroy:
                    OnSiteDestroy(data.File);
                    break;

                case SiteStepMode.Rewards:
                    OnReceiveRewards(data.Files);
                    break;
            }
        }

        public static void RequestSiteBuild()
        {
            int toRemove = SessionManager.CurrentActionValues.SiteAction.BuildingCost;

            if (!RimworldManager.CheckIfHasEnoughItemInCaravan(SessionManager.ChosenCaravan, ThingDefOf.Silver.defName, toRemove))
            {
                DLG_Base.PushNewDialog(new DLG_Message("ERROR", new string[] { "You do not have enough silver!" }));
                return;
            }

            RimworldManager.RemoveThingFromCaravan(SessionManager.ChosenCaravan, DefDatabase<ThingDef>.GetNamed(ThingDefOf.Silver.defName), toRemove);

            PKT_Site siteData = new PKT_Site();
            siteData.StepMode = SiteStepMode.Build;
            siteData.File.Tile = SessionManager.ChosenCaravan.Tile;
            Network.ServerEndpoint.EnqueuePacket(PacketHeader.Site, siteData);

            DLG_Base.PushNewDialog(new DLG_Wait());
        }

        public static void RequestDestroySite()
        {
            Action r1 = delegate
            {
                PKT_Site siteData = new PKT_Site();
                siteData.File.Tile = SessionManager.ChosenSite.Tile;
                siteData.StepMode = SiteStepMode.Destroy;

                Network.ServerEndpoint.EnqueuePacket(PacketHeader.Site, siteData);
            };

            DLG_YesNo d1 = new DLG_YesNo("Are you sure you want to destroy this site?", r1, null);
            DLG_Base.PushNewDialog(d1);
        }

        private static void RequestPutWorker()
        {
            DLG_Wait.Instance.Close();

            Action selectWorker = delegate
            {
                Pawn toSend = SessionManager.ChosenCaravan.PawnsListForReading.Where(fetch => RimworldManager.CheckIfThingIsHuman(fetch)).ToList()
                    [DLG_ListingWithButton.ResultInt];

                PKT_Site siteData = new PKT_Site();
                siteData.StepMode = SiteStepMode.Worker;
                siteData.File.Tile = SessionManager.ChosenSite.Tile;
                siteData.File.WorkerString = ScribeManager.SerializeToString(toSend, ScribeManager.SerializableType.Pawn);

                SessionManager.ChosenCaravan.RemovePawn(toSend);
                Find.WorldPawns.RemovePawn(toSend);
                toSend.Destroy();

                Network.ServerEndpoint.EnqueuePacket(PacketHeader.Site, siteData);
                PM_Saves.ForceSave();
            };

            List<string> contents = new List<string>();
            foreach (Pawn pawn in SessionManager.ChosenCaravan.PawnsListForReading.Where(fetch => RimworldManager.CheckIfThingIsHuman(fetch)))
            {
                contents.Add(pawn.LabelCap);
            }

            string title = "Available pawns";
            string description = "Choose the pawn you want to send as a worker";
            DLG_Base.PushNewDialog(new DLG_ListingWithButton(title, description, contents.ToArray(), selectWorker, null));
        }

        private static void RequestRetrieveWorker(FL_Site file)
        {
            DLG_Wait.Instance.Close();

            Action retrieveWorker = delegate
            {
                Pawn toRetrieve = ScribeManager.SerializeFromString<Pawn>(file.WorkerString, ScribeManager.SerializableType.Pawn);
                RimworldManager.PlaceThingIntoCaravan(toRetrieve, SessionManager.ChosenCaravan);

                PKT_Site siteData = new PKT_Site();
                siteData.StepMode = SiteStepMode.RetrieveWorker;
                siteData.File.Tile = SessionManager.ChosenSite.Tile;
                Network.ServerEndpoint.EnqueuePacket(PacketHeader.Site, siteData);
            };

            DLG_Base.PushNewDialog(new DLG_YesNo("Do you want to retrieve the worker from the site?", retrieveWorker));
        }

        public static void RequestSiteRewards()
        {
            PKT_Site siteData = new PKT_Site();
            siteData.StepMode = SiteStepMode.Rewards;
            Network.ServerEndpoint.EnqueuePacket(PacketHeader.Site, siteData);
        }

        public static void RequestWorkerInfo()
        {
            PKT_Site siteData = new PKT_Site();
            siteData.StepMode = SiteStepMode.Worker;
            siteData.File.Tile = SessionManager.ChosenSite.Tile;
            Network.ServerEndpoint.EnqueuePacket(PacketHeader.Site, siteData);
        }

        private static void OnReceiveRewards(List<FL_Site> files)
        {
            List<Thing> rewards = new List<Thing>();
            foreach (FL_Site file in files)
            {
                try
                {
                    Thing toMake = ThingMaker.MakeThing(ThingDefOf.Silver);
                    toMake.stackCount = SessionManager.CurrentActionValues.SiteAction.RewardsCount;
                    toMake.HitPoints = toMake.def.BaseMaxHitPoints;

                    rewards.Add(toMake);
                }
                catch (Exception e) { Printer.Warning(e.ToString(), Verbosity.Verbose); }
            }

            if (rewards.Count > 0)
            {
                Map map = Find.AnyPlayerHomeMap;
                IntVec3 position = RimworldManager.GetTransferLocationInMap(map);
                foreach (Thing thing in rewards) RimworldManager.PlaceThingIntoMap(thing, map, position, true);

                RimworldManager.GenerateLetter("Site rewards", $"You've received your site rewards", LetterDefOf.PositiveEvent);
                Printer.Message("Rewards delivered", Verbosity.Verbose);
            }
        }

        public static void AddSites(List<FL_Site> sites)
        {
            foreach (FL_Site toAdd in sites)
            {
                OnSiteBuild(toAdd);
            }
        }

        public static void OnSiteBuild(FL_Site toAdd)
        {
            try
            {
                SitePartDef siteDef = RTSitePartDefOf.RTBase;
                WO_Site site = (WO_Site)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.AllDefs.First(fetch => fetch.defName == "RTSite"));
                site.Tile = toAdd.Tile;
                site.SetFaction(PlanetManagerHelper.GetPlayerFactionFromGoodwill(toAdd.Goodwill));
                site.AddPart(new RTSitePart(site, siteDef));

                PlayerSites.Add(site);
                Find.WorldObjects.Add(site);
            }
            catch (Exception e) { Printer.Error($"Failed to spawn site at {toAdd.Tile}. Reason: {e}"); }
        }

        public static void OnSiteDestroy(FL_Site toRemove)
        {
            try
            {
                WO_Site toGet = Finder.GetRTSiteFromTile(toRemove.Tile);
                if (!RimworldManager.CheckIfMapHasPlayerPawns(toGet.Map))
                {
                    if (PlayerSites.Contains(toGet)) PlayerSites.Remove(toGet);
                    Find.WorldObjects.Remove(toGet);
                }
                else Printer.Warning($"Ignored removal of site at {toGet.Tile} because player was inside");
            }
            catch (Exception e) { Printer.Error($"Failed to remove site at {toRemove.Tile}. Reason: {e}"); }
        }

        public static void RecalculateSiteGoodwill(WO_Site site, Goodwill goodwill)
        {
            FL_Site file = new FL_Site();
            file.Tile = site.Tile;
            file.Goodwill = goodwill;

            OnSiteDestroy(file);
            OnSiteBuild(file);
        }

        private static void OnSiteAccept()
        {
            RimworldManager.GenerateLetter("Site built", $"You've built a site!", LetterDefOf.PositiveEvent);
            DLG_Wait.Instance.Close();
            PM_Saves.ForceSave();
        }

        private static void OnSiteWorkerInfo(FL_Site file)
        {
            if (string.IsNullOrEmpty(file.WorkerString)) RequestPutWorker();
            else { RequestRetrieveWorker(file); }
        }

        [OnSessionStart]
        private static void StartTickingSites()
        {
            Token = new CancellationTokenSource();
            double currentRewardDelay = 0;
            int tickDuration = 100;
            Task.Run(async () =>
            {
                while (!Token.Token.IsCancellationRequested)
                {
                    if (currentRewardDelay >= SessionManager.CurrentActionValues.SiteAction.TimeInterval)
                    {
                        MainThreadHandler.Instance.Enqueue(RequestSiteRewards);
                        currentRewardDelay = 0;
                    }

                    else
                    {
                        await Task.Delay(tickDuration, Token.Token);
                        currentRewardDelay += tickDuration;
                    }
                }
            });
        }

        [OnSessionEnd]
        private static void StopTickingSites() { Token.Cancel(); }

        [OnSessionEnd]
        private static void CleanValues() { PlayerSites.Clear(); }
    }
}