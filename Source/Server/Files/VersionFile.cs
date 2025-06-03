using Shared;

namespace GameServer.Files
{
    [Serializable]
    public class VersionFile
    {
        public string Version = CommonValues.ExecutableVersion;
    }
}