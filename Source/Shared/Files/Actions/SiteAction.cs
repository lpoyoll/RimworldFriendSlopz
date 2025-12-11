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

        public double TimeIntervalMinutes { get; set; } = 30;

        public SiteInfoFile[] SiteTypes { get; set; } = new SiteInfoFile[]
        {
            new SiteInfoFile()
            {
                DefName = "RTFarmland",
                Cost = 500,
                Rewards =
                [
                    new SiteRewardFile()
                    {
                        DefName = "RawRice",
                        Amount = 50
                    },
                    new SiteRewardFile()
                    {
                        DefName = "RawCorn",
                        Amount = 50
                    },
                    new SiteRewardFile()
                    {
                        DefName = "SmokeleafLeaves",
                        Amount = 25
                    },
                    new SiteRewardFile()
                    {
                        DefName = "PsychoidLeaves",
                        Amount = 25
                    }
                ]
            },

            new SiteInfoFile()
            {
                DefName = "RTHunterCamp",
                Cost = 500,
                Rewards =
                [
                    new SiteRewardFile()
                    {
                        DefName = "Meat_Muffalo",
                        Amount = 125
                    },
                    new SiteRewardFile()
                    {
                        DefName = "Meat_Human",
                        Amount = 125
                    },
                    new SiteRewardFile()
                    {
                        DefName = "Leather_Chinchilla",
                        Amount = 60
                    },
                    new SiteRewardFile()
                    {
                        DefName = "Leather_Bear",
                        Amount = 60
                    },
                ]
            },

            new SiteInfoFile()
            {
                DefName = "RTQuarry",
                Cost = 500,
                Rewards =
                [
                    new SiteRewardFile()
                    {
                        DefName = "BlocksGranite",
                        Amount = 50
                    },
                    new SiteRewardFile()
                    {
                        DefName = "BlocksMarble",
                        Amount = 50
                    },
                    new SiteRewardFile()
                    {
                        DefName = "Steel",
                        Amount = 30
                    },
                    new SiteRewardFile()
                    {
                        DefName = "Plasteel",
                        Amount = 10
                    }
                ]
            },

            new SiteInfoFile()
            {
                DefName = "RTSawmill",
                Cost = 300,
                Rewards =
                [
                    new SiteRewardFile()
                    {
                        DefName = "WoodLog",
                        Amount = 100
                    }
                ]
            },

            new SiteInfoFile()
            {
                DefName = "RTBank",
                Cost = 750,
                Rewards =
                [
                    new SiteRewardFile()
                    {
                        DefName = "Silver",
                        Amount = 50
                    },
                    new SiteRewardFile()
                    {
                        DefName = "Gold",
                        Amount = 15
                    }
                ]
            },

            new SiteInfoFile()
            {
                DefName = "RTLaboratory",
                Cost = 750,
                Rewards =
                    [
                    new SiteRewardFile()
                    {
                        DefName = "ComponentIndustrial",
                        Amount = 10
                    },
                    new SiteRewardFile()
                    {
                        DefName = "ComponentSpacer",
                        Amount = 2
                    },
                ]
            },

            new SiteInfoFile()
            {
                DefName = "RTRefinery",
                Cost = 750,
                Rewards =
                [
                    new SiteRewardFile()
                    {
                        DefName = "Chemfuel",
                        Amount = 50
                    }
                ]
            },

            new SiteInfoFile()
            {
                DefName = "RTHerbalWorkshop",
                Cost = 750,
                Rewards =
                [
                    new SiteRewardFile()
                    {
                        DefName = "MedicineHerbal",
                        Amount = 10
                    },
                    new SiteRewardFile()
                    {
                        DefName = "MedicineIndustrial",
                        Amount = 2
                    }
                ]
            },

            new SiteInfoFile()
            {
                DefName = "RTTextileFactory",
                Cost = 750,
                Rewards =
                [
                    new SiteRewardFile()
                    {
                        DefName = "Cloth",
                        Amount = 50
                    },
                    new SiteRewardFile()
                    {
                        DefName = "DevilstrandCloth",
                        Amount = 30
                    }
                ]
            },

            new SiteInfoFile()
            {
                DefName = "RTFoodProcessor",
                Cost = 750,
                Rewards =
                [
                    new SiteRewardFile()
                    {
                        DefName = "MealSurvivalPack",
                        Amount = 10
                    },
                    new SiteRewardFile()
                    {
                        DefName = "MealNutrientPaste",
                        Amount = 30
                    }
                ]
            }
        };
    }
}
