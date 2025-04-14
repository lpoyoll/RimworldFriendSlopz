using System.Collections.Generic;
using static Shared.CommonEnumerators;

namespace Shared
{

    public class TransferData
    {
        public TransferStepMode _stepMode { get; set; }

        public TransferMode _transferMode { get; set; }

        public int _fromTile { get; set; }

        public int _toTile { get; set; }

        public List<HumanFile> _humans { get; set; } = new List<HumanFile>();

        public List<AnimalFile> _animals { get; set; } = new List<AnimalFile>();

        public List<ThingFile> _things { get; set; } = new List<ThingFile>();
    }
}
