using Shared;
using Shared.Files.Actions;
using System;
using static System.Collections.Specialized.BitVector32;

namespace TCPNetwork.Files.Client
{
    public class PlayerCooldown
    {
        public DateTime RaidProtectionTime { get; set; } = DateTime.MinValue;

        public DateTime ZoomProtectionTime { get; set; } = DateTime.MinValue;

        public DateTime EventProtectionTime { get; set; } = DateTime.MinValue;

        public DateTime AidProtectionTime { get; set; } = DateTime.MinValue;

        public DateTime PollutionProtectionTime { get; set; } = DateTime.MinValue;

        public DateTime RoadProtectionTime { get; set; } = DateTime.MinValue;

        public DateTime NPCProtectionTime { get; set; } = DateTime.MinValue;

        public void SetEventTimer(UserFile file) 
        { 
            EventProtectionTime = DateTime.Now;
            file.SaveUserFile();
        }

        public void SetAidTimer(UserFile file) 
        { 
            AidProtectionTime = DateTime.Now; 
            file.SaveUserFile();
        }

        public void SetPollutionTimer(UserFile file)
        {
            PollutionProtectionTime = DateTime.Now;
            file.SaveUserFile();
        }

        public void SetRoadTimer(UserFile file)
        {
            RoadProtectionTime = DateTime.Now;
            file.SaveUserFile();
        }

        public void SetNPCTimer(UserFile file)
        {
            NPCProtectionTime = DateTime.Now;
            file.SaveUserFile();
        }

        public void SetRaidTimer(UserFile file)
        {
            RaidProtectionTime = DateTime.Now;
            file.SaveUserFile();
        }

        public void SetZoomTimer(UserFile file)
        {
            ZoomProtectionTime = DateTime.Now;
            file.SaveUserFile();
        }

        public static bool CheckIfCanRaid(UserFile file, ACT_Base action)
        {
            if (!action.IsEnabled) return false;
            else if (!TimeConverter.CompareTimes(file.Cooldowns.RaidProtectionTime, action.Cooldown)) return false;
            else return true;
        }

        public static bool CheckIfCanZoom(UserFile file, ACT_Base action)
        {
            if (!action.IsEnabled) return false;
            else if (!TimeConverter.CompareTimes(file.Cooldowns.ZoomProtectionTime, action.Cooldown)) return false;
            else return true;
        }

        public static bool CheckIfCanEvent(UserFile file, ACT_Base action)
        {
            if (!action.IsEnabled) return false;
            else if (!TimeConverter.CompareTimes(file.Cooldowns.EventProtectionTime, action.Cooldown)) return false;
            else return true;
        }

        public static bool CheckIfCanAid(UserFile file, ACT_Base action)
        {
            if (!action.IsEnabled) return false;
            else if (!TimeConverter.CompareTimes(file.Cooldowns.AidProtectionTime, action.Cooldown)) return false;
            else return true;
        }

        public static bool CheckIfCanPollute(UserFile file, ACT_Base action)
        {
            if (!action.IsEnabled) return false;
            else if (!TimeConverter.CompareTimes(file.Cooldowns.PollutionProtectionTime, action.Cooldown)) return false;
            else return true;
        }

        public static bool CheckIfCanRoad(UserFile file, ACT_Base action)
        {
            if (!action.IsEnabled) return false;
            else if (!TimeConverter.CompareTimes(file.Cooldowns.RoadProtectionTime, action.Cooldown)) return false;
            else return true;
        }

        public static bool CheckIfCanNPC(UserFile file, ACT_Base action)
        {
            if (!action.IsEnabled) return false;
            else if (!TimeConverter.CompareTimes(file.Cooldowns.NPCProtectionTime, action.Cooldown)) return false;
            else return true;
        }
    }
}
