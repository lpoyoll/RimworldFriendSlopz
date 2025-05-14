using GameServer.Core;
using GameServer.Files;
using Shared;

namespace GameServer.Misc
{
    public static class ValueChecker
    {
        public static bool CheckIfCanActivity(UserFile file)
        {
            if (!Master.ServerConfig.TemporalActivityProtection) return true;
            else if (!TimeConverter.CheckForEpochTimer(file.ActivityProtectionTime, Master.ServerConfig.TemporalActivityProtectionTime * 1000)) return false;
            else return true;
        }

        public static bool CheckIfCanEvent(UserFile file)
        {
            if (!Master.ServerConfig.TemporalEventProtection) return true;
            else if (!TimeConverter.CheckForEpochTimer(file.EventProtectionTime, Master.ServerConfig.TemporalEventProtectionTime * 1000)) return false;
            else return true;
        }

        public static bool CheckIfCanAid(UserFile file)
        {
            if (!Master.ServerConfig.TemporalAidProtection) return true;
            else if (!TimeConverter.CheckForEpochTimer(file.AidProtectionTime, Master.ServerConfig.TemporalAidProtectionTime * 1000)) return false;
            else return true;
        }

        public static bool CheckIfCanSpy(UserFile file)
        {
            if (!Master.ServerConfig.TemporalSpyProtection) return true;
            else if (!TimeConverter.CheckForEpochTimer(file.SpyProtectionTime, Master.ServerConfig.TemporalSpyProtectionTime * 1000)) return false;
            else return true;
        }
    }
}
