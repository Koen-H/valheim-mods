// ModdedAchievements - earn achievements in a modded game, for Valheim 1.0 (Unity 6, BepInEx 5.4.23+)
//
// Valheim asks mods to set Game.isModded (for the "Modded" text in the main menu and support logs), and
// Achievements.IsCheatedAtAll treats a modded game like a cheated one: no achievements, no stats for them.
// While IsCheatedAtAll runs, isModded reads false, so only real cheating counts: dev commands used by the
// character, cheated world modifiers and cheated items in the inventory. The flag itself stays set.

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace ModdedAchievements
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class ModdedAchievementsPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.koendertjh.moddedachievements";
        public const string ModName = "ModdedAchievements";
        public const string ModVersion = PluginInfo.Version;

        internal static ManualLogSource Log;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            _harmony = new Harmony(ModGuid);
            _harmony.PatchAll(typeof(ModdedAchievementsPlugin).Assembly);
        }

        private void OnDestroy() => _harmony?.UnpatchSelf();
    }

    [HarmonyPatch(typeof(Achievements), nameof(Achievements.IsCheatedAtAll))]
    internal static class IgnoreModdedFlag
    {
        private static void Prefix(out bool __state)
        {
            __state = Game.isModded;
            Game.isModded = false;
        }

        private static void Finalizer(bool __state) => Game.isModded = __state;
    }
}
