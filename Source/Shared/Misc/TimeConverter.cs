using System;

namespace Shared
{
    public static class TimeConverter
    {
        public static bool CompareTimes(DateTime toCompare, double extraValue)
        {
            if (DateTime.Now > toCompare.AddMilliseconds(extraValue)) return true;
            else return false;
        }
    }
}