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
                        RewardDef = "RawRice",
                        RewardAmount = 50
                    },
                    new SiteRewardFile()
                    {
                        RewardDef = "RawCorn",
                        RewardAmount = 50
                    },
                    new SiteRewardFile()
                    {
                        RewardDef = "SmokeleafLeaves",
                        RewardAmount = 25
                    },
                    new SiteRewardFile()
                    {
                        RewardDef = "PsychoidLeaves",
                        RewardAmount = 25
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
                        RewardDef = "Meat_Muffalo",
                        RewardAmount = 125
                    },
                    new SiteRewardFile()
                    {
                        RewardDef = "Meat_Human",
                        RewardAmount = 125
                    },
                    new SiteRewardFile()
                    {
                        RewardDef = "Leather_Chinchilla",
                        RewardAmount = 60
                    },
                    new SiteRewardFile()
                    {
                        RewardDef = "Leather_Bear",
                        RewardAmount = 60
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
                        RewardDef = "BlocksGranite",
                        RewardAmount = 50
                    },
                    new SiteRewardFile()
                    {
                        RewardDef = "BlocksMarble",
                        RewardAmount = 50
                    },
                    new SiteRewardFile()
                    {
                        RewardDef = "Steel",
                        RewardAmount = 30
                    },
                    new SiteRewardFile()
                    {
                        RewardDef = "Plasteel",
                        RewardAmount = 10
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
                        RewardDef = "WoodLog",
                        RewardAmount = 100
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
                        RewardDef = "Silver",
                        RewardAmount = 50
                    },
                    new SiteRewardFile()
                    {
                        RewardDef = "Gold",
                        RewardAmount = 15
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
                        RewardDef = "ComponentIndustrial",
                        RewardAmount = 10
                    },
                    new SiteRewardFile()
                    {
                        RewardDef = "ComponentSpacer",
                        RewardAmount = 2
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
                        RewardDef = "Chemfuel",
                        RewardAmount = 50
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
                        RewardDef = "MedicineHerbal",
                        RewardAmount = 10
                    },
                    new SiteRewardFile()
                    {
                        RewardDef = "MedicineIndustrial",
                        RewardAmount = 2
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
                        RewardDef = "Cloth",
                        RewardAmount = 50
                    },
                    new SiteRewardFile()
                    {
                        RewardDef = "DevilstrandCloth",
                        RewardAmount = 30
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
                        RewardDef = "MealSurvivalPack",
                        RewardAmount = 10
                    },
                    new SiteRewardFile()
                    {
                        RewardDef = "MealNutrientPaste",
                        RewardAmount = 30
                    }
                ]
            }
        };
    }
}
