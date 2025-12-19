using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Files.Guilds
{
    public class GuildMember
    {
        public enum GuildRanks { Member, Moderator, Admin }

        public string Username { get; set; } = string.Empty;

        public GuildRanks Rank { get; set; } = GuildRanks.Member;
    }
}
