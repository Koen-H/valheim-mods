// ExploreHelper - reach the four far edges of the world, for Valheim 1.0 (Unity 6, BepInEx 5.4.23+)
//
// The Explorer (and The Pathfinder, in a world without a map) count once you have been more than 10350 m from
// the centre to the north, south, east and west. The game names the directions its own way: "far east" counts
// on the left side of the map and "far west" on the right. The pins follow the game, so they are where it counts.
// 1. Four pins on the map mark where each direction counts, crossed off once you have it.
// 2. Reaching a new one shows a message with your progress.
// Client-side only; the pins are not saved and not shared.

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace ExploreHelper
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class ExploreHelperPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.koendertjh.explorehelper";
        public const string ModName = "ExploreHelper";
        public const string ModVersion = PluginInfo.Version;

        internal static ManualLogSource Log;

        internal static ConfigEntry<bool> ShowPins;
        internal static ConfigEntry<bool> ReachedMessage;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            ShowPins = Config.Bind("1 - General", "ShowPins", true,
                "Pins on the map where each far direction counts, crossed off once you have it.");
            ReachedMessage = Config.Bind("1 - General", "ReachedMessage", true,
                "A message when you reach a far direction for the first time.");

            _harmony = new Harmony(ModGuid);
            _harmony.PatchAll(typeof(ExploreHelperPlugin).Assembly);
        }

        private void OnDestroy() => _harmony?.UnpatchSelf();
    }
}
