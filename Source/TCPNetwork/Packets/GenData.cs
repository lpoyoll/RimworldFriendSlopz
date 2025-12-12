using Shared.Files.Configs;
using static Shared.CommonEnumerators;

namespace TCPNetwork.Packets
{
    public class GameParameterData
    {
        public GenStepMode _stepMode { get; set; } = GenStepMode.Scenario;

        public ScenarioConfigFile _scenario { get; set; } = null;

        public StorytellerConfigFile _storyteller { get; set; } = null;

        public DifficultyConfigFile _difficulty { get; set; } = null;

        public override string ToString()
        {
            return $"GameParameterData:|{_stepMode}|{_scenario}|{_storyteller}|{_difficulty}";
        }
    }
}