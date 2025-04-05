using MessagePack;
using static Shared.CommonEnumerators;

namespace Shared
{
    [MessagePackObject]
    public class ResponseShortcutData
    {
        public ResponseStepMode stepMode;
    }
}