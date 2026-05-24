using GameClient.Defs;
using GameClient.Dialogs;
using GameClient.Dialogs.Default;
using GameClient.Managers;
using GameClient.Misc;
using GameClient.WorldObjects;
using RimWorld;
using RimWorld.Planet;
using Shared;
using Shared.Files.Sites;
using Shared.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TCPNetwork;
using TCPNetwork.PacketManagers;
using TCPNetwork.Packets;
using Verse;
using static Shared.CommonEnumerators;
using static Shared.Misc.Printer;
using static TCPNetwork.Packets.PKT_Site;


namespace GameClient.PacketManagers
{
    public class PM_Sites : PM_Base
    {
        public static List<FL_SiteType> SiteValues { get; set; }

        public static List<WO_Site> PlayerSites { get; set; } = new List<WO_Site>();

        private static CancellationTokenSource Token { get; set; } = new CancellationTokenSource();

        public static double RewardDelay { get; set; } = -1;

        [HandlesPacket(PacketHeader.SiteManager)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_Site data = Serializer.ConvertBytesToObject<PKT_Site>(bytes);

            switch (data._stepMode)
            {
                case SiteStepMode.Accept:
                    OnSiteAccept();
                    break;

                case SiteStepMode.Info:
                    OnSiteInfo(data._file);
                    break;

                case SiteStepMode.Build:
                    OnSiteBuild(data._file);
                    break;

                case SiteStepMode.Destroy:
                    OnSiteDestroy(data._file);
                    break;

                case SiteStepMode.Rewards:
                    OnReceiveRewards(data._rewardFiles);
                    break;
            }
        }

        public static void RequestSiteBuild(FL_SiteType configFile)
        {
            if (!RimworldManager.CheckIfHasEnoughItemInCaravan(SessionHandler.ChosenCaravan, ThingDefOf.Silver.defName, configFile.Cost))
            {
                DLG_Base.PushNewDialog(new DLG_Message("ERROR", new string[] { "You do not have enough silver!" }));
                return;
            }

            RimworldManager.RemoveThingFromCaravan(SessionHandler.ChosenCaravan,
                DefDatabase<ThingDef>.GetNamed(ThingDefOf.Silver.defName), configFile.Cost);

            PKT_Site siteData = new PKT_Site();
            siteData._stepMode = SiteStepMode.Build;
            siteData._file.Tile = SessionHandler.ChosenCaravan.Tile;
            siteData._file.Type.DefName = configFile.DefName;

            Network.ServerEndpoint.EnqueuePacket(PacketHeader.SiteManager, siteData);

            DLG_Base.PushNewDialog(new DLG_Wait());
        }

        public static void RequestDestroySite()
        {
            Action r1 = delegate
            {
                PKT_Site siteData = new PKT_Site();
                siteData._file.Tile = SessionHandler.ChosenSite.Tile;
                siteData._stepMode = SiteStepMode.Destroy;

                Network.ServerEndpoint.EnqueuePacket(PacketHeader.SiteManager, siteData);
            };

            DLG_YesNo d1 = new DLG_YesNo("Are you sure you want to destroy this site?", r1, null);
            DLG_Base.PushNewDialog(d1);
        }

        public static void RequestSiteChangeConfig(FL_SiteType config, string reward)
        {
            PKT_SiteRewardConfig rewardConfig = new PKT_SiteRewardConfig();
            rewardConfig._siteDef = config.DefName;
            rewardConfig._rewardDef = reward;

            PKT_Site siteData = new PKT_Site();
            siteData._stepMode = SiteStepMode.Config;
            siteData._rewardConfig = rewardConfig;

            Network.ServerEndpoint.EnqueuePacket(PacketHeader.SiteManager, siteData);
        }

