using BarRaider.SdTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoveeLightController {
    internal class Program {
        static void Main(string[] args) {
            // Uncomment this line of code to allow for debugging
            //while (!System.Diagnostics.Debugger.IsAttached) { System.Threading.Thread.Sleep(100); }

            SDWrapper.Run(args);


            /*
            LeagueEffectManager.Instance.Start(new List<string>() { "192.168.178.40" });
            Console.WriteLine("LeagueEffectManager has started. Press Enter to exit...");

            // Wait for the user to press Enter
            Console.ReadLine();

            // Stop the LeagueEffectManager and exit the application
            LeagueEffectManager.Instance.Stop();

            Console.WriteLine("LeagueEffectManager has stopped. Application exiting.");
            */
        }
    }
}
