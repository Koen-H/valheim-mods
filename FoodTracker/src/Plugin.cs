// FoodTracker - which foods you still have to eat or cook, for Valheim 1.0 (Unity 6, BepInEx 5.4.23+)
//
// The Gourmet asks you to eat every edible item, The Master Chef to cook every dish. The lists come from
// those achievements, so they match what the game counts.
// 1. Item tooltips say when you haven't eaten or made the item yet, with your progress.
// 2. Inventory and chest slots get a small marker on food you haven't eaten yet.
// Client-side only; works on vanilla servers.

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace FoodTracker
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class FoodTrackerPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.koendertjh.foodtracker";
        public const string ModName = "FoodTracker";
        public const string ModVersion = PluginInfo.Version;

        internal static ManualLogSource Log;

        internal static ConfigEntry<bool> TooltipHints;
        internal static ConfigEntry<bool> ShowProgress;
        internal static ConfigEntry<bool> InventoryMarkers;
        internal static ConfigEntry<Color> MarkerColor;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            TooltipHints = Config.Bind("1 - General", "TooltipHints", true,
                "Item tooltips say when you haven't eaten or made the item yet.");
            ShowProgress = Config.Bind("1 - General", "ShowProgress", true,
                "Tooltips of food also show your progress for The Gourmet and The Master Chef.");
            InventoryMarkers = Config.Bind("2 - Inventory", "InventoryMarkers", true,
                "A small diamond on the left of inventory and chest slots holding food you haven't eaten yet.");
            MarkerColor = Config.Bind("2 - Inventory", "MarkerColor", new Color(1f, 0.7f, 0.28f),
                "Colour of the inventory marker.");

            _harmony = new Harmony(ModGuid);
            _harmony.PatchAll(typeof(FoodTrackerPlugin).Assembly);
        }

        private void OnDestroy() => _harmony?.UnpatchSelf();
    }
}
