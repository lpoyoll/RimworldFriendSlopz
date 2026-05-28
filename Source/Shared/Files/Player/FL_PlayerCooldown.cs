using Shared;
using Shared.Files.Actions;
using System;
using static System.Collections.Specialized.BitVector32;

namespace Shared.Files.ServerClient
{
    public class FL_PlayerCooldown
    {
        public DateTime RaidProtectionTime { get; set; } = DateTime.MinValue;

        public DateTime ZoomProtectionTime { get; set; } = DateTime.MinValue;

        public DateTime EventProtectionTime { get; set; } = DateTime.MinValue;

        public DateTime AidProtectionTime { get; set; } = DateTime.MinValue;

        public DateTime PollutionProtectionTime { get; set; } = DateTime.MinValue;

        public DateTime RoadProtectionTime { get; set; } = DateTime.MinValue;

        public DateTime NPCProtectionTime { get; set; } = DateTime.MinValue;

        public void SetEventTimer(FL_Player file) 
        { 
            EventProtectionTime = DateTime.Now;
            file.SaveUserFile();
        }

        public void SetAidTimer(FL_Player file) 
        { 
            AidProtectionTime = DateTime.Now; 
            file.SaveUserFile();
        }

        public void SetPollutionTimer(FL_Player file)
        {
            PollutionProtectionTime = DateTime.Now;
            file.SaveUserFile();
        }

        public void SetRoadTimer(FL_Player file)
        {
            RoadProtectionTime = DateTime.Now;
            file.SaveUserFile();
        }

        public void SetNPCTimer(FL_Player file)
        {
            NPCProtectionTime = DateTime.Now;
            file.SaveUserFile();
        }

        public void SetRaidTimer(FL_Player file)
        {
            RaidProtectionTime = DateTime.Now;
            file.SaveUserFile();
        }

        public void SetZoomTimer(FL_Player file)
        {
            ZoomProtectionTime = DateTime.Now;
            file.SaveUserFile();
        }

        public static bool CheckIfCanRaid(FL_Player file, ACT_Base action)
        {
            if (!action.IsEnabled) return false;
            else if (!TimeConverter.CompareTimes(file.Cooldowns.RaidProtectionTime, action.Cooldown)) return false;
            else return true;
        }

        public static bool CheckIfCanZoom(FL_Player file, ACT_Base action)
        {
            if (!action.IsEnabled) return false;
            else if (!TimeConverter.CompareTimes(file.Cooldowns.ZoomProtectionTime, action.Cooldown)) return false;
            else return true;
        }

        public static bool CheckIfCanEvent(FL_Player file, ACT_Base action)
        {
            if (!action.IsEnabled) return false;
            else if (!TimeConverter.CompareTimes(file.Cooldowns.EventProtectionTime, action.Cooldown)) return false;
            else return true;
        }

        public static bool CheckIfCanAid(FL_Player file, ACT_Base action)
        {
            if (!action.IsEnabled) return false;
            else if (!TimeConverter.CompareTimes(file.Cooldowns.AidProtectionTime, action.Cooldown)) return false;
            else return true;
        }

        public static bool CheckIfCanPollute(FL_Player file, ACT_Base action)
        {
            if (!action.IsEnabled) return false;
            else if (!TimeConverter.CompareTimes(file.Cooldowns.PollutionProtectionTime, action.Cooldown)) return false;
            else return true;
        }

        public static bool CheckIfCanRoad(FL_Player file, ACT_Base action)
        {
            if (!action.IsEnabled) return false;
            else if (!TimeConverter.CompareTimes(file.Cooldowns.RoadProtectionTime, action.Cooldown)) return false;
            else return true;
        }

        public static bool CheckIfCanNPC(FL_Player file, ACT_Base action)
        {
            if (!action.IsEnabled) return false;
            else if (!TimeConverter.CompareTimes(file.Cooldowns.NPCProtectionTime, action.Cooldown)) return false;
            else return true;
        }
    }
}
