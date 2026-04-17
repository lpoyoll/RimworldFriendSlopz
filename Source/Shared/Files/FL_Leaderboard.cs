using System.Collections.Generic;

namespace Shared.Files
{
    public class FL_Leaderboard : FL_Base
    {
        public static string SavePath { get; set; } = string.Empty;
        
        public Dictionary<string, double> Scores { get; set; } = new Dictionary<string, double>();
    }
}
