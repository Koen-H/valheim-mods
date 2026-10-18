// BossReset - reset a boss fight that went wrong, for Valheim 1.0 (Unity 6, BepInEx 5.4.23+)
//
// The bossreset command removes the nearest living boss and the hostile creatures around its arena
// (its minions), and gives back what was used to summon it at the altar, so the fight can be started over.
// Works for every boss, also ones summoned before the mod was installed.

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace BossReset
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class BossResetPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.koendertjh.bossreset";
        public const string ModName = "BossReset";
        public const string ModVersion = PluginInfo.Version;

        internal static ManualLogSource Log;

        internal static ConfigEntry<float> BossRange;
        internal static ConfigEntry<float> MinionRange;
        internal static ConfigEntry<bool> RemoveMinions;

        private void Awake()
        {
            Log = Logger;

            BossRange = Config.Bind("Reset", "BossRange", 150f,
                "How close (meters) a living boss has to be for bossreset to find it.");
            RemoveMinions = Config.Bind("Reset", "RemoveMinions", true,
                "Also remove the hostile creatures around the arena (the boss's minions).");
            MinionRange = Config.Bind("Reset", "MinionRange", 80f,
                "Hostile creatures within this many meters of the arena are removed with the boss.");

            Commands.Register();
        }
    }
}
