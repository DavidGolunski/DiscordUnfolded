﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUnfolded.DiscordCommunication {
    internal enum MessageType {
        UNKNOWN,
        DISPATCH,
        AUTHORIZE,
        AUTHENTICATE,
        GET_GUILD,
        GET_GUILDS,
        GET_CHANNEL,
        GET_CHANNELS,
        SUBSCRIBE,
        UNSUBSCRIBE,
        SET_USER_VOICE_SETTINGS,
        SELECT_VOICE_CHANNEL,
        GET_SELECTED_VOICE_CHANNEL,
        SELECT_TEXT_CHANNEL,
        GET_VOICE_SETTINGS,
        SET_VOICE_SETTINGS,
        SET_CERTIFIED_DEVICES,
        SET_ACTIVITY,
        SEND_ACTIVITY_JOIN_INVITE,
        CLOSE_ACTIVITY_REQUEST
    }
}