        private static void OnReceiveRewards(FL_SiteReward[] files)
        {
            List<Thing> rewards = new List<Thing>();
            foreach (FL_SiteReward reward in files)
            {
                try
                {
                    ThingDef def = DefDatabase<ThingDef>.AllDefs.First(fetch => fetch.defName == reward.DefName);
                    Thing toMake = ThingMaker.MakeThing(def);
                    toMake.stackCount = reward.Amount;
                    toMake.HitPoints = def.BaseMaxHitPoints;
                    rewards.Add(toMake);

                    Printer.Message($"Received {reward.Amount} of {reward.DefName}", Verbosity.Verbose);
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

        public static void ClearAllSites()
        {
            PlayerSites.Clear();

            foreach (WorldObject site in Finder.GetAllRTSites())
            {
                FL_Site siteFile = new FL_Site();
                siteFile.Tile = site.Tile;
                OnSiteDestroy(siteFile);
            }
        }

        public static void OnSiteBuild(FL_Site toAdd)
        {
            if (!RimworldManager.CheckIfTileIsValid(toAdd.Tile)) return;
            else if (Find.WorldObjects.Sites.FirstOrDefault(fetch => fetch.Tile == toAdd.Tile) != null) return;
            else
            {
                try
                {
                    SitePartDef siteDef = RTSitePartDefs.Defs.First(fetch => fetch.defName == toAdd.Type.DefName);
                    WO_Site site = (WO_Site)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.AllDefs.First(fetch => fetch.defName == "RTSite"));
                    site.Tile = toAdd.Tile;
                    site.SetFaction(PlanetManagerHelper.GetPlayerFactionFromGoodwill(toAdd.Goodwill));
                    site.AddPart(new RTSitePart(site, siteDef));

                    PlayerSites.Add(site);
                    Find.WorldObjects.Add(site);
                }
                catch (Exception e) { Printer.Error($"Failed to spawn site at {toAdd.Tile}. Reason: {e}"); }
            }
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
            file.Type = SiteValues.First(fetch => fetch.DefName == site.MainSitePartDef.defName);

            OnSiteDestroy(file);
            OnSiteBuild(file);
        }

        private static void OnSiteAccept()
        {
            RimworldManager.GenerateLetter("Site built", $"You've built a site!", LetterDefOf.PositiveEvent);
            DLG_Wait.Instance.Close();
            PM_Saves.ForceSave();
        }

        private static void OnSiteInfo(FL_Site file)
        {
            DLG_Wait.Instance.Close();

            Action selectWorker = delegate
            {
                Pawn toSend = SessionHandler.ChosenCaravan.PawnsListForReading.Where(fetch => RimworldManager.CheckIfThingIsHuman(fetch)).ToList()
                    [DLG_ListingWithButton.ResultInt];

                PKT_Site siteData = new PKT_Site();
                siteData._stepMode = SiteStepMode.Worker;
                siteData._file.Tile = SessionHandler.ChosenSite.Tile;
                siteData._file.WorkerString = ScribeManager.SerializeToString(toSend, ScribeManager.SerializableType.Thing);

                SessionHandler.ChosenCaravan.RemovePawn(toSend);
                Find.WorldPawns.RemovePawn(toSend);
                toSend.Destroy();

                Network.ServerEndpoint.EnqueuePacket(PacketHeader.SiteManager, siteData);
                PM_Saves.ForceSave();
            };

            Action retrieveWorker = delegate
            {
                Pawn toRetrieve = ScribeManager.SerializeFromString<Pawn>(file.WorkerString, ScribeManager.SerializableType.Pawn);
                RimworldManager.PlaceThingIntoCaravan(toRetrieve, SessionHandler.ChosenCaravan);

                PKT_Site siteData = new PKT_Site();
                siteData._stepMode = SiteStepMode.Worker;
                siteData._file.Tile = SessionHandler.ChosenSite.Tile;

                Network.ServerEndpoint.EnqueuePacket(PacketHeader.SiteManager, siteData);
            };

            if (file.WorkerString == null)
            {
                List<string> contents = new List<string>();
                foreach (Pawn pawn in SessionHandler.ChosenCaravan.PawnsListForReading.Where(fetch => RimworldManager.CheckIfThingIsHuman(fetch)))
                {
                    contents.Add(pawn.LabelCap);
                }

                string title = "Available pawns";
                string description = "Choose the pawn you want to send as a worker";
                DLG_Base.PushNewDialog(new DLG_ListingWithButton(title, description, contents.ToArray(), selectWorker, null));
            }
            else { DLG_Base.PushNewDialog(new DLG_YesNo("Do you want to retrieve the worker from the site?", retrieveWorker)); }
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
                    if (currentRewardDelay >= RewardDelay)
                    {
                        MainThreadHandler.Instance.Enqueue(AskForSiteRewards);
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

        public static void AskForSiteRewards()
        {
            PKT_Site siteData = new PKT_Site();
            siteData._stepMode = SiteStepMode.Rewards;

            Network.ServerEndpoint.EnqueuePacket(PacketHeader.SiteManager, siteData);
        }

        public static void AskForInformation()
        {
            PKT_Site siteData = new PKT_Site();
            siteData._stepMode = SiteStepMode.Info;
            siteData._file.Tile = SessionHandler.ChosenSite.Tile;

            Network.ServerEndpoint.EnqueuePacket(PacketHeader.SiteManager, siteData);
        }

        public static void SetValues()
        {
            PM_Sites.SiteValues = SessionHandler.GlobalData.SiteValues;
            PM_Sites.RewardDelay = SessionHandler.GlobalData.ActionValues.SiteAction.TimeInterval;
        }
    }
}