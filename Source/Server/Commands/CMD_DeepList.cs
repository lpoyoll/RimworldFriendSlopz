using GameServer.Managers;
using Shared;
using Shared.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCPNetwork.Files.Client;

namespace GameServer.Commands
{
    public class CMD_DeepList : CMD_Base
    {
        public CMD_DeepList()
        {
            Prefix = "deeplist";
            Description = "Shows a list of all server players";
            ParameterCount = 0;
        }

        public override void Action() 
        {
            UserFile[] userFiles = UserManagerH.GetAllUserFiles();

            Printer.Title($"Server players: [{userFiles.Count()}]");
            Printer.Title("----------------------------------------");
            foreach (UserFile user in userFiles) Printer.Warning($"{user.Username}");
            Printer.Title("----------------------------------------");
        }
    }
}
