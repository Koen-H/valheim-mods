// Chat / console commands (not cheats, so they work in chat with a leading '/'):
//   trophypity                  list this character's dry-kill counters
//   trophyreset <creature>      reset one creature (prefab or display name, e.g. Wolf)
//   trophyreset all             reset every counter of this character (file is backed up first)
//
// Commands register themselves in the game's static command list, so creating
// them once at plugin start is enough.

using System;
using System.Collections.Generic;
using System.Linq;

namespace TrophyAssister
{
    internal static class Commands
    {
        public static void Register()
        {
            try
            {
                new Terminal.ConsoleCommand("trophypity",
                    "TrophyAssister: list this character's dry-kill counters",
                    (Terminal.ConsoleEvent)List);
                new Terminal.ConsoleCommand("trophyreset",
                    "TrophyAssister: reset dry kills - trophyreset <creature> | trophyreset all",
                    (Terminal.ConsoleEvent)Reset,
                    optionsFetcher: TabOptions, alwaysRefreshTabOptions: true);
            }
            catch (Exception e)
            {
                TrophyAssisterPlugin.Log.LogWarning($"Couldn't add console commands: {e.Message}");
            }
        }

        private static void List(Terminal.ConsoleEventArgs args)
        {
            if (!CurrentCharacter(args, out string charKey)) return;
            var entries = PityStore.ForCharacter(charKey);
            if (entries.Count == 0)
            {
                Say(args, "TrophyAssister: no dry kills counted for this character.");
                return;
            }
            Say(args, $"TrophyAssister: {entries.Count} creature(s) with dry kills:");
            foreach (var e in entries)
                Say(args, $"  {DisplayName(e.Key)} ({e.Key}): {e.Value}");
        }

        private static void Reset(Terminal.ConsoleEventArgs args)
        {
            if (!CurrentCharacter(args, out string charKey)) return;

            string input = string.Join(" ", args.Args.Skip(1)).Trim();
            if (input.Length == 0)
            {
                Say(args, "Usage: trophyreset <creature>  or  trophyreset all   (see trophypity for names)");
                return;
            }

            if (input.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                string bak = PityStore.Backup();
                int n = PityStore.ResetCharacter(charKey);
                Say(args, $"TrophyAssister: reset {n} counter(s) for this character." +
                          (bak != null ? $" Backup: {bak}" : ""));
                return;
            }

            var match = PityStore.ForCharacter(charKey).FirstOrDefault(e =>
                e.Key.Equals(input, StringComparison.OrdinalIgnoreCase) ||
                DisplayName(e.Key).Equals(input, StringComparison.OrdinalIgnoreCase));
            if (match.Key == null)
            {
                Say(args, $"TrophyAssister: no dry kills counted for '{input}'. Type trophypity to see the list.");
                return;
            }

            PityStore.Reset(charKey, match.Key);
            Say(args, $"TrophyAssister: reset {DisplayName(match.Key)} ({match.Value} dry kills).");
        }

        private static List<string> TabOptions()
        {
            var list = new List<string> { "all" };
            try
            {
                if (Player.m_localPlayer != null)
                    list.AddRange(PityStore.ForCharacter(GameUtil.CharKey(Player.m_localPlayer)).Select(e => e.Key));
            }
            catch { /* no options */ }
            return list;
        }

        private static bool CurrentCharacter(Terminal.ConsoleEventArgs args, out string charKey)
        {
            charKey = Player.m_localPlayer != null ? GameUtil.CharKey(Player.m_localPlayer) : null;
            if (charKey == null) Say(args, "TrophyAssister: load into a world with a character first.");
            return charKey != null;
        }

        private static string DisplayName(string creaturePrefab)
        {
            try
            {
                var go = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(creaturePrefab) : null;
                var ch = go != null ? go.GetComponent<Character>() : null;
                if (ch != null) return GameUtil.Localize(ch.m_name);
            }
            catch { /* fall back to prefab name */ }
            return creaturePrefab;
        }

        private static void Say(Terminal.ConsoleEventArgs args, string text)
        {
            if (args?.Context != null) args.Context.AddString(text);
            else TrophyAssisterPlugin.Log.LogInfo(text);
        }
    }
}
