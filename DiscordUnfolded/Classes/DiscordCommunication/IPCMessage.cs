using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUnfolded.DiscordCommunication {
    internal class IPCMessage {

        public static IPCMessage Empty = new IPCMessage();

        public MessageType MessageType { get; private set; }
        public JObject Error { get; private set; }
        public JObject Data { get; private set; }

        private IPCMessage() {
            MessageType = MessageType.UNKNOWN;
            Error = null;
            Data = null;
        }

        public IPCMessage(MessageType messageType, JObject error, JObject data) {
            MessageType = messageType;
            Error = error;
            Data = data;
        }

        public override string ToString() {
            if(Error != null) 
                return MessageType.ToString() + " Error: " + Error;

            return MessageType.ToString() + " Data: " + Data;
        }
    }
}
