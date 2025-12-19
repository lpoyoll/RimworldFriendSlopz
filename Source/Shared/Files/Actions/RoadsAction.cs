using Shared.Files.Configs;

namespace Shared.Files.Actions;

public class RoadsAction : BaseAction
{
    public bool IsEnabled { get; set; } = true;

    public double Cooldown { get; set; } = -1;

    public RoadsConfigFile RoadValues { get; set; } = new RoadsConfigFile();
}