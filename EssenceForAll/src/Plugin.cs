// EssenceForAll - guaranteed essence drops for Valheim 1.0, scaled by group size (Unity 6, BepInEx 5.4.23+)
//
// Every essence in a creature's drop list (Thunderblood, Frostfire, ...) always drops, and its amount is
// multiplied by the number of players that were near the creature when it spawned. That count is stored
// on the creature itself (its ZDO), so it survives the area unloading and players leaving.
// Loot is rolled by the game that owns the creature, so the mod only acts there.

using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace EssenceForAll
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class EssenceForAllPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.koendertjh.essenceforall";
        public const string ModName = "EssenceForAll";
        public const string ModVersion = PluginInfo.Version;

        internal static ManualLogSource Log;

        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<string> EssenceItems;
        internal static ConfigEntry<bool> MultiplyByPlayers;
        internal static ConfigEntry<float> PlayerRange;
        internal static ConfigEntry<bool> LogDrops;

        private static HashSet<string> s_essences = new HashSet<string>();

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            Enabled = Config.Bind("General", "Enabled", true, "Master switch.");
            EssenceItems = Config.Bind("General", "EssenceItems", "OrbThunderBlood,OrbFrostFire",
                "Comma-separated item prefab names that always drop and are multiplied by the player count.");
            MultiplyByPlayers = Config.Bind("General", "MultiplyByPlayers", true,
                "If true, the essence amount is multiplied by the number of players near the creature when it spawned.");
            PlayerRange = Config.Bind("General", "PlayerRange", 200f,
                "Players within this many meters of a creature when it spawns are counted. 0 = every player in the world.");
            LogDrops = Config.Bind("General", "LogDrops", false, "Logs every boosted essence drop (for troubleshooting).");

            ParseEssences();
            EssenceItems.SettingChanged += (_, __) => ParseEssences();

            _harmony = new Harmony(ModGuid);
            _harmony.PatchAll(typeof(EssenceForAllPlugin).Assembly);
        }

        private void OnDestroy() => _harmony?.UnpatchSelf();

        private static void ParseEssences() =>
            s_essences = new HashSet<string>(EssenceItems.Value.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0),
                StringComparer.OrdinalIgnoreCase);

        internal static bool IsEssence(CharacterDrop.Drop drop) => drop?.m_prefab != null && s_essences.Contains(drop.m_prefab.name);
    }
}
