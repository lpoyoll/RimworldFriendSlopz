using Shared.Files.Configs.Mods;
using static Shared.CommonEnumerators;

namespace TCPNetwork.Packets
{
    public class PKT_ModConfig : PKT_Base
    {
        public ModConfigStepMode _stepMode { get; set; } = ModConfigStepMode.Send;

        public ModConfigFile _configFile { get; set; } = new ModConfigFile();

        public enum ModConfigStepMode { Send, Ask }
    }
}