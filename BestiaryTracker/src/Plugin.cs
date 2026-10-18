// BestiaryTracker - slay every kind of creature, for Valheim 1.0 (Unity 6, BepInEx 5.4.23+)
//
// The Hunter asks you to kill every kind of creature, The Master Hunter the same on hard difficulty (or
// harder). The lists come from those achievements, so they match what the game counts.
// 1. Creatures you haven't slain yet get a marker after their name, above their health bar.
// 2. Slaying a new kind shows a message with your progress.
// On hard difficulty or harder the Master Hunter list is used, until that achievement is done.
// Client-side only; works on vanilla servers.

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace BestiaryTracker
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class BestiaryTrackerPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.koendertjh.bestiarytracker";
        public const string ModName = "BestiaryTracker";
        public const string ModVersion = PluginInfo.Version;

        internal static ManualLogSource Log;

        internal static ConfigEntry<bool> NameMarker;
        internal static ConfigEntry<string> MarkerText;
        internal static ConfigEntry<bool> KillMessage;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            NameMarker = Config.Bind("1 - General", "NameMarker", true,
                "Creatures you haven't slain yet get a marker after their name, above their health bar.");
            MarkerText = Config.Bind("1 - General", "MarkerText", "<color=#ffb347>(not slain)</color>",
                "The marker. Rich text is allowed.");
            KillMessage = Config.Bind("1 - General", "KillMessage", true,
                "Slaying a new kind of creature shows a message with your progress.");

            _harmony = new Harmony(ModGuid);
            _harmony.PatchAll(typeof(BestiaryTrackerPlugin).Assembly);
        }

        private void OnDestroy() => _harmony?.UnpatchSelf();
    }
}
