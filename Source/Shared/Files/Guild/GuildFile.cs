using System;
using System.Collections.Generic;
using System.Threading;

namespace Shared.Files.Guild
{
    public class GuildFile
    {
        public string Name { get; set; } = string.Empty;

        public List<GuildMember> GuildMembers { get; set; } = new List<GuildMember>();

        [NonSerialized] public Semaphore SavingSemaphore = new Semaphore(1, 1);
    }
}
