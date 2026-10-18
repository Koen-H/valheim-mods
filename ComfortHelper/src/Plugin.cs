// ComfortHelper - what would raise your comfort, for Valheim 1.0 (Unity 6, BepInEx 5.4.23+)
//
// Comfort is 1, +1 under a roof, plus the comfort of the pieces within 10 m: only the best piece of each comfort
// group counts (one bed, one chair...), and each other piece once (SE_Rested.CalculateComfortLevel).
// 1. When you start resting, a message lists the pieces you know that would raise your comfort here.
// 2. The /comfort chat command shows the same, any time.
// 3. A Comfort tab in the build menu lists every comfort piece by comfort group, with check marks on what is
//    within 10 m, and comfort pieces say in their description what they would add here.
// 1 and 2 also show your best comfort level so far, for Comfort is King (level 20).
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
        internal static ConfigEntry<bool> ComfortTab;
        internal static ConfigEntry<string> TabName;
        internal static ConfigEntry<bool> ChecksInAllTabs;
        internal static ConfigEntry<bool> HoverInfo;

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

            ComfortTab = Config.Bind("2 - Build menu", "ComfortTab", true,
                "A Comfort tab in the build menu with every comfort piece, one category per comfort group. Needs a restart.");
            TabName = Config.Bind("2 - Build menu", "TabName", "Comfort", "Name of the tab. Needs a restart.");
            ChecksInAllTabs = Config.Bind("2 - Build menu", "ChecksInAllTabs", false,
                "Also show the check marks on comfort pieces in the other build menu tabs.");
            HoverInfo = Config.Bind("2 - Build menu", "HoverInfo", true,
                "The description of a comfort piece shows its comfort, its group and what it would add where you stand.");

            _harmony = new Harmony(ModGuid);
            _harmony.PatchAll(typeof(ComfortHelperPlugin).Assembly);
            Commands.Register();
        }

        private void OnDestroy() => _harmony?.UnpatchSelf();
    }
}
