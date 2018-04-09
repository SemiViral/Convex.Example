#region USINGS

using System;
using System.Threading.Tasks;

#endregion

namespace Convex.Example {
    internal static class Program {
        #region MEMBERS

        private static IrcBot Bot { get; set; }

        #endregion

        private static async Task DebugRun() {
            do {
                await Bot.Execute();
            } while (Bot.Executing);

            Bot.Dispose();
        }

        private static async Task InitialiseAndExecute() {
            using (Bot = new IrcBot()) {
                await Bot.Initialise();
                await DebugRun();
            }
        }

        private static void Main() {
            InitialiseAndExecute().Wait();

            Console.Write("Program terminated. Press any key to continue.");
            Console.ReadKey();
        }
    }
}
