using Shared.Files.Configs.Mods;
using static Shared.CommonEnumerators;

namespace TCPNetwork.Packets;

public class ModConfigData
{
    public ModConfigStepMode _stepMode { get; set; } = ModConfigStepMode.Send;

    public ModsConfigFile _configFile { get; set; } = new ModsConfigFile();

    public override string ToString()
    {
        return $"ModConfigData|{_stepMode}|{_configFile}";
    }
}