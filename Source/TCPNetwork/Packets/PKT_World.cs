using Shared.Files.Configs;

namespace TCPNetwork.Packets
{
    public class PKT_World : PKT_Base
    {
        public WorldStepMode _stepMode { get; set; } = WorldStepMode.AskFor;

        public enum WorldStepMode { AskFor, Required, Sent }

        public FL_PlanetConfig File { get; set; } = null;
    }
}
