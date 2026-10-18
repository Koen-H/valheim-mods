// ComfortHelper - what would raise your comfort, for Valheim 1.0 (Unity 6, BepInEx 5.4.23+)
//
// Comfort is 1, +1 under a roof, plus the comfort of the pieces within 10 m: only the best piece of each comfort
// group counts (one bed, one chair...), and each other piece once (SE_Rested.CalculateComfortLevel).
// 1. When you start resting, a message lists the pieces you know that would raise your comfort here.
// 2. The /comfort chat command shows the same, any time.
// Both also show your best comfort level so far, for Comfort is King (level 20).
// Client-side only; works on vanilla servers.

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace ComfortHelper
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class ComfortHelperPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.koendertjh.comforthelper";
        public const string ModName = "ComfortHelper";
        public const string ModVersion = PluginInfo.Version;

        internal static ManualLogSource Log;

        internal static ConfigEntry<bool> RestingMessage;
        internal static ConfigEntry<float> MessageCooldown;
        internal static ConfigEntry<int> MaxSuggestions;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            RestingMessage = Config.Bind("1 - General", "RestingMessage", true,
                "When you start resting, show which known pieces would raise your comfort here.");
            MessageCooldown = Config.Bind("1 - General", "MessageCooldown", 600f,
                new ConfigDescription("Seconds before the resting message shows again.", new AcceptableValueRange<float>(0f, 3600f)));
            MaxSuggestions = Config.Bind("1 - General", "MaxSuggestions", 4,
                new ConfigDescription("Pieces listed in the resting message (the /comfort command lists all).",
                    new AcceptableValueRange<int>(1, 20)));

            _harmony = new Harmony(ModGuid);
            _harmony.PatchAll(typeof(ComfortHelperPlugin).Assembly);
            Commands.Register();
        }

        private void OnDestroy() => _harmony?.UnpatchSelf();
    }
}
