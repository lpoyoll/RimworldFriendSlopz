using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using RTNetwork.Components;
using RTServer.Core;
using RTServer.Hooks.TCPNetwork;
using RTServer.PacketManagers;
using RTShared.Files;
using RTShared.Files.Player;
using RTShared.Misc;

namespace RTServer.Managers
{
    public enum SharedColonyStance
    {
        Neutral,
        Support,
        Ally,
        Hostile
    }

    public sealed class SharedColonyTile
    {
        public int Tile { get; set; }

        public int MapSize { get; set; }

        public string MapHostUsername { get; set; }

        public List<string> Members { get; set; } = new List<string>();
    }

    public sealed class SharedColonyRelation
    {
        public string SourceUsername { get; set; }

        public string TargetUsername { get; set; }

        public SharedColonyStance Stance { get; set; } = SharedColonyStance.Neutral;
    }

    /// <summary>
    /// Owns the server-side rules for putting several independently owned
    /// colonies on one world tile. The existing network packets remain valid;
    /// the companion client patch adds an explicit target username when two
    /// settlements share a tile.
    /// </summary>
    public static class SharedColonyManager
    {
        public const string ProtocolUsername = "RWT_SHARED";

        public const string ProtocolPrefix = "[RWT_SHARED]";

        private static readonly object StateLock = new object();

        private static string RelationsPath => Path.Combine(Master.SharedColoniesPath, "Relations.json");

        public static bool Enabled => Master.ServerConfig?.EnableSharedColonyTiles == true;

        public static string GetSettlementPath(int tile, string username)
        {
            byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(username.ToLowerInvariant()));
            string suffix = Convert.ToHexString(digest).Substring(0, 16).ToLowerInvariant();
            return Path.Combine(Master.SettlementsPath, $"{tile}--{suffix}{CommonValues.DefaultSaveFormat}");
        }

        public static string FindSettlementPath(int tile, string username)
        {
            foreach (string path in Directory.GetFiles(Master.SettlementsPath))
            {
                try
                {
                    FL_Settlement settlement = Serializer.SerializeFromFile<FL_Settlement>(path);
                    if (settlement.Tile == tile && settlement.Username == username) return path;
                }
                catch (Exception ex)
                {
                    Printer.Warning($"Unable to inspect settlement file '{path}': {ex.Message}");
                }
            }

            return null;
        }

        public static bool CanAddSettlement(int tile, string username, out string reason)
        {
            reason = null;
            if (!Enabled)
            {
                if (PM_Settlements.CheckIfTileIsInUse(tile)) reason = "That tile already contains a settlement.";
                return reason == null;
            }

            List<FL_Settlement> occupants = PM_Settlements.GetAllSettlementsAtTile(tile);
            if (occupants.Any(fetch => fetch.Username == username))
            {
                reason = "You already own a settlement on that tile.";
                return false;
            }

            int capacity = Math.Clamp(Master.ServerConfig.SharedColonyTileCapacity, 1, 8);
            if (occupants.Count >= capacity)
            {
                reason = $"That shared colony tile is full ({capacity} settlements).";
                return false;
            }

            if (occupants.Count > 0 && !MapHasConfiguredSize(tile))
            {
                reason = $"The existing map is not {Master.ServerConfig.SharedColonyMapSize}x{Master.ServerConfig.SharedColonyMapSize}. " +
                    "Only maps created with the Shared Colony client patch can accept another settlement.";
                return false;
            }

            return true;
        }

        public static void RegisterSettlement(FL_Settlement settlement)
        {
            if (!Enabled) return;

            lock (StateLock)
            {
                SharedColonyTile state = LoadTile(settlement.Tile) ?? CreateTileState(settlement.Tile);
                if (!state.Members.Contains(settlement.Username)) state.Members.Add(settlement.Username);
                state.MapHostUsername ??= settlement.Username;
                state.MapSize = Master.ServerConfig.SharedColonyMapSize;
                SaveTile(state);
            }

            BroadcastSnapshots();
        }

