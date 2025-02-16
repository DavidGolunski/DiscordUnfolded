using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using WindowsInput;
using WindowsInput.Native;

namespace DiscordUnfolded {
    public class KeyBindAction {

        public static Dictionary<string, KeyBindAction> KeyBindActions = new Dictionary<string, KeyBindAction>() {
            { "MUTE", new KeyBindAction(VirtualKeyCode.F13, VirtualKeyCode.F14) },
            { "DEAFEN", new KeyBindAction(VirtualKeyCode.F13, VirtualKeyCode.F15) },
            { "STREAM", new KeyBindAction(VirtualKeyCode.F13, VirtualKeyCode.F16) },
            { "DISCONNECT", new KeyBindAction(VirtualKeyCode.F13, VirtualKeyCode.F17) },

            // Salzminen Commands
            { "JOIN_797567441210441778", new KeyBindAction(VirtualKeyCode.F14, VirtualKeyCode.F15) }, // Aufzug Nach Unten
            { "JOIN_1106656308964372510", new KeyBindAction(VirtualKeyCode.F14, VirtualKeyCode.F16) }, // Suchtberatungsstelle 
            { "JOIN_797566194806292526", new KeyBindAction(VirtualKeyCode.F14, VirtualKeyCode.F17) }, // Haupthalle
            { "JOIN_797576387778445352", new KeyBindAction(VirtualKeyCode.F14, VirtualKeyCode.F18) }, // Nebenhalle

            // FAT Clash Commands 
            { "JOIN_823214866915524628", new KeyBindAction(VirtualKeyCode.F15, VirtualKeyCode.F16) } // General
        };

        private static readonly InputSimulator sim = new InputSimulator();

        private readonly VirtualKeyCode keyCode1;
        private readonly VirtualKeyCode keyCode2;

        public KeyBindAction(VirtualKeyCode keyCode1, VirtualKeyCode keyCode2) {
            this.keyCode1 = keyCode1;
            this.keyCode2 = keyCode2;
        }

        public void Execute() {
            sim.Keyboard.KeyDown(keyCode1);
            sim.Keyboard.KeyDown(keyCode2);

            sim.Keyboard.KeyUp(keyCode1);
            sim.Keyboard.KeyUp(keyCode2);
        }
    }
}
