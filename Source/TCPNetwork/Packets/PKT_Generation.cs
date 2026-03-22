using Shared.Files.Configs;
using static Shared.CommonEnumerators;

namespace TCPNetwork.Packets
{
    public class GameParameterData : PKT_Base
    {
        public GenStepMode _stepMode { get; set; } = GenStepMode.Scenario;

        public enum GenStepMode { Scenario, Storyteller, Difficulty }

        public byte[] _bytes { get; set; } = null;
    }
}