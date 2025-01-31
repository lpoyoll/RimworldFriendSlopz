using System;
using static Shared.CommonEnumerators;

namespace Shared
{
    [Serializable]
    public class HediffOrderData
    {
        public OnlineActivityTargetFaction _pawnFaction;

        public OnlineActivityApplyMode _applyMode;

        public HediffDetails _hediffComponent = new HediffDetails();

        public string targetID;
    }
}