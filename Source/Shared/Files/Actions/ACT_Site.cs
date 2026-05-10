using Shared.Files.Sites;
using System.Collections.Generic;

namespace Shared.Files.Actions
{
    public class ACT_Site : ACT_Base
    {
        public override bool IsEnabled { get; set; } = true;

        public override double Cooldown { get; set; } = -1;

        public double TimeInterval { get; set; } = 1800000;

        public List<FL_SiteType> SiteTypes { get; set; } = new List<FL_SiteType>()
        {
            new FL_SiteType()
            {
                DefName = "RTFarmland",
                Cost = 500,
                Rewards = new FL_SiteReward[] {

                    new FL_SiteReward()
                    {
                        DefName = "RawRice",
                        Amount = 50
                    },
                    new FL_SiteReward()
                    {
                        DefName = "RawCorn",
                        Amount = 50
                    },
                    new FL_SiteReward()
                    {
                        DefName = "SmokeleafLeaves",
                        Amount = 25
                    },
                    new FL_SiteReward()
                    {
                        DefName = "PsychoidLeaves",
                        Amount = 25
                    }
                }
            },

            new FL_SiteType()
            {
                DefName = "RTHunterCamp",
                Cost = 500,
                Rewards = new FL_SiteReward[] {
                    new FL_SiteReward()
                    {
                        DefName = "Meat_Muffalo",
                        Amount = 125
                    },
                    new FL_SiteReward()
                    {
                        DefName = "Meat_Human",
                        Amount = 125
                    },
                    new FL_SiteReward()
                    {
                        DefName = "Leather_Chinchilla",
                        Amount = 60
                    },
                    new FL_SiteReward()
                    {
                        DefName = "Leather_Bear",
                        Amount = 60
                    }
                }
            },

            new FL_SiteType()
            {
                DefName = "RTQuarry",
                Cost = 500,
                Rewards = new FL_SiteReward[] {
                    new FL_SiteReward()
                    {
                        DefName = "BlocksGranite",
                        Amount = 50
                    },
                    new FL_SiteReward()
                    {
                        DefName = "BlocksMarble",
                        Amount = 50
                    },
                    new FL_SiteReward()
                    {
                        DefName = "Steel",
                        Amount = 30
                    },
                    new FL_SiteReward()
                    {
                        DefName = "Plasteel",
                        Amount = 10
                    }
                }
            },

            new FL_SiteType()
            {
                DefName = "RTSawmill",
                Cost = 300,
                Rewards = new FL_SiteReward[] {

                    new FL_SiteReward()
                    {
                        DefName = "WoodLog",
                        Amount = 100
                    }
                }
            },

            new FL_SiteType()
            {
                DefName = "RTBank",
                Cost = 750,
                Rewards = new FL_SiteReward[] {

                    new FL_SiteReward()
                    {
                        DefName = "Silver",
                        Amount = 50
                    },
                    new FL_SiteReward()
                    {
                        DefName = "Gold",
                        Amount = 15
                    }
                }
            },

            new FL_SiteType()
            {
                DefName = "RTLaboratory",
                Cost = 750,
                Rewards = new FL_SiteReward[] {

                    new FL_SiteReward()
                    {
                        DefName = "ComponentIndustrial",
                        Amount = 10
                    },
                    new FL_SiteReward()
                    {
                        DefName = "ComponentSpacer",
                        Amount = 2
                    }
                }
            },

            new FL_SiteType()
            {
                DefName = "RTRefinery",
                Cost = 750,
                Rewards = new FL_SiteReward[] {

                    new FL_SiteReward()
                    {
                        DefName = "Chemfuel",
                        Amount = 50
                    }
                }
            },

            new FL_SiteType()
            {
                DefName = "RTHerbalWorkshop",
                Cost = 750,
                Rewards = new FL_SiteReward[] {

                    new FL_SiteReward()
                    {
                        DefName = "MedicineHerbal",
                        Amount = 10
                    },
                    new FL_SiteReward()
                    {
                        DefName = "MedicineIndustrial",
                        Amount = 2
                    }
                }
            },

            new FL_SiteType()
            {
                DefName = "RTTextileFactory",
                Cost = 750,
                Rewards = new FL_SiteReward[] {

                    new FL_SiteReward()
                    {
                        DefName = "Cloth",
                        Amount = 50
                    },
                    new FL_SiteReward()
                    {
                        DefName = "DevilstrandCloth",
                        Amount = 30
                    }
                }
            },

            new FL_SiteType()
            {
                DefName = "RTFoodProcessor",
                Cost = 750,
                Rewards = new FL_SiteReward[] {

                    new FL_SiteReward()
                    {
                        DefName = "MealSurvivalPack",
                        Amount = 10
                    },
                    new FL_SiteReward()
                    {
                        DefName = "MealNutrientPaste",
                        Amount = 30
                    }
                }
            }
        };
    }
}
