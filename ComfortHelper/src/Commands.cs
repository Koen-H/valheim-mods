// Chat / console command (not a cheat, so it works in chat with a leading '/'):
//   comfort    your comfort here and every known piece that would raise it
//
// Commands register themselves in the game's static command list, so creating
// them once at plugin start is enough.

using System;

namespace ComfortHelper
{
    internal static class Commands
    {
        public static void Register()
        {
            try
            {
                new Terminal.ConsoleCommand("comfort",
                    "ComfortHelper: your comfort here and the pieces that would raise it",
                    (Terminal.ConsoleEvent)Run);
            }
            catch (Exception e)
            {
                ComfortHelperPlugin.Log.LogWarning($"Couldn't add console commands: {e.Message}");
            }
        }

        private static void Run(Terminal.ConsoleEventArgs args)
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                Say(args, "ComfortHelper: load into a world with a character first.");
                return;
            }

            ComfortReport report = Comfort.Build(player);
            Say(args, $"ComfortHelper: comfort here {report.Current}, best so far {report.MaxReached:0} " +
                      "(Comfort is King needs 20).");
            if (!report.InShelter)
                Say(args, "  You are not under a roof: pieces only count in a shelter.");
            Say(args, $"  With the pieces you know, the most you can reach is {report.BestKnown}." +
                      (report.Locked > 0 ? $" Pieces you haven't unlocked yet add up to {report.Locked} more." : ""));

            if (report.Suggestions.Count == 0)
            {
                Say(args, "  Nothing you know would raise it here.");
                return;
            }
            Say(args, "  Within 10 m, add:");
            foreach (Suggestion suggestion in report.Suggestions)
                Say(args, "    " + Comfort.Describe(suggestion));
        }

        private static void Say(Terminal.ConsoleEventArgs args, string text)
        {
            if (args?.Context != null) args.Context.AddString(text);
            else ComfortHelperPlugin.Log.LogInfo(text);
        }
    }
}
