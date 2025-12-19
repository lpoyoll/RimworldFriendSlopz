using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using GameClient.Managers;
using Shared;
using Shared.Files.Sites;

namespace GameClient.Dialogs;

public class RT_Dialog_SiteMenu_Info : RT_Dialog_Base
{
    public override Vector2 InitialSize => new Vector2(450f, 250f);

    private readonly SitePartDef SitePartDef;

    private readonly SiteType ConfigFile;

    private readonly Dictionary<ThingDef, int> CostThing = new Dictionary<ThingDef, int>();

    private readonly Dictionary<ThingDef, int> RewardThing = new Dictionary<ThingDef, int>();

    // todo check if unused after site rework
    private readonly bool IsInvalid;

    public static RT_Dialog_SiteMenu_Info Instance { get; private set; }

    public RT_Dialog_SiteMenu_Info(SitePartDef thingChosen)
    {
        SitePartDef = thingChosen;
        Title = thingChosen.label;
        ConfigFile = SiteManager.SiteValues.First(f => f.DefName == thingChosen.defName);
        Instance = this;

        ThingDef cost = DefDatabase<ThingDef>.GetNamed(ThingDefOf.Silver.defName);
        if (cost != null) CostThing.Add(cost, ConfigFile.Cost);

        for (int i = 0; i < ConfigFile.Rewards.Length; i++)
        {
            ThingDef reward = DefDatabase<ThingDef>.GetNamedSilentFail(ConfigFile.Rewards[i].DefName);
            if (reward != null) RewardThing.Add(reward, ConfigFile.Rewards[i].Amount);
            else Printer.Warning($"{ConfigFile.Rewards[i].DefName} could not be found and won't be added to the list. Double check the def exists.");
        }
    }

    public override void DoWindowContents(Rect mainRect)
    {
        if (IsInvalid)
        {
            PushNewDialog(new RT_Dialog_Message("ERROR", ["Site could not be loaded because of invalid configuration"]));
            Close();
        }

        Widgets.DrawLineHorizontal(mainRect.x, mainRect.y - 1, mainRect.width);
        Widgets.DrawLineHorizontal(mainRect.x, mainRect.yMax + 1, mainRect.width);

        if (Widgets.CloseButtonFor(mainRect)) Close();
        float centeredX = mainRect.width / 2;
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(centeredX - Text.CalcSize(Title).x / 2, mainRect.y, Text.CalcSize(Title).x, Text.CalcSize(Title).y), Title);

        Rect leftColumn = new Rect(mainRect.x, mainRect.y + 30f, mainRect.width / 2, mainRect.height - 20f);
        Widgets.DrawTextureFitted(leftColumn, SitePartDef.ExpandingIconTexture, 1f); // Icon of the site

        Rect rightColumn = new Rect(mainRect.width / 2, mainRect.y + 30f, mainRect.width / 2, mainRect.height - 70f);
        float heightDesc = Text.CalcHeight(SitePartDef.description, rightColumn.width - 16f) / 2 + 9f;
        float height = 40f + CostThing.Count() * 25f + RewardThing.Count() * 25f + heightDesc;
        Rect viewRightColumn = new Rect(rightColumn.x, rightColumn.y, rightColumn.width - 16f, height);

        Widgets.BeginScrollView(rightColumn, ref ScrollPosition, viewRightColumn);
        Text.Font = GameFont.Small;
        float num = viewRightColumn.y;

        Widgets.Label(new Rect(viewRightColumn.x, num, viewRightColumn.width, heightDesc), SitePartDef.description); // Description of site
        num += heightDesc;
        Widgets.Label(new Rect(viewRightColumn.x, num, viewRightColumn.width, 20f), "Cost:");
        num += 20f;

        foreach (ThingDef thing in CostThing.Keys)
        {
            Widgets.Label(new Rect(viewRightColumn.x, num, viewRightColumn.width, 25), $"- {thing.label} {CostThing[thing].ToString()}");
            num += 25;
        }

        Text.Font = GameFont.Small;
        Widgets.Label(new Rect(viewRightColumn.x, num, viewRightColumn.width, 20f), $"Produces:");
        num += 20f;

        foreach (ThingDef thing in RewardThing.Keys)
        {
            Widgets.Label(new Rect(viewRightColumn.x, num, viewRightColumn.width, 25), $"- {thing.label} {RewardThing[thing].ToString()} ");
            num += 25;
        }

        Widgets.EndScrollView();
        if (Widgets.ButtonText(new Rect(rightColumn.x + 5f, rightColumn.yMax, rightColumn.width - 10f, 40f), "Build"))
        {
            SiteManager.RequestSiteBuild(ConfigFile);
            RT_Dialog_SiteMenu.Instance.Close();
            Instance.Close();
        }
    }
}