        public static void UnregisterSettlement(FL_Settlement settlement)
        {
            if (!Enabled) return;

            lock (StateLock)
            {
                SharedColonyTile state = LoadTile(settlement.Tile);
                if (state == null) return;

                state.Members.Remove(settlement.Username);
                if (state.MapHostUsername == settlement.Username) state.MapHostUsername = state.Members.FirstOrDefault();

                string path = GetTilePath(settlement.Tile);
                if (state.Members.Count == 0) File.Delete(path);
                else SaveTile(state);
            }

            BroadcastSnapshots();
        }

        public static string GetMapHostUsername(int tile)
        {
            if (!Enabled) return PM_Settlements.GetSettlementFileFromTile(tile)?.Username;

            lock (StateLock)
            {
                SharedColonyTile state = LoadTile(tile) ?? CreateTileState(tile);
                return state.MapHostUsername;
            }
        }

        public static bool CanSaveCanonicalMap(string username, int tile)
        {
            if (!Enabled || !Master.ServerConfig.SharedColonyHostOnlyMapSaves) return true;

            List<FL_Settlement> occupants = PM_Settlements.GetAllSettlementsAtTile(tile);
            if (occupants.Count <= 1) return true;
            return GetMapHostUsername(tile) == username;
        }

        public static bool MapHasConfiguredSize(int tile)
        {
            string path = Path.Combine(Master.MapsPath, tile + CommonValues.DefaultSaveFormat);
            if (!File.Exists(path)) return false;

            try
            {
                FL_Map map = Serializer.ConvertBytesToObject<FL_Map>(File.ReadAllBytes(path));
                int[] size = map.Size;
                int configured = Math.Clamp(Master.ServerConfig.SharedColonyMapSize, 250, 750);
                return size != null && size.Length >= 3 && size[0] == configured && size[2] == configured;
            }
            catch (Exception ex)
            {
                Printer.Warning($"Unable to inspect map size for tile {tile}: {ex.Message}");
                return false;
            }
        }

        public static bool MapBytesHaveConfiguredSize(byte[] bytes)
        {
            if (!Enabled || !Master.ServerConfig.EnforceSharedColonyMapSize) return true;

            try
            {
                FL_Map map = Serializer.ConvertBytesToObject<FL_Map>(bytes);
                int[] size = map.Size;
                int configured = Math.Clamp(Master.ServerConfig.SharedColonyMapSize, 250, 750);
                return size != null && size.Length >= 3 && size[0] == configured && size[2] == configured;
            }
            catch (Exception ex)
            {
                Printer.Warning($"Unable to inspect uploaded map size: {ex.Message}");
                return false;
            }
        }

        public static SharedColonyStance GetDeclaredStance(string source, string target)
        {
            if (source == target) return SharedColonyStance.Ally;

            lock (StateLock)
            {
                SharedColonyRelation relation = LoadRelations().FirstOrDefault(fetch =>
                    fetch.SourceUsername == source && fetch.TargetUsername == target);
                return relation?.Stance ?? SharedColonyStance.Neutral;
            }
        }

        public static SharedColonyStance GetEffectiveStance(string first, string second)
        {
            SharedColonyStance firstStance = GetDeclaredStance(first, second);
            SharedColonyStance secondStance = GetDeclaredStance(second, first);

            if (firstStance == SharedColonyStance.Hostile || secondStance == SharedColonyStance.Hostile) return SharedColonyStance.Hostile;
            if (firstStance == SharedColonyStance.Ally && secondStance == SharedColonyStance.Ally) return SharedColonyStance.Ally;
            if (firstStance == SharedColonyStance.Support || secondStance == SharedColonyStance.Support ||
                firstStance == SharedColonyStance.Ally || secondStance == SharedColonyStance.Ally) return SharedColonyStance.Support;
            return SharedColonyStance.Neutral;
        }

