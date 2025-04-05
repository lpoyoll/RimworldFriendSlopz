using MessagePack;
using static Shared.CommonEnumerators;

namespace Shared
{
    [MessagePackObject]
    public class GameParameterData
    {
        public GenStepMode _stepMode;

        public ScenarioValuesFile _scenario;

        public StorytellerValuesFile _storyteller;

        public DifficultyValuesFile _difficulty;
    }
}