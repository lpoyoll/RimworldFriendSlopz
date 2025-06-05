namespace Shared
{
    #if SERVER
    using GameServer.Core;
    #endif

    public class WorldTilesFile
    {
        #if SERVER
        public static string FilePath => Path.Combine(Master.WorldPath, "WorldTilesFile.json");
        #endif

        public string[] TileData { get; set; } = null;
    }
}