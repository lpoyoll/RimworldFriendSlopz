namespace Shared
{
    public class VersionData
    {
        public enum VersionStep { Ask, Pass }

        public VersionStep _step;

        public string _version;
    }
}