// MaterialTracker - every crafting material in one window, for Valheim 1.0 (Unity 6, BepInEx 5.4.23+)
//
// A Materials button next to Trophies opens a copy of the Trophies window that lists every item used in a
// recipe or build piece (trophies excepted): the ones you have found in colour, the rest greyed out with a hint
// where they come from. Materials of later biomes stay hidden ("Only Odin knows") until you defeat the boss
// before that biome, unless ShowAll is on.
// Client-side only; works on vanilla servers.

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace MaterialTracker
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class MaterialTrackerPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.koendertjh.materialtracker";
        public const string ModName = "MaterialTracker";
        public const string ModVersion = PluginInfo.Version;

        internal static ManualLogSource Log;

        internal static ConfigEntry<bool> ShowAll;
        internal static ConfigEntry<bool> ShowFound;
        internal static ConfigEntry<bool> ShowHints;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            ShowAll = Config.Bind("1 - Spoilers", "ShowAll", false,
                "Show every missing material right away. Off = materials of a biome show once you defeated the boss " +
                "before it; later ones show as '?'.");
            ShowFound = Config.Bind("2 - Display", "ShowFound", true,
                "Also list the materials you have already found. Off = only the missing ones.");
            ShowHints = Config.Bind("2 - Display", "ShowHints", true,
                "Show where each material comes from (dropped by, crafted at, mined...).");

            _harmony = new Harmony(ModGuid);
            _harmony.PatchAll(typeof(MaterialTrackerPlugin).Assembly);
        }

        private void OnDestroy() => _harmony?.UnpatchSelf();
    }
}
