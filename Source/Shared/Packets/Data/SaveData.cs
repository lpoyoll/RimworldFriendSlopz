using static Shared.CommonEnumerators;

namespace Shared
{

    public class SaveData
    {
        public SaveStepMode _stepMode { get; set; } = SaveStepMode.Send;

        public byte[] _fileBytes { get; set; } = null;

        public int _instructions { get; set; } = -1;
    }
}
