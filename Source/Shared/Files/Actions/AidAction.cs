namespace Shared.Files.Actions
{
    public class AidAction : BaseAction
    {
        public bool IsEnabled { get; set; } = true;

        public double Cooldown { get; set; } = -1;
    }
}