        public static void SetDeclaredStance(string source, string target, SharedColonyStance stance)
        {
            lock (StateLock)
            {
                List<SharedColonyRelation> relations = LoadRelations();
                SharedColonyRelation relation = relations.FirstOrDefault(fetch =>
                    fetch.SourceUsername == source && fetch.TargetUsername == target);

                if (relation == null)
                {
                    relation = new SharedColonyRelation { SourceUsername = source, TargetUsername = target };
                    relations.Add(relation);
                }

                relation.Stance = stance;
                SaveJson(RelationsPath, relations);
            }

            SendSnapshot(ServerNetwork.GetConnectedClientFromUsername(source));
            SendSnapshot(ServerNetwork.GetConnectedClientFromUsername(target));
        }

        public static void SendSnapshot(ServerClient client)
        {
            if (!Enabled || client == null) return;

            string username = client.GetData<FL_Player>().Username;
            int mapSize = Math.Clamp(Master.ServerConfig.SharedColonyMapSize, 250, 750);
            int capacity = Math.Clamp(Master.ServerConfig.SharedColonyTileCapacity, 1, 8);
            PM_Chat.SendProtocolMessage(client, $"{ProtocolPrefix}|CONFIG|{mapSize}|{capacity}");

            // Tell a reconnecting member which player owns the canonical map.
            // The client cannot infer this safely from overlapping world markers,
            // and settlement registration acknowledgements only exist during the
            // original new-colony flow.
            FL_Settlement ownedSettlement = PM_Settlements.GetSettlementFileFromUsername(username);
            if (ownedSettlement != null)
            {
                List<FL_Settlement> occupants = PM_Settlements.GetAllSettlementsAtTile(ownedSettlement.Tile);
                if (occupants.Count > 1)
                {
                    string hostUsername = GetMapHostUsername(ownedSettlement.Tile);
                    PM_Chat.SendProtocolMessage(client,
                        $"{ProtocolPrefix}|TILE|{ownedSettlement.Tile}|{hostUsername}|{occupants.Count}");
                }
            }

            foreach (string other in PM_Settlements.GetAllSettlements()
                         .Select(fetch => fetch.Username)
                         .Where(fetch => fetch != username)
                         .Distinct())
            {
                SharedColonyStance declared = GetDeclaredStance(username, other);
                SharedColonyStance effective = GetEffectiveStance(username, other);
                PM_Chat.SendProtocolMessage(client, $"{ProtocolPrefix}|REL|{other}|{declared}|{effective}");
            }
        }

        public static void BroadcastSnapshots()
        {
            if (!Enabled) return;
            foreach (ServerClient client in ServerNetwork.GetConnectedClients()) SendSnapshot(client);
        }

        private static SharedColonyTile CreateTileState(int tile)
        {
            List<string> members = PM_Settlements.GetAllSettlementsAtTile(tile)
                .Select(fetch => fetch.Username)
                .Distinct()
                .ToList();

            SharedColonyTile state = new SharedColonyTile
            {
                Tile = tile,
                MapSize = Master.ServerConfig.SharedColonyMapSize,
                MapHostUsername = members.FirstOrDefault(),
                Members = members
            };
            SaveTile(state);
            return state;
        }

        private static SharedColonyTile LoadTile(int tile) => LoadJson<SharedColonyTile>(GetTilePath(tile));

        private static void SaveTile(SharedColonyTile state) => SaveJson(GetTilePath(state.Tile), state);

        private static string GetTilePath(int tile) => Path.Combine(Master.SharedColoniesPath, $"{tile}.json");

        private static List<SharedColonyRelation> LoadRelations() =>
            LoadJson<List<SharedColonyRelation>>(RelationsPath) ?? new List<SharedColonyRelation>();

        private static T LoadJson<T>(string path) where T : class
        {
            if (!File.Exists(path)) return null;
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
        }

        private static void SaveJson(string path, object value)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporaryPath = path + ".tmp";
            File.WriteAllText(temporaryPath, JsonConvert.SerializeObject(value, Formatting.Indented));
            File.Move(temporaryPath, path, true);
        }
    }
}
