using System;
using MessagePack;

namespace Shared
{
    [MessagePackObject]
    public class DifficultyData
    {
        public DifficultyValuesFile _values = new DifficultyValuesFile();
    }
}