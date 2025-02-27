﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUnfolded.DiscordCommunication {
    internal enum EventType {
        UNKNOWN,
        READY,
        ERROR,
        GUILD_STATUS,
        GUILD_CREATE,
        CHANNEL_CREATE,
        VOICE_CHANNEL_SELECT,
        VOICE_STATE_CREATE,
        VOICE_STATE_UPDATE,
        VOICE_STATE_DELETE,
        VOICE_SETTINGS_UPDATE,
        VOICE_CONNECTION_STATUS,
        SPEAKING_START,
        SPEAKING_STOP,
        MESSAGE_CREATE,
        MESSAGE_UPDATE,
        MESSAGE_DELETE,
        NOTIFICATION_CREATE,
        ACTIVITY_JOIN,
        ACTIVITY_SPECTATE,
        ACTIVITY_JOIN_REQUEST
    }
}
