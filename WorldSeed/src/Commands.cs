// Chat / console command (not a cheat, so it works in chat with a leading '/'):
//   seed    show the world seed and copy it to the clipboard
//
// Commands register themselves in the game's static command list, so creating
// them once at plugin start is enough.

using System;
using UnityEngine;

namespace WorldSeed
{
    internal static class Commands
    {
        public static void Register()
        {
            try
            {
                new Terminal.ConsoleCommand("seed",
                    "WorldSeed: show the world seed and copy it to the clipboard",
                    (Terminal.ConsoleEvent)Run);
            }
            catch (Exception e)
            {
                WorldSeedPlugin.Log.LogWarning($"Couldn't add console commands: {e.Message}");
            }
        }

        private static void Run(Terminal.ConsoleEventArgs args)
        {
            string seed = WorldSeedPlugin.Seed;
            if (seed == null)
            {
                Say(args, "WorldSeed: load into a world first.");
                return;
            }
            if (!WorldSeedPlugin.Unlocked)
            {
                Say(args, "WorldSeed: " + WorldSeedPlugin.LockedText);
                return;
            }

            GUIUtility.systemCopyBuffer = seed;
            Say(args, $"WorldSeed: the seed of this world is {seed} (copied to the clipboard).");
        }

        private static void Say(Terminal.ConsoleEventArgs args, string text)
        {
            if (args?.Context != null) args.Context.AddString(text);
            else WorldSeedPlugin.Log.LogInfo(text);
        }
    }
}
