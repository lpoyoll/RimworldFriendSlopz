namespace Shared
{
    public class HediffDetails
    {
        public string DefName { get; set; }

        public string PartDefName { get; set; }

        public string PartLabel { get; set; }

        public string WeaponDefName { get; set; }

        public float Severity { get; set; }

        public float Immunity { get; set; }

        public float TendQuality { get; set; }

        public float TotalTendQuality { get; set; }

        public int TendDuration { get; set; }

        public bool IsPermanent { get; set; }
    }
}