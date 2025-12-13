using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPNetwork.Files.Client
{
    public class PlayerCooldown
    {
        public double EventProtectionTime { get; set; } = -1;

        public double AidProtectionTime { get; set; } = -1;

        public void SetEventTimer(double value, UserFile file) 
        { 
            EventProtectionTime = value;
            file.SaveUserFile();
        }

        public void SetAidTimer(double value, UserFile file) 
        { 
            AidProtectionTime = value; 
            file.SaveUserFile();
        }

        public static bool CheckIfCanEvent(UserFile file, bool isEnabled, double baseTimer)
        {
            if (!isEnabled) return false;
            else if (!TimeConverter.CheckForEpochTimer(file.Cooldowns.EventProtectionTime, baseTimer * 1000)) return false;
            else return true;
        }

        public static bool CheckIfCanAid(UserFile file, bool isEnabled, double baseTimer)
        {
            if (!isEnabled) return false;
            else if (!TimeConverter.CheckForEpochTimer(file.Cooldowns.AidProtectionTime, baseTimer * 1000)) return false;
            else return true;
        }
    }
}
