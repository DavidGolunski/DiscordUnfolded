﻿using BarRaider.SdTools;
using BarRaider.SdTools.Wrappers;
using DiscordUnfolded.Classes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data.Common;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordUnfolded.DiscordStructure;
using DiscordUnfolded.DiscordCommunication;

namespace DiscordUnfolded {
    [PluginActionId("com.davidgolunski.discordunfolded.currentserveraction")]
    public class CurrentServerAction : KeypadBase {

        private DiscordGuildInfo currentGuildInfo = null;


        public CurrentServerAction(SDConnection connection, InitialPayload payload) : base(connection, payload) {
            DiscordRPC.Instance.OnSelectedGuildChanged += UpdateButton;
            UpdateButton(DiscordRPC.Instance.SelectedGuild);
        }

        public override void Dispose() {
            DiscordRPC.Instance.OnSelectedGuildChanged -= UpdateButton;
        }

        public override void KeyPressed(KeyPayload payload) {
            ChannelGridManager.Instance.UpdateChannelGrid();
        }

        public override void KeyReleased(KeyPayload payload) { }

        public override void OnTick() { }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) {  }

        public override void ReceivedSettings(ReceivedSettingsPayload payload) { }


        public void UpdateButton(DiscordGuild discordGuild) {
            DiscordGuildInfo discordGuildInfo = discordGuild?.GetInfo();

            // if the info has not changed, then we do not need to update
            if(currentGuildInfo == null && discordGuildInfo == null)
                return;
            if(currentGuildInfo != null && currentGuildInfo.Equals(discordGuildInfo))
                return;


            currentGuildInfo = discordGuildInfo;

            if(discordGuildInfo == null) {
                Connection.SetDefaultImageAsync().GetAwaiter().GetResult();
                return;
            }

            string imageUrl = discordGuildInfo.IconUrl;
            Bitmap bitmap = ImageTools.GetResizedBitmapFromUrl(imageUrl);
            if(bitmap == null) {
                bitmap = ImageTools.GetBitmapFromFilePath("./Images/RoundRectangle@2x.png");
                string title = ImageTools.SplitString(discordGuildInfo.GuildName, 7);
                bitmap = ImageTools.AddTextToBitmap(bitmap, title, Color.White);
            }

            Connection.SetImageAsync(bitmap).GetAwaiter().GetResult();
            bitmap.Dispose();
        }
    }
}
