namespace Shared
{
    public class ModConfigFile
    {
        public string[] UnsortedMods = new string[0];

        public string[] RequiredMods = new string[0];

        public string[] RequiredModsByID = new string[0];

        public string[] OptionalMods = new string[0];

        public string[] OptionalModsByID = new string[0];

        public string[] ForbiddenMods = new string[0];

        public string[] ForbiddenModsByID = new string[0];

        public bool EnforcedConfigs = false;

        public string[] ModFileNames = new string[0];

        public string[] ModConfigs = new string[0];
    }
}