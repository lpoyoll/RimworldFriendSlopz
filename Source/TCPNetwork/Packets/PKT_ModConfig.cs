using Shared.Files.Configs.Mods;
using static Shared.CommonEnumerators;

namespace TCPNetwork.Packets
{
    public class PKT_ModConfig : PKT_Base
    {
        public ModConfigStepMode _stepMode { get; set; } = ModConfigStepMode.Send;

        public ModsConfigFile _configFile { get; set; } = new ModsConfigFile();
    }
}