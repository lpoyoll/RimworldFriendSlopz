using static Shared.CommonEnumerators;

namespace Shared
{

    public class SaveData
    {
        public SaveStepMode _stepMode { get; set; }

        public byte[] _fileBytes { get; set; }

        public int _instructions { get; set; } = -1;
    }
}
