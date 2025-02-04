using BarRaider.SdTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUnfolded {
    internal class Program {
        static void Main(string[] args) {
            // Uncomment this line of code to allow for debugging
            //while (!System.Diagnostics.Debugger.IsAttached) { System.Threading.Thread.Sleep(100); }

            // call the instance of the managers once so they can subscribe to any events beforehand
            //ServerBrowserManager.Instance.ToString();
            //ChannelGridManager.Instance.ToString();
            DiscordBot.Instance.Start();

            SDWrapper.Run(args);

            DiscordBot.Instance.Stop();
        }
    }
}
