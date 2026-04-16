namespace Shared.Files.Actions
{
    public class ACT_Base
    {
        public virtual bool IsEnabled { get; set; } = false;

        public virtual double Cooldown { get; set; } = -1;
    }
}
