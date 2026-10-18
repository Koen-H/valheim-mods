// PeakFinder - where to build high, for Valheim 1.0 (Unity 6, BepInEx 5.4.23+)
//
// Top of the World needs a piece placed at 432 m world height (sea level is 30 m), outdoors and on your own
// foundation; Mighty Halls a piece 49 m above the ground below it (Player.PlacePiece).
// 1. The big map gets a "Show high points" toggle: pins on the highest peaks of the world, with how much you
//    still need to build on top. Peaks are found from the world generator, so they are right for every seed.
// 2. A new highest or tallest build shows a message with how far you are from both achievements.
// Client-side only; the pins are only on your own map and are not saved.

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace PeakFinder
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class PeakFinderPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.koendertjh.peakfinder";
        public const string ModName = "PeakFinder";
        public const string ModVersion = PluginInfo.Version;

        internal const float TopOfTheWorld = 432f;
        internal const float MightyHalls = 49f;

        internal static ManualLogSource Log;
        internal static PeakFinderPlugin Instance;

        internal static ConfigEntry<bool> ShowPeaks;
        internal static ConfigEntry<int> PeakCount;
        internal static ConfigEntry<float> MinDistance;
        internal static ConfigEntry<bool> OnlyExplored;
        internal static ConfigEntry<bool> HeightMessages;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            Instance = this;

            ShowPeaks = Config.Bind("1 - Map", "ShowPeaks", false,
                "Show the highest peaks on the map. Also switched with the toggle on the big map.");
            PeakCount = Config.Bind("1 - Map", "PeakCount", 10,
                new ConfigDescription("Number of peaks to show.", new AcceptableValueRange<int>(1, 50)));
            MinDistance = Config.Bind("1 - Map", "MinDistance", 500f,
                new ConfigDescription("Peaks are at least this many metres apart, so one mountain doesn't fill the list.",
                    new AcceptableValueRange<float>(100f, 3000f)));
            OnlyExplored = Config.Bind("1 - Map", "OnlyExplored", true,
                "Only show peaks in parts of the map you (or players you shared the map with) have explored.");
            HeightMessages = Config.Bind("2 - Messages", "HeightMessages", true,
                "A message when you place your highest or tallest piece so far, with how far you are from " +
                "Top of the World (432 m) and Mighty Halls (49 m).");

            _harmony = new Harmony(ModGuid);
            _harmony.PatchAll(typeof(PeakFinderPlugin).Assembly);
        }

        private void OnDestroy() => _harmony?.UnpatchSelf();
    }
}
