// AchievementPeek - shows what the achievement screen hides, for Valheim 1.0 (Unity 6, BepInEx 5.4.23+)
//
// 1. The details of an achievement list every condition, but the ones you haven't met yet show as
//    "??? / ???". The mod shows their name and your real progress instead, for the things you have come
//    across (or everything, or nothing: see MissingRows). The rest is summed up in one "Only Odin knows" row.
// 2. Secret achievements show their name and description and can be opened, all at once or one boss at a
//    time (see SecretAchievements).
// 3. Achievements made of other achievements list them in their details.
// Everything is only shown on your own screen: it works on vanilla servers.

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Koendertjh.Shared;

namespace AchievementPeek
{
    internal enum RowMode
    {
        Off,
        Discovered,
        All
    }

    internal enum SecretMode
    {
        Hidden,
        Progressive,
        All
    }

    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class AchievementPeekPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.koendertjh.achievementpeek";
        public const string ModName = "AchievementPeek";
        public const string ModVersion = PluginInfo.Version;

        internal static ManualLogSource Log;

        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<Boss> RequiredBoss;
        internal static ConfigEntry<RowMode> MissingRows;
        internal static ConfigEntry<SecretMode> SecretAchievements;
        internal static ConfigEntry<bool> MissingFirst;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            Enabled = Config.Bind("1 - General", "Enabled", true, "Master switch.");
            RequiredBoss = Config.Bind("2 - Spoilers", "RequiredBoss", Boss.None,
                "The mod stays off (the achievement screen looks like vanilla) until you have defeated this boss.");
            MissingRows = Config.Bind("2 - Spoilers", "MissingRows", RowMode.Discovered,
                "What the details of an achievement show for conditions you haven't met yet. " +
                "Off = vanilla '???'. Discovered = only things you have come across (known items, recipes and " +
                "pieces, creatures you killed or whose drops you picked up); the rest is counted in an " +
                "'Only Odin knows' row. All = everything.");
            SecretAchievements = Config.Bind("2 - Spoilers", "SecretAchievements", SecretMode.Progressive,
                "Hidden = vanilla. Progressive = a boss achievement shows once you defeated the boss before it, " +
                "Ashlands secrets after the Queen; the other secrets show right away. All = every secret shows.");
            MissingFirst = Config.Bind("3 - Display", "MissingFirst", true,
                "List the conditions you haven't met yet above the ones you have.");

            _harmony = new Harmony(ModGuid);
            _harmony.PatchAll(typeof(AchievementPeekPlugin).Assembly);
        }

        private void OnDestroy() => _harmony?.UnpatchSelf();

        /// <summary>Enabled, and the required boss is defeated.</summary>
        internal static bool Active => Enabled.Value && BossProgress.IsDefeated(RequiredBoss.Value);
    }
}
