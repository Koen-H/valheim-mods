// FishTracker - catch every fish, for Valheim 1.0 (Unity 6, BepInEx 5.4.23+)
//
// 1. Fish tooltips say when you haven't caught that fish yet (The Fisherman), and bait tooltips list the fish
//    that bite on it, marking the ones you are still missing.
// 2. Chum: every fishing bait can be mashed into chum by hand. Use it (right click) while facing water and a
//    few fish of that bait show up, picked from the fish that live in the biome you are in. Fish you haven't
//    caught yet are more likely, and some come with stars (up to the 3 stars that A Catch Worthy of Njord needs).
// 3. A fishing indicator under the crosshair says when to pull: to hook a fish that bites, and to reel in a
//    hooked fish that isn't fighting.
// Chum is a new item: it disappears from inventories and chests if the mod is removed.

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace FishTracker
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class FishTrackerPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.koendertjh.fishtracker";
        public const string ModName = "FishTracker";
        public const string ModVersion = PluginInfo.Version;

        internal static ManualLogSource Log;

        internal static ConfigEntry<bool> TooltipHints;
        internal static ConfigEntry<bool> HideUnknownFish;
        internal static ConfigEntry<bool> ChumEnabled;
        internal static ConfigEntry<int> BaitPerChum;
        internal static ConfigEntry<int> FishPerChum;
        internal static ConfigEntry<float> UncaughtWeight;
        internal static ConfigEntry<float> LevelUpChance;
        internal static ConfigEntry<int> MaxLevel;
        internal static ConfigEntry<bool> FishingIndicator;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            TooltipHints = Config.Bind("1 - Tooltips", "TooltipHints", true,
                "Fish tooltips say when you haven't caught the fish yet; bait tooltips list the fish that bite on it.");
            HideUnknownFish = Config.Bind("1 - Tooltips", "HideUnknownFish", false,
                "Bait tooltips show '???' for fish you have never had, instead of their name.");

            ChumEnabled = Config.Bind("2 - Chum", "Enabled", true,
                "Add chum, crafted by hand from fishing bait. Needs a restart. Chum disappears from inventories and " +
                "chests when this is off or the mod is removed.");
            BaitPerChum = Config.Bind("2 - Chum", "BaitPerChum", 5,
                new ConfigDescription("Bait needed for one chum. Needs a restart.", new AcceptableValueRange<int>(1, 50)));
            FishPerChum = Config.Bind("2 - Chum", "FishPerChum", 3,
                new ConfigDescription("Fish that show up for one chum.", new AcceptableValueRange<int>(1, 10)));
            UncaughtWeight = Config.Bind("2 - Chum", "UncaughtWeight", 3f,
                new ConfigDescription("Fish you haven't caught yet are this many times more likely to show up.",
                    new AcceptableValueRange<float>(1f, 20f)));
            LevelUpChance = Config.Bind("2 - Chum", "LevelUpChance", 0.25f,
                new ConfigDescription("Chance for a chum fish to gain a star, rolled again for each star.",
                    new AcceptableValueRange<float>(0f, 1f)));
            MaxLevel = Config.Bind("2 - Chum", "MaxLevel", 4,
                new ConfigDescription("Highest fish level from chum. Level 4 (3 stars) is what A Catch Worthy of Njord needs.",
                    new AcceptableValueRange<int>(1, 5)));

            FishingIndicator = Config.Bind("3 - Fishing", "Indicator", true,
                "Show under the crosshair when to reel in (block) while fishing: when a fish bites, and while a " +
                "hooked fish is calm. Says to wait while a hooked fish fights, when reeling costs double stamina.");

            _harmony = new Harmony(ModGuid);
            _harmony.PatchAll(typeof(FishTrackerPlugin).Assembly);
        }

        private void OnDestroy() => _harmony?.UnpatchSelf();
    }
}
