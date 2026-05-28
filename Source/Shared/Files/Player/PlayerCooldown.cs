using Shared;
using Shared.Files.Actions;
using System;
using static System.Collections.Specialized.BitVector32;

namespace Shared.Files.ServerClient
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

        public void SetEventTimer(PlayerFile file) 
        { 
            EventProtectionTime = DateTime.Now;
            file.SaveUserFile();
        }

        public void SetAidTimer(PlayerFile file) 
        { 
            AidProtectionTime = DateTime.Now; 
            file.SaveUserFile();
        }

        public void SetPollutionTimer(PlayerFile file)
        {
            PollutionProtectionTime = DateTime.Now;
            file.SaveUserFile();
        }

        public void SetRoadTimer(PlayerFile file)
        {
            RoadProtectionTime = DateTime.Now;
            file.SaveUserFile();
        }

        public void SetNPCTimer(PlayerFile file)
        {
            NPCProtectionTime = DateTime.Now;
            file.SaveUserFile();
        }

        public void SetRaidTimer(PlayerFile file)
        {
            RaidProtectionTime = DateTime.Now;
            file.SaveUserFile();
        }

        public void SetZoomTimer(PlayerFile file)
        {
            ZoomProtectionTime = DateTime.Now;
            file.SaveUserFile();
        }

        public static bool CheckIfCanRaid(PlayerFile file, ACT_Base action)
        {
            if (!action.IsEnabled) return false;
            else if (!TimeConverter.CompareTimes(file.Cooldowns.RaidProtectionTime, action.Cooldown)) return false;
            else return true;
        }

        public static bool CheckIfCanZoom(PlayerFile file, ACT_Base action)
        {
            if (!action.IsEnabled) return false;
            else if (!TimeConverter.CompareTimes(file.Cooldowns.ZoomProtectionTime, action.Cooldown)) return false;
            else return true;
        }

        public static bool CheckIfCanEvent(PlayerFile file, ACT_Base action)
        {
            if (!action.IsEnabled) return false;
            else if (!TimeConverter.CompareTimes(file.Cooldowns.EventProtectionTime, action.Cooldown)) return false;
            else return true;
        }

        public static bool CheckIfCanAid(PlayerFile file, ACT_Base action)
        {
            if (!action.IsEnabled) return false;
            else if (!TimeConverter.CompareTimes(file.Cooldowns.AidProtectionTime, action.Cooldown)) return false;
            else return true;
        }

        public static bool CheckIfCanPollute(PlayerFile file, ACT_Base action)
        {
            if (!action.IsEnabled) return false;
            else if (!TimeConverter.CompareTimes(file.Cooldowns.PollutionProtectionTime, action.Cooldown)) return false;
            else return true;
        }

        public static bool CheckIfCanRoad(PlayerFile file, ACT_Base action)
        {
            if (!action.IsEnabled) return false;
            else if (!TimeConverter.CompareTimes(file.Cooldowns.RoadProtectionTime, action.Cooldown)) return false;
            else return true;
        }

        public static bool CheckIfCanNPC(PlayerFile file, ACT_Base action)
        {
            if (!action.IsEnabled) return false;
            else if (!TimeConverter.CompareTimes(file.Cooldowns.NPCProtectionTime, action.Cooldown)) return false;
            else return true;
        }
    }
}
