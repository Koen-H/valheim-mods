// SummonTweaks - friendlier summons for Valheim 1.0 (Unity 6, BepInEx 5.4.23+)
//
// 1. Players walk through summons (Dead Raiser skeletons, Spirit Caller spirits). Only the physical push
//    between a summon and a player is turned off: summons still stand on the ground and bump into
//    everything else, and attacks and projectiles still hit (those use their own hit checks).
//    The Trollstav troll is left alone.
// 2. The Spirit Caller picks a random spirit, and each kind is capped by the staff's quality: rolling a
//    kind you already have unsummons the old one. The mod rolls only among spirits you can still add.

using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace SummonTweaks
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class SummonTweaksPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.koendertjh.summontweaks";
        public const string ModName = "SummonTweaks";
        public const string ModVersion = PluginInfo.Version;

        internal static ManualLogSource Log;

        internal static ConfigEntry<bool> WalkThroughSummons;
        internal static ConfigEntry<string> SolidSummons;
        internal static ConfigEntry<bool> SmartSpiritCaller;

        private static HashSet<string> s_solid = new HashSet<string>();

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            WalkThroughSummons = Config.Bind("Collision", "WalkThroughSummons", true,
                "Players walk through summons instead of bumping into them. Attacks and projectiles still hit.");
            SolidSummons = Config.Bind("Collision", "SolidSummons", "Troll_Summoned",
                "Comma-separated creature prefab names that stay solid for players.");
            SmartSpiritCaller = Config.Bind("Spawning", "SmartSpiritCaller", true,
                "Summoning staffs with several creatures (Spirit Caller) only roll creatures you can add without " +
                "unsummoning one you already have. When every kind is at its limit, the game's normal roll is used.");

            ParseSolid();
            SolidSummons.SettingChanged += (_, __) => ParseSolid();

            _harmony = new Harmony(ModGuid);
            _harmony.PatchAll(typeof(SummonTweaksPlugin).Assembly);
        }

        private void OnDestroy() => _harmony?.UnpatchSelf();

        private static void ParseSolid() =>
            s_solid = new HashSet<string>(SolidSummons.Value.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0),
                StringComparer.OrdinalIgnoreCase);

        internal static bool IsSolid(string prefabName) => s_solid.Contains(prefabName);
    }
}
