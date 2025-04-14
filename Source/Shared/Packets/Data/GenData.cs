using static Shared.CommonEnumerators;

namespace Shared
{

    public class GameParameterData
    {
        public GenStepMode _stepMode { get; set; }

        public ScenarioValuesFile _scenario { get; set; }

        public StorytellerValuesFile _storyteller { get; set; }

        public DifficultyValuesFile _difficulty { get; set; }
    }
}