using BarRaider.SdTools;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GoveeLightController {
    internal class Program {

        static void Main(string[] args) {
            // Uncomment this line of code to allow for debugging
            //while (!System.Diagnostics.Debugger.IsAttached) { System.Threading.Thread.Sleep(100); }

            // only call the StreamDeck Wrapper function if the arguemnts are not specifically for a standalone version
            SDWrapper.Run(args);
        }
    }
}
