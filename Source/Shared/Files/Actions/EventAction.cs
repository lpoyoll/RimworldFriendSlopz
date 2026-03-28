namespace Shared.Files.Actions
{
    public class EventAction : BaseAction
    {
        public bool IsEnabled { get; set; } = true;

        public double Cooldown { get; set; } = 3600;
    }
}
