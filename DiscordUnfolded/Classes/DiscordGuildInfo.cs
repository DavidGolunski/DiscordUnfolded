using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUnfolded {
    public class DiscordGuildInfo {

        public ulong GuildId { get; set; }  
        public string GuildName { get; set; }
        public string IconUrl { get; set; }

        public DiscordGuildInfo() {
            GuildId = 0;
            GuildName = string.Empty;
            IconUrl = string.Empty;
        }

        public override bool Equals(object obj) {
            if(obj == null || obj.GetType () != this.GetType()) return false;
            DiscordGuildInfo other = obj as DiscordGuildInfo;

            return this.GuildId == other.GuildId
                && this.GuildName == other.GuildName
                && this.IconUrl == other.IconUrl;
        }

        public override int GetHashCode() {
            int hash = 17;

            hash = hash * 23 + (GuildId.GetHashCode());
            hash = hash * 23 + (GuildName.GetHashCode());
            hash = hash * 23 + (IconUrl.GetHashCode());

            return hash;
        }

        public override string ToString() {
            return "DiscordGuild: " + GuildId.ToString() + "," + GuildName.ToString() + "," + IconUrl.ToString();
        }
    }
}
