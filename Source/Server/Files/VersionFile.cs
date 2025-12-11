using Shared;

namespace GameServer.Files
{
    public class VersionFile
    {
        public string Version { get; set; } = CommonValues.ExecutableVersion;
    }
}