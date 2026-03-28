using GameClient.Misc;
using Verse;

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
