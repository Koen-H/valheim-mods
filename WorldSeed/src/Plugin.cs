// WorldSeed - the world seed in game, for Valheim 1.0 (Unity 6, BepInEx 5.4.23+)
//
// 1. The big map shows the seed below the biome name.
// 2. The /seed chat command shows it and copies it to the clipboard.
// The seed can be kept secret until a boss is defeated (RequiredBoss). Players joining a server receive the seed
// from it (the game needs it to build the terrain), so this works on vanilla servers too.

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Koendertjh.Shared;

namespace WorldSeed
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class WorldSeedPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.koendertjh.worldseed";
        public const string ModName = "WorldSeed";
        public const string ModVersion = PluginInfo.Version;

        internal static ManualLogSource Log;

        internal static ConfigEntry<bool> ShowOnMap;
        internal static ConfigEntry<Boss> RequiredBoss;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            ShowOnMap = Config.Bind("1 - General", "ShowOnMap", true, "Show the seed on the big map.");
            RequiredBoss = Config.Bind("1 - General", "RequiredBoss", Boss.None,
                "Keep the seed secret until you have defeated this boss.");

            _harmony = new Harmony(ModGuid);
            _harmony.PatchAll(typeof(WorldSeedPlugin).Assembly);
            Commands.Register();
        }

        private void OnDestroy() => _harmony?.UnpatchSelf();

        /// <summary>The seed, or null when there is no world (or the world has none).</summary>
        internal static string Seed
        {
            get
            {
                string seed = ZNet.World?.m_seedName;
                return string.IsNullOrEmpty(seed) ? null : seed;
            }
        }

        internal static bool Unlocked => BossProgress.IsDefeated(RequiredBoss.Value);

        internal static string LockedText =>
            $"Defeat {Localization.instance.Localize(BossProgress.EnemyName(RequiredBoss.Value))} to learn the seed of this world.";
    }
}
