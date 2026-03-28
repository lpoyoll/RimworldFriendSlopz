using System.Collections.Generic;

namespace GameClient.Hooks.Synchronous
{
    public class PlayerJob
    {
        public string PawnID { get; set; } = string.Empty;

        public string PawnPosition { get; set; } = string.Empty;

        public string Job { get; set; } = string.Empty;

        public string TargetA { get; set; } = string.Empty;

        public string TargetB { get; set; } = string.Empty;

        public string TargetC { get; set; } = string.Empty;

        public List<string> QueueA { get; set; } = new List<string>();

        public List<string> QueueB { get; set; } = new List<string>();
    }
}
