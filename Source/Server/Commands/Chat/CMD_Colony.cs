using RTServer.Hooks.TCPNetwork;
using RTServer.Managers;
using RTServer.PacketManagers;
using RTShared.Commands;
using RTShared.Files;
using RTShared.Files.Player;
using RTNetwork.Components;

namespace RTServer.Commands.Chat
{
    public class CMD_Colony : CMD_Base
    {
        public CMD_Colony()
        {
            Prefix = "/colony";
            Description = "Shows shared-colony status or sets a player relationship";
            IsChatCommand = true;
        }

        public override void Action()
        {
            ServerClient client = PM_Chat.TargetClient;
            if (client == null) return;

            string[] command = PM_Chat.LatestCommand ?? Array.Empty<string>();
            if (command.Length == 1 || command[1].Equals("status", StringComparison.OrdinalIgnoreCase))
            {
                ShowStatus(client);
                return;
            }

            if (command.Length == 4 && command[1].Equals("relation", StringComparison.OrdinalIgnoreCase))
            {
                SetRelation(client, command[2].TrimStart('@'), command[3]);
                return;
            }

            // Used by the companion patch immediately before the stock
            // synchronous request. This disambiguates overlapping world
            // objects without changing RTNetwork's packet schema.
            if (command.Length == 3 && command[1].Equals("target", StringComparison.OrdinalIgnoreCase))
            {
                string target = command[2].TrimStart('@');
                FL_Player targetFile = UserManagerH.GetAllUserFiles().FirstOrDefault(fetch =>
                    fetch.Username.Equals(target, StringComparison.OrdinalIgnoreCase));
                if (targetFile == null || targetFile.Username == client.GetData<FL_Player>().Username)
                {
                    PM_Chat.SendConsoleMessage(client, "Shared-colony target was not found.");
                    return;
                }

                SharedSessionManager.SetNextTarget(client, targetFile.Username);
                return;
            }

            PM_Chat.SendConsoleMessage(client, "Usage: /colony status");
            PM_Chat.SendConsoleMessage(client, "Usage: /colony relation @player neutral|support|ally|hostile");
        }

        private static void ShowStatus(ServerClient client)
        {
            string username = client.GetData<FL_Player>().Username;
            FL_Settlement[] ownedSettlements = PM_Settlements.GetAllSettlementsFromUsername(username);
            if (ownedSettlements.Length == 0)
            {
                PM_Chat.SendConsoleMessage(client, "You do not currently own a settlement.");
                return;
            }

            foreach (FL_Settlement owned in ownedSettlements)
            {
                List<FL_Settlement> occupants = PM_Settlements.GetAllSettlementsAtTile(owned.Tile);
                PM_Chat.SendConsoleMessage(client,
                    $"Tile {owned.Tile}: {occupants.Count}/{Math.Clamp(RTServer.Core.Master.ServerConfig.SharedColonyTileCapacity, 1, 8)} settlements; " +
                    $"map host: {SharedColonyManager.GetMapHostUsername(owned.Tile)}.");

                foreach (FL_Settlement occupant in occupants.Where(fetch => fetch.Username != username))
                {
                    PM_Chat.SendConsoleMessage(client,
                        $"- {occupant.Username}: declared {SharedColonyManager.GetDeclaredStance(username, occupant.Username)}, " +
                        $"effective {SharedColonyManager.GetEffectiveStance(username, occupant.Username)}");
                }
            }
        }

        private static void SetRelation(ServerClient client, string requestedUsername, string requestedStance)
        {
            string source = client.GetData<FL_Player>().Username;
            FL_Player targetFile = UserManagerH.GetAllUserFiles().FirstOrDefault(fetch =>
                fetch.Username.Equals(requestedUsername, StringComparison.OrdinalIgnoreCase));

            if (targetFile == null || targetFile.Username == source)
            {
                PM_Chat.SendConsoleMessage(client, "That player was not found.");
                return;
            }

            if (!Enum.TryParse(requestedStance, true, out SharedColonyStance stance))
            {
                PM_Chat.SendConsoleMessage(client, "Relationship must be neutral, support, ally or hostile.");
                return;
            }

            SharedColonyManager.SetDeclaredStance(source, targetFile.Username, stance);
            SharedColonyStance effective = SharedColonyManager.GetEffectiveStance(source, targetFile.Username);
            PM_Chat.SendConsoleMessage(client,
                $"Your stance towards {targetFile.Username} is now {stance}. Effective relationship: {effective}.");

            ServerClient targetClient = ServerNetwork.GetConnectedClientFromUsername(targetFile.Username);
            if (targetClient != null)
            {
                PM_Chat.SendServerMessage(targetClient,
                    $"{source} changed their stance towards you to {stance}. Effective relationship: {effective}.");
            }
        }
    }
}
