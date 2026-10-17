// UnlockAwareness - marks newly unlocked recipes, build pieces and items for Valheim 1.0 (Unity 6, BepInEx 5.4.23+)
//
// Every recipe or piece the character learns gets a "!" badge in the crafting panel or build menu
// until it is selected, or cleared with the "Mark all as seen" button. Items picked up for the first
// time get the same badge in the inventory until clicked. The unseen list is stored in the
// character's own save (Player.m_customData). Client-side only.

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace UnlockAwareness
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class UnlockAwarenessPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.koendertjh.unlockawareness";
        public const string ModName = "UnlockAwareness";
        public const string ModVersion = PluginInfo.Version;

        internal static ManualLogSource Log;

        internal static ConfigEntry<bool> FlagAllKnownOnce;
        internal static ConfigEntry<bool> InventoryBadges;
        internal static ConfigEntry<Color> BadgeColor;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            FlagAllKnownOnce = Config.Bind("General", "FlagAllKnownOnce", false,
                "If true, the first time each character loads in, every recipe and piece it already knows is marked as new. " +
                "Runs once per character; use 'Mark all as seen' to clear them afterwards.");
            InventoryBadges = Config.Bind("General", "InventoryBadges", true,
                "If true, items you pick up for the first time get a '!' marker in your inventory until you click them. " +
                "If false, no inventory markers are shown and new items are not recorded.");
            BadgeColor = Config.Bind("Badge", "Color", new Color(1f, 0.62f, 0.1f, 1f),
                "Color of the '!' marker shown on newly unlocked recipes, pieces and items.");

            _harmony = new Harmony(ModGuid);
            _harmony.PatchAll(typeof(UnlockAwarenessPlugin).Assembly);
        }

        private void OnDestroy() => _harmony?.UnpatchSelf();
    }
}
