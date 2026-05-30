using Verse;

namespace GameClient.Managers
{
    public static class SynchronousManager
    {
        public static bool CheckIfShouldPatch(Map map)
        {
            if (SessionManager.SynchronousMap.Tile != map.Tile) return false;
            else return true;
        }
    }
}
