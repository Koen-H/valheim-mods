// MerchantTracker - what the traders still have for you, for Valheim 1.0 (Unity 6, BepInEx 5.4.23+)
//
// 1. In a trader's shop, wares you have never owned get a "new" marker after their name.
// 2. A last row counts the wares the trader only sells later (after a boss, or for Hildir after returning her
//    chests), without saying what they are.
// Client-side only; works on vanilla servers.

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace MerchantTracker
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class MerchantTrackerPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.koendertjh.merchanttracker";
        public const string ModName = "MerchantTracker";
        public const string ModVersion = PluginInfo.Version;

        internal static ManualLogSource Log;

        internal static ConfigEntry<bool> NewMarker;
        internal static ConfigEntry<string> MarkerText;
        internal static ConfigEntry<bool> LockedWaresRow;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            NewMarker = Config.Bind("1 - General", "NewMarker", true,
                "Wares you have never owned get a marker after their name.");
            MarkerText = Config.Bind("1 - General", "MarkerText", "<color=#ffb347>new</color>",
                "The marker. Rich text is allowed.");
            LockedWaresRow = Config.Bind("1 - General", "LockedWaresRow", true,
                "A last row counting the wares the trader will only sell later.");

            _harmony = new Harmony(ModGuid);
            _harmony.PatchAll(typeof(MerchantTrackerPlugin).Assembly);
        }

        private void OnDestroy() => _harmony?.UnpatchSelf();
    }
}
