// BuildTracker - adds a "Not built" tab to the build menu for Valheim 1.0 (Unity 6, BepInEx 5.4.23+)
//
// The tab lists every piece of the tool in hand that this character has never placed.
// The data comes from the character's own save: the per-piece "placed" stats that the
// game keeps for the building achievements. Client-side only, nothing is written.
// Optionally, seasonal build pieces and recipes (Yule, Midsummer, Halloween ...) are available all year.

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace BuildTracker
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class BuildTrackerPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.koendertjh.buildtracker";
        public const string ModName = "BuildTracker";
        public const string ModVersion = PluginInfo.Version;

        internal static ManualLogSource Log;

        internal static ConfigEntry<string> TabName;
        internal static ConfigEntry<bool> ShowCategories;
        internal static ConfigEntry<bool> ShowUndiscovered;
        internal static ConfigEntry<string> UndiscoveredText;
        internal static ConfigEntry<string> CongratsText;
        internal static ConfigEntry<string> CongratsSoFarText;
        internal static ConfigEntry<bool> SeasonalPieces;
        internal static ConfigEntry<bool> SeasonalRecipes;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            TabName = Config.Bind("General", "TabName", "Not built",
                "Label of the build menu tab. Takes effect after restarting the game.");
            ShowCategories = Config.Bind("General", "ShowCategories", true,
                "Show the category list (Building, Furniture, ...) on the left of the tab, " +
                "only with categories that still have unbuilt pieces.");
            ShowUndiscovered = Config.Bind("General", "ShowUndiscovered", true,
                "Show how many pieces of the tool (or selected category) you have not discovered yet.");
            UndiscoveredText = Config.Bind("General", "UndiscoveredText", "{0} not discovered yet",
                "Text of the undiscovered indicator; {0} is the number of pieces.");
            CongratsText = Config.Bind("General", "CongratsText", "Congrats! You have built every piece of it!",
                "Shown in the tab when every piece of the tool has been built.");
            CongratsSoFarText = Config.Bind("General", "CongratsSoFarText",
                "Congrats! You have built every piece you have discovered so far. There are still more pieces to build that you have not discovered yet!",
                "Shown in the tab when every discovered piece has been built, but some are still undiscovered.");

            SeasonalPieces = Config.Bind("Seasonal", "SeasonalPieces", false,
                "Seasonal build pieces (Yule, Midsummer, Halloween ...) can be built all year, not only during " +
                "their event. They then also count in the Not built tab.");
            SeasonalRecipes = Config.Bind("Seasonal", "SeasonalRecipes", false,
                "Seasonal recipes (like the Yule hat and the Midsummer crown) can be crafted all year.");
            SeasonalPieces.SettingChanged += (_, __) => Seasons.Apply();
            SeasonalRecipes.SettingChanged += (_, __) => Seasons.Apply();

            _harmony = new Harmony(ModGuid);
            _harmony.PatchAll(typeof(BuildTrackerPlugin).Assembly);
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            Seasons.Restore();
        }
    }
}
