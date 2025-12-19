using Shared.Files.Sites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Files.Actions
{
    public class SiteAction : BaseAction
    {
        public bool IsEnabled { get; set; } = true;

        public double Cooldown { get; set; } = -1;

        public double TimeInterval { get; set; } = 1800000;

        public SiteType[] SiteTypes { get; set; } = new SiteType[]
        {
            new SiteType()
            {
                DefName = "RTFarmland",
                Cost = 500,
                Rewards =
                [
                    new SiteReward()
                    {
                        DefName = "RawRice",
                        Amount = 50
                    },
                    new SiteReward()
                    {
                        DefName = "RawCorn",
                        Amount = 50
                    },
                    new SiteReward()
                    {
                        DefName = "SmokeleafLeaves",
                        Amount = 25
                    },
                    new SiteReward()
                    {
                        DefName = "PsychoidLeaves",
                        Amount = 25
                    }
                ]
            },

            new SiteType()
            {
                DefName = "RTHunterCamp",
                Cost = 500,
                Rewards =
                [
                    new SiteReward()
                    {
                        DefName = "Meat_Muffalo",
                        Amount = 125
                    },
                    new SiteReward()
                    {
                        DefName = "Meat_Human",
                        Amount = 125
                    },
                    new SiteReward()
                    {
                        DefName = "Leather_Chinchilla",
                        Amount = 60
                    },
                    new SiteReward()
                    {
                        DefName = "Leather_Bear",
                        Amount = 60
                    },
                ]
            },

            new SiteType()
            {
                DefName = "RTQuarry",
                Cost = 500,
                Rewards =
                [
                    new SiteReward()
                    {
                        DefName = "BlocksGranite",
                        Amount = 50
                    },
                    new SiteReward()
                    {
                        DefName = "BlocksMarble",
                        Amount = 50
                    },
                    new SiteReward()
                    {
                        DefName = "Steel",
                        Amount = 30
                    },
                    new SiteReward()
                    {
                        DefName = "Plasteel",
                        Amount = 10
                    }
                ]
            },

            new SiteType()
            {
                DefName = "RTSawmill",
                Cost = 300,
                Rewards =
                [
                    new SiteReward()
                    {
                        DefName = "WoodLog",
                        Amount = 100
                    }
                ]
            },

            new SiteType()
            {
                DefName = "RTBank",
                Cost = 750,
                Rewards =
                [
                    new SiteReward()
                    {
                        DefName = "Silver",
                        Amount = 50
                    },
                    new SiteReward()
                    {
                        DefName = "Gold",
                        Amount = 15
                    }
                ]
            },

            new SiteType()
            {
                DefName = "RTLaboratory",
                Cost = 750,
                Rewards =
                    [
                    new SiteReward()
                    {
                        DefName = "ComponentIndustrial",
                        Amount = 10
                    },
                    new SiteReward()
                    {
                        DefName = "ComponentSpacer",
                        Amount = 2
                    },
                ]
            },

            new SiteType()
            {
                DefName = "RTRefinery",
                Cost = 750,
                Rewards =
                [
                    new SiteReward()
                    {
                        DefName = "Chemfuel",
                        Amount = 50
                    }
                ]
            },

            new SiteType()
            {
                DefName = "RTHerbalWorkshop",
                Cost = 750,
                Rewards =
                [
                    new SiteReward()
                    {
                        DefName = "MedicineHerbal",
                        Amount = 10
                    },
                    new SiteReward()
                    {
                        DefName = "MedicineIndustrial",
                        Amount = 2
                    }
                ]
            },

            new SiteType()
            {
                DefName = "RTTextileFactory",
                Cost = 750,
                Rewards =
                [
                    new SiteReward()
                    {
                        DefName = "Cloth",
                        Amount = 50
                    },
                    new SiteReward()
                    {
                        DefName = "DevilstrandCloth",
                        Amount = 30
                    }
                ]
            },

            new SiteType()
            {
                DefName = "RTFoodProcessor",
                Cost = 750,
                Rewards =
                [
                    new SiteReward()
                    {
                        DefName = "MealSurvivalPack",
                        Amount = 10
                    },
                    new SiteReward()
                    {
                        DefName = "MealNutrientPaste",
                        Amount = 30
                    }
                ]
            }
        };
    }
}
