// Chat / console commands (not cheats, so they work in chat with a leading '/'):
//   bossreset            show what resetting the nearest boss fight would remove and give back
//   bossreset confirm    do it
//
// Commands register themselves in the game's static command list, so creating
// them once at plugin start is enough.

using System;
using System.Collections.Generic;
using System.Linq;

namespace BossReset
{
    internal static class Commands
    {
        public static void Register()
        {
            try
            {
                new Terminal.ConsoleCommand("bossreset",
                    "BossReset: end the nearest boss fight and get the summoning items back - bossreset | bossreset confirm",
                    (Terminal.ConsoleEvent)Run,
                    optionsFetcher: () => new List<string> { "confirm" });
            }
            catch (Exception e)
            {
                BossResetPlugin.Log.LogWarning($"Couldn't add console commands: {e.Message}");
            }
        }

        private static void Run(Terminal.ConsoleEventArgs args)
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                Say(args, "BossReset: load into a world with a character first.");
                return;
            }

            var fight = BossFight.Find(player.transform.position);
            if (fight == null)
            {
                Say(args, $"BossReset: no living boss within {BossResetPlugin.BossRange.Value:0} m.");
                return;
            }

            bool confirmed = args.Args.Skip(1).Any(a => a.Equals("confirm", StringComparison.OrdinalIgnoreCase));
            string boss = BossFight.Name(fight.Boss);
            string items = fight.GiveBack.Count > 0
                ? string.Join(", ", fight.GiveBack.Select(i => $"{i.Value}x {BossFight.Localize(i.Key.m_itemData.m_shared.m_name)}"))
                : null;

            if (!confirmed)
            {
                Say(args, $"BossReset: this removes {boss}" +
                          (fight.Minions.Count > 0 ? $" and {fight.Minions.Count} hostile creature(s) around the arena." : "."));
                if (items != null) Say(args, $"  You get back: {items}.");
                else if (fight.Altar == null) Say(args, "  No altar found near where the boss spawned, so nothing is given back.");
                else Say(args, "  Nothing to give back (the altar's items are still in place).");
                Say(args, "  Type 'bossreset confirm' to do it.");
                return;
            }

            int minions = fight.Minions.Count(BossFight.Remove);
            if (!BossFight.Remove(fight.Boss))
            {
                Say(args, $"BossReset: couldn't remove {boss}, nothing given back. See the BepInEx log.");
                return;
            }
            foreach (var item in fight.GiveBack)
                BossFight.Give(player, item.Key, item.Value);

            Say(args, $"BossReset: removed {boss}" + (minions > 0 ? $" and {minions} creature(s)" : "") +
                      (items != null ? $". Gave back: {items}." : "."));
            BossResetPlugin.Log.LogInfo($"Reset {Utils.GetPrefabName(fight.Boss.gameObject)}: {minions} creature(s) removed, " +
                                         $"gave back {items ?? "nothing"}.");
        }

        private static void Say(Terminal.ConsoleEventArgs args, string text)
        {
            if (args?.Context != null) args.Context.AddString(text);
            else BossResetPlugin.Log.LogInfo(text);
        }
    }
}
