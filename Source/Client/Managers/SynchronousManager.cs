using GameClient.Core;
using GameClient.Misc;
using HarmonyLib;
using Shared;
using Shared.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static Shared.Misc.Printer;

namespace GameClient.Managers
{
    public static class SynchronousManager
    {
        public static bool CheckIfShouldPatch(Map map)
        {
            if (SessionHandler.SynchronousMap.Tile != map.Tile) return false;
            else return true;
        }
    }
}
