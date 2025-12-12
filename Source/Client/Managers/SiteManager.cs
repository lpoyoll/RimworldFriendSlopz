using GameClient.Dialogs;
using GameClient.Managers;
using GameClient.Misc;
using GameClient.Values;
using GameClient.WorldObjects;
using RimWorld;
using RimWorld.Planet;
using Shared;
using Shared.Files.Sites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TCPNetwork.Packets;
using Verse;
using static Shared.CommonEnumerators;


namespace GameClient.Managers
{
    public static class SiteManager
    {
        public static SitePartDef[] SiteDefs { get; set; }

        public static SiteType[] SiteValues { get; set; }

        public static List<Site> PlayerSites { get; set; } = new List<Site>();

        private static CancellationTokenSource Token { get; set; } = new CancellationTokenSource();

        public static double RewardDelay { get; set; } = -1;

        [HandlesPacket(PacketHeader.SiteManager)]
        private static void ParsePacket(byte[] bytes)
        {
            SiteData data = Serializer.ConvertBytesToObject<SiteData>(bytes);

            Printer.Warning(data, LogImportanceMode.Extreme);

            switch (data._stepMode)
            {
                case SiteStepMode.Accept:
                    OnSiteAccept();
                    break;

                case SiteStepMode.Build:
                    SpawnSingleSite(data._file);
                    break;

                case SiteStepMode.Destroy:
                    RemoveSingleSite(data._file);
                    break;

                case SiteStepMode.Rewards:
                    ReceiveSiteRewards(data._rewardFiles);
                    break;
            }
        }

