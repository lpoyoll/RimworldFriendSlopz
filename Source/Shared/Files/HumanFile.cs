using System;

namespace Shared
{
    [Serializable]

    public class HumanFile
    {
        public string ID { get; set; }

        public string ScribeData { get; set; }

        public IdeologyFile Ideology { get; set; } 

        public override string ToString()
        {
            return $"HumanFile:|{ID}|{ScribeData?.Length ?? 0}";
        }
    }
}