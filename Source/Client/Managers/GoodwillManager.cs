using GameClient.Dialogs;
using GameClient.Misc;
using GameClient.WorldObjects;
using RimWorld;
using RimWorld.Planet;
using Shared;
using System.Linq;
using Verse;
using static Shared.CommonEnumerators;
using TCPNetwork.Packets.Goodwills;


namespace GameClient.Managers;
public static class GoodwillManager
{
    [HandlesPacket(PacketHeader.GoodWillManager)]
    private static void ParsePacket(byte[] bytes)
    {
        FactionGoodwillData data = Serializer.ConvertBytesToObject<FactionGoodwillData>(bytes);

        ChangeStructureGoodwill(data);
        RT_Dialog_Wait.Instance.Close();
    }

    //Tries to request a goodwill change depending on the values given

    public static void TryRequestGoodwill(Goodwill type, GoodwillTarget target)
    {
        int tileToUse = target switch
        {
            GoodwillTarget.Settlement => SessionHandler.ChosenSettlement.Tile,
            GoodwillTarget.Site => SessionHandler.ChosenSite.Tile,
            _ => 0
        };

        Faction factionToUse = target switch
        {
            GoodwillTarget.Settlement => SessionHandler.ChosenSettlement.Faction,
            GoodwillTarget.Site => SessionHandler.ChosenSite.Faction,
            _ => null
        };

        switch (type)
        {
            case Goodwill.Enemy when factionToUse == SessionHandler.EnemyFaction:
            {
                RT_Dialog_Message d1 = new RT_Dialog_Message("ERROR", ["Chosen settlement is already marked as enemy!"]);
                RT_Dialog_Base.PushNewDialog(d1);
                break;
            }
            case Goodwill.Enemy:
                RequestChangeStructureGoodwill(tileToUse, Goodwill.Enemy);
                break;
            case Goodwill.Neutral when factionToUse == SessionHandler.NeutralFaction:
            {
                RT_Dialog_Message d1 = new RT_Dialog_Message("ERROR", ["Chosen settlement is already marked as neutral!"
                ]);
                RT_Dialog_Base.PushNewDialog(d1);
                break;
            }
            case Goodwill.Neutral:
                RequestChangeStructureGoodwill(tileToUse, Goodwill.Neutral);
                break;
            case Goodwill.Ally when factionToUse == SessionHandler.AllyFaction:
            {
                RT_Dialog_Message d1 = new RT_Dialog_Message("ERROR", ["Chosen settlement is already marked as ally!"]);
                RT_Dialog_Base.PushNewDialog(d1);
                break;
            }
            case Goodwill.Ally:
                RequestChangeStructureGoodwill(tileToUse, Goodwill.Ally);
                break;
        }
    }

    public static void RequestChangeStructureGoodwill(int structureTile, Goodwill goodwill)
    {
        FactionGoodwillData factionGoodwillData = new FactionGoodwillData();
        factionGoodwillData._tile = structureTile;
        factionGoodwillData._goodwill = goodwill;

        ClientNetwork.Instance.ClientListener.EnqueuePacket(PacketHeader.GoodWillManager, factionGoodwillData);

        RT_Dialog_Wait d1 = new RT_Dialog_Wait("Changing settlement goodwill");
        RT_Dialog_Base.PushNewDialog(d1);
    }

    public static void ChangeStructureGoodwill(FactionGoodwillData data)
    {
        ChangeSettlementGoodwills(data);
        ChangeSiteGoodwills(data);
    }

    private static void ChangeSettlementGoodwills(FactionGoodwillData factionGoodwillData)
    {
        foreach (SettlementGoodwill _ in factionGoodwillData._settlements)
        {
            RTSettlement settlement = (RTSettlement)Find.WorldObjects.AllWorldObjects.First(fetch => fetch.Tile == _.Tile && fetch is RTSettlement);
            if (settlement.Faction == Faction.OfPlayer) continue;
            else
            {
                SettlementManager.PlayerSettlements.Remove(settlement);
                Find.WorldObjects.Remove(settlement);

                WorldObjectDef def = DefDatabase<WorldObjectDef>.AllDefs.First(fetch => fetch.defName == "RTSettlement");
                RTSettlement newSettlement = (RTSettlement)WorldObjectMaker.MakeWorldObject(def);
                newSettlement.Tile = settlement.Tile;
                newSettlement.Name = settlement.Name;
                newSettlement.SetFaction(PlanetManagerHelper.GetPlayerFactionFromGoodwill(_.Goodwill));

                SettlementManager.PlayerSettlements.Add(newSettlement);
                Find.WorldObjects.Add(newSettlement);
            }
        }
    }

    private static void ChangeSiteGoodwills(FactionGoodwillData factionGoodwillData)
    {
        foreach (SiteGoodwill _ in factionGoodwillData._sites) 
        {
            Site site = (Site)Find.WorldObjects.AllWorldObjects.First(fetch => fetch.Tile == _.Tile && fetch is Site);

            SiteManager.PlayerSites.Remove(site);
            Find.WorldObjects.Remove(site);

            Site newSite = SiteMaker.MakeSite(sitePart: site.MainSitePartDef, tile: site.Tile, threatPoints: 1000, 
                faction: PlanetManagerHelper.GetPlayerFactionFromGoodwill(_.Goodwill));

            SiteManager.PlayerSites.Add(newSite);
            Find.WorldObjects.Add(newSite);
        }
    }
}