// CraftTracker - adds a "Not crafted" tab to crafting stations for Valheim 1.0 (Unity 6, BepInEx 5.4.23+)
//
// The tab lists every recipe at the current station (or crafting by hand) that this character has
// never crafted. The data comes from the character's own save: the per-item "crafted" stats that
// the game keeps for the crafting achievements. Client-side only, nothing is written.

using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace CraftTracker
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class CraftTrackerPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.koendertjh.crafttracker";
        public const string ModName = "CraftTracker";
        public const string ModVersion = PluginInfo.Version;

        internal static ConfigEntry<string> TabName;
        internal static ConfigEntry<bool> ShowUndiscovered;
        internal static ConfigEntry<string> UndiscoveredText;
        internal static ConfigEntry<string> CongratsText;
        internal static ConfigEntry<string> CongratsSoFarText;

        private Harmony _harmony;

        private void Awake()
        {
            TabName = Config.Bind("General", "TabName", "Not crafted",
                "Label of the crafting tab. Takes effect after restarting the game.");
            ShowUndiscovered = Config.Bind("General", "ShowUndiscovered", true,
                "Show how many recipes of this station you have not discovered yet.");
            UndiscoveredText = Config.Bind("General", "UndiscoveredText", "{0} not discovered yet",
                "Text of the undiscovered indicator; {0} is the number of recipes.");
            CongratsText = Config.Bind("General", "CongratsText", "Congrats! You have crafted every item of it!",
                "Shown in the tab when every recipe of the station has been crafted.");
            CongratsSoFarText = Config.Bind("General", "CongratsSoFarText",
                "Congrats! You have crafted every item you have discovered so far. There are still more items to craft that you have not discovered yet!",
                "Shown in the tab when every discovered recipe has been crafted, but some are still undiscovered.");

            _harmony = new Harmony(ModGuid);
            _harmony.PatchAll(typeof(CraftTrackerPlugin).Assembly);
        }

        private void OnDestroy() => _harmony?.UnpatchSelf();
    }
}
