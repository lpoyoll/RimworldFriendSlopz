using System.Collections.Generic;

namespace Shared.Files
{
    public class LeaderboardFile : BaseFile
    {
        public static string SavePath { get; set; } = string.Empty;
        
        public Dictionary<string, double> Scores { get; set; } = new Dictionary<string, double>();
    }
}
