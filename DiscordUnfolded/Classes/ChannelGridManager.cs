using BarRaider.SdTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUnfolded {
    public class ChannelGridManager {

        private static ChannelGridManager instance;
        public static ChannelGridManager Instance {
            get => instance ??= new ChannelGridManager();
            private set => instance = value;
        }

        private const int width = 8;
        private const int height = 4;

        private int xOffset = 0;
        public int XOffset {
            get => xOffset;
            set {
                if(xOffset == value)
                    return;

                int newValue = Math.Min(value, width);
                newValue = Math.Max(newValue, 0);
                xOffset = newValue;
            }
        }
        private int yOffset = 0;
        public int YOffset {
            get => yOffset;
            set {
                if(yOffset == value)
                    return;

                int newValue = Math.Min(value, height);
                newValue = Math.Max(newValue, 0);
                yOffset = newValue;
            }
        }

        private readonly Dictionary<(int, int), EventHandler<(DiscordChannelInfo, DiscordUserInfo)>> updateEvents = new Dictionary<(int, int), EventHandler<(DiscordChannelInfo, DiscordUserInfo)>>();

        private DiscordGuildInfo guildInfo = null;
        private readonly Dictionary<ulong, DiscordChannelInfo> voiceChannels = new Dictionary<ulong, DiscordChannelInfo>();
        private readonly Dictionary<ulong, DiscordChannelInfo> textChannels = new Dictionary<ulong, DiscordChannelInfo>();
        private readonly Dictionary<(ulong, ulong), DiscordUserInfo> userInfos = new Dictionary<(ulong, ulong), DiscordUserInfo>();

        private ChannelGridManager() {

            for(int y = 0; y < height; y++) {
                for(int x = 0; x < width; x++) {
                    updateEvents[(x, y)] = null;
                }
            }

            ServerBrowserManager.Instance.SubscribeToSelectedGuild(OnSelectedGuildChanged);
        }

        ~ChannelGridManager() {
            ServerBrowserManager.Instance.UnsubscribeFromSelectedGuild(OnSelectedGuildChanged);
        }

        private void OnSelectedGuildChanged(object sender, DiscordGuildInfo discordGuildInfo) {
            guildInfo = discordGuildInfo;

        }


        public void SubscribeToPosition(int xPos, int yPos, EventHandler<(DiscordChannelInfo, DiscordUserInfo)> handler, bool instantlyUpdate = false) {
            if(xPos < 0 || xPos >= width || yPos < 0 || yPos >= height) {
                Logger.Instance.LogMessage(TracingLevel.WARN, "ChannelButton wanted to subsribe to position (" + xPos + "," + yPos + ")");
                return;
            }
            updateEvents[(xPos, yPos)] += handler;

            if(!instantlyUpdate)
                return;

            //DiscordGuildInfo guildToUpdate = GetGuildAtPosition(position);
            //handler.Invoke(this, guildToUpdate);
        }

        public void UnsubscribeFromPosition(int xPos, int yPos, EventHandler<(DiscordChannelInfo, DiscordUserInfo)> handler) {
            if(xPos < 0 || xPos >= width || yPos < 0 || yPos >= height) {
                Logger.Instance.LogMessage(TracingLevel.WARN, "ChannelButton wanted to unsubsribe from position (" + xPos + "," + yPos + ")");
                return;
            }
            updateEvents[(xPos, yPos)] -= handler;
        }

    }
}
