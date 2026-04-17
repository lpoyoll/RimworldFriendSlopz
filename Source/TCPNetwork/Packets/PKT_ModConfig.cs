using Shared.Files.Configs;

namespace TCPNetwork.Packets
{
    public class PKT_ModConfig : PKT_Base
    {
        public ModConfigStepMode _stepMode { get; set; } = ModConfigStepMode.Send;

        public FL_ModConfig _configFile { get; set; } = new FL_ModConfig();

        public enum ModConfigStepMode { Send, Ask }
    }
}