        public static void RequestSiteBuild(SiteType configFile)
        {
            if (!RimworldManager.CheckIfHasEnoughItemInCaravan(SessionValues.ChosenCaravan, ThingDefOf.Silver.defName, configFile.Cost))
            {
                RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "You do not have enough silver!" }));
                return;
            }

            RimworldManager.RemoveThingFromCaravan(SessionValues.ChosenCaravan,
                DefDatabase<ThingDef>.GetNamed(ThingDefOf.Silver.defName), configFile.Cost);

            SiteData siteData = new SiteData();
            siteData._stepMode = SiteStepMode.Build;
            siteData._file.Tile = SessionValues.ChosenCaravan.Tile;
            siteData._file.Type.DefName = configFile.DefName;

            ClientNetwork.Instance.ClientListener.EnqueuePacket(PacketHeader.SiteManager, siteData);

            RT_Dialog_Base.PushNewDialog(new RT_Dialog_Wait("Waiting for building"));
        }

        public static void RequestDestroySite()
        {
            Action r1 = delegate
            {
                SiteData siteData = new SiteData();
                siteData._file.Tile = SessionValues.ChosenSite.Tile;
                siteData._stepMode = SiteStepMode.Destroy;

                ClientNetwork.Instance.ClientListener.EnqueuePacket(PacketHeader.SiteManager, siteData);
            };

            RT_Dialog_YesNo d1 = new RT_Dialog_YesNo("Are you sure you want to destroy this site?", r1, null);
            RT_Dialog_Base.PushNewDialog(d1);
        }

        public static void RequestSiteChangeConfig(SiteType config, string reward)
        {
            SiteRewardConfigData rewardConfig = new SiteRewardConfigData();
            rewardConfig._siteDef = config.DefName;
            rewardConfig._rewardDef = reward;

            SiteData siteData = new SiteData();
            siteData._stepMode = SiteStepMode.Config;
            siteData._rewardConfig = rewardConfig;

            ClientNetwork.Instance.ClientListener.EnqueuePacket(PacketHeader.SiteManager, siteData);
        }

        private static void ReceiveSiteRewards(SiteReward[] files)
        {
            List<Thing> rewards = new List<Thing>();
            foreach (SiteReward reward in files)
            {
                try
                {
                    ThingDef def = DefDatabase<ThingDef>.AllDefs.First(fetch => fetch.defName == reward.DefName);
                    Thing toMake = ThingMaker.MakeThing(def);
                    toMake.stackCount = reward.Amount;
                    toMake.HitPoints = def.BaseMaxHitPoints;
                    rewards.Add(toMake);

                    Printer.Message($"Received {reward.Amount} of {reward.DefName}", LogImportanceMode.Verbose);
                }
                catch (Exception e) { Printer.Warning(e.ToString(), LogImportanceMode.Verbose); }
            }

            if (rewards.Count > 0)
            {
                TransferManager.GetTransferedItemsToSettlement(rewards.ToArray(), true, false, false);
                RimworldManager.GenerateLetter("Site rewards", $"You've received your site rewards", LetterDefOf.PositiveEvent);
                Printer.Message("Rewards delivered", LogImportanceMode.Verbose);
            }
        }

        public static void AddSites(SiteFile[] sites)
        {
            foreach (SiteFile toAdd in sites)
            {
                SpawnSingleSite(toAdd);
            }
        }

        public static void ClearAllSites()
        {
            PlayerSites.Clear();

            Site[] sites = Find.WorldObjects.Sites.Where(fetch => ClientValues.PlayerFactions.Contains(fetch.Faction) ||
                fetch.Faction == Faction.OfPlayer).ToArray();

            foreach (Site toRemove in sites)
            {
                SiteFile siteFile = new SiteFile();
                siteFile.Tile = toRemove.Tile;
                RemoveSingleSite(siteFile);
            }
        }

        public static void SpawnSingleSite(SiteFile toAdd)
        {
            if (Find.WorldObjects.Sites.FirstOrDefault(fetch => fetch.Tile == toAdd.Tile) != null) return;
            else
            {
                try
                {
                    SitePartDef siteDef = SiteDefs.First(fetch => fetch.defName == toAdd.Type.DefName);
                    Site site = SiteMaker.MakeSite(sitePart: siteDef,
                        tile: toAdd.Tile,
                        threatPoints: 1000,
                        faction: PlanetManagerHelper.GetPlayerFactionFromGoodwill(toAdd.Goodwill));

                    PlayerSites.Add(site);
                    Find.WorldObjects.Add(site);
                }
                catch (Exception e) { Printer.Error($"Failed to spawn site at {toAdd.Tile}. Reason: {e}"); }
            }
        }

        public static void RemoveSingleSite(SiteFile toRemove)
        {
            try
            {
                Site toGet = Find.WorldObjects.Sites.Find(fetch => fetch.Tile == toRemove.Tile);
                if (!RimworldManager.CheckIfMapHasPlayerPawns(toGet.Map))
                {
                    if (PlayerSites.Contains(toGet)) PlayerSites.Remove(toGet);
                    Find.WorldObjects.Remove(toGet);
                }
                else Printer.Warning($"Ignored removal of site at {toGet.Tile} because player was inside");
            }
            catch (Exception e) { Printer.Error($"Failed to remove site at {toRemove.Tile}. Reason: {e}"); }
        }

        private static void OnSiteAccept()
        {
            RimworldManager.GenerateLetter("Site built", $"You've built a site!", LetterDefOf.PositiveEvent);
            RT_Dialog_Wait.Instance.Close();
            SaveManager.ForceSave();
        }

        [TriggerOnSessionStart]
        private static void StartTickingSites()
        {
            Token = new CancellationTokenSource();
            double currentRewardDelay = 0;
            int tickDuration = 100;
            Task.Run(async () =>
            {
                while (!Token.Token.IsCancellationRequested)
                {
                    Printer.Warning(1);

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

        [TriggerOnSessionEnd]
        private static void StopTickingSites() { Token.Cancel(); }

        public static void AskForSiteRewards()
        {
            SiteData siteData = new SiteData();
            siteData._stepMode = SiteStepMode.Rewards;

            ClientNetwork.Instance.ClientListener.EnqueuePacket(PacketHeader.SiteManager, siteData);
        }
    }
}

public static class SiteManagerH
{
    public static SiteFile[] tempSites;

    public static void SetValues(ServerGlobalData serverGlobalData)
    {
        tempSites = serverGlobalData._playerSites;
        SiteManager.SiteValues = serverGlobalData._siteValues;
        SiteManager.RewardDelay = serverGlobalData._actionValues.SiteAction.TimeInterval;
    }

    public static void SetSiteDefs()
    {
        SiteManager.SiteDefs = new SitePartDef[]
        {
            RTSitePartDefOf.RTFarmland,
            RTSitePartDefOf.RTHunterCamp,
            RTSitePartDefOf.RTQuarry,
            RTSitePartDefOf.RTSawmill,
            RTSitePartDefOf.RTBank,
            RTSitePartDefOf.RTLaboratory,
            RTSitePartDefOf.RTRefinery,
            RTSitePartDefOf.RTHerbalWorkshop,
            RTSitePartDefOf.RTTextileFactory,
            RTSitePartDefOf.RTFoodProcessor
        };
    }
}


