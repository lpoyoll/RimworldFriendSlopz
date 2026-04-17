using Shared.Files;

namespace TCPNetwork.Packets
{
    public class PKT_PlayerGuild : PKT_Base
    {
        public enum GuildStepMode { Create, Delete, NameInUse, Invite, RemoveMember, AddMember, Promote, Demote, AdminProtection, MemberList }

        public GuildStepMode _stepMode { get; set; } = GuildStepMode.Create;

        public FL_Guild _guild { get; set; } = new FL_Guild();

        public int _dataInt { get; set; } = -1;
    }
}
