using MessagePack;

namespace Shared
{
    [MessagePackObject]
    public class VersionData
    {
        public enum VersionStep { Ask, Pass }

        [Key(0)]
        public VersionStep _step;

        [Key(1)]
        public string _version;
    }
}