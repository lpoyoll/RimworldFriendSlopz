namespace Shared.Files.Actions
{
    public class ACT_Zoom : ACT_Base
    {
        public override bool IsEnabled { get; set; } = true;

        public override double Cooldown { get; set; } = 1000;
    }
}
