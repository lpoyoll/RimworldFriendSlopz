using GameServer.Core;
using GameServer.Files;
using Shared;

namespace GameServer.Misc
{
    public static class ValueChecker
    {
        public static bool CheckIfCanActivity(UserFile file)
        {
            if (!Master.serverConfig.TemporalActivityProtection) return true;
            else if (!TimeConverter.CheckForEpochTimer(file.ActivityProtectionTime, Master.serverConfig.TemporalActivityProtectionTime * 1000)) return false;
            else return true;
        }

        public static bool CheckIfCanEvent(UserFile file)
        {
            if (!Master.serverConfig.TemporalEventProtection) return true;
            else if (!TimeConverter.CheckForEpochTimer(file.EventProtectionTime, Master.serverConfig.TemporalEventProtectionTime * 1000)) return false;
            else return true;
        }

        public static bool CheckIfCanAid(UserFile file)
        {
            if (!Master.serverConfig.TemporalAidProtection) return true;
            else if (!TimeConverter.CheckForEpochTimer(file.AidProtectionTime, Master.serverConfig.TemporalAidProtectionTime * 1000)) return false;
            else return true;
        }

        public static bool CheckIfCanSpy(UserFile file)
        {
            if (!Master.serverConfig.TemporalSpyProtection) return true;
            else if (!TimeConverter.CheckForEpochTimer(file.SpyProtectionTime, Master.serverConfig.TemporalSpyProtectionTime * 1000)) return false;
            else return true;
        }
    }
}
