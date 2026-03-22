using Shared.Files.Guilds;
using static Shared.CommonEnumerators;

namespace TCPNetwork.Packets
{
    public class PKT_PlayerGuild : PKT_Base
    {
        public enum GuildStepMode { Create, Delete, NameInUse, Invite, RemoveMember, AddMember, Promote, Demote, AdminProtection, MemberList }

        public GuildStepMode _stepMode { get; set; } = GuildStepMode.Create;

        public GuildFile _guild { get; set; } = new GuildFile();

        public int _dataInt { get; set; } = -1;
    }
}
