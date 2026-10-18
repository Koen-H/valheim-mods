// 1. EnemyHud.ShowHud creates the name and health bar above a creature once; the marker is added then.
// 2. PlayerProfile.IncrementStatEnemy counts every kill; a creature that wasn't slain before and is now gets
//    a message.

using System.Collections;
using System.Reflection;
using HarmonyLib;
using TMPro;

namespace BestiaryTracker
{
    [HarmonyPatch]
    internal static class Patches
    {
        // Dictionary<Character, HudData>; HudData is private.
        private static readonly FieldInfo HudsField = AccessTools.Field(typeof(EnemyHud), "m_huds");

        private static IDictionary Huds(EnemyHud hud) => (IDictionary)HudsField.GetValue(hud);

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EnemyHud), "ShowHud")]
        private static void BeforeShowHud(EnemyHud __instance, Character c, out bool __state) =>
            __state = c != null && !Huds(__instance).Contains(c);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemyHud), "ShowHud")]
        private static void AfterShowHud(EnemyHud __instance, Character c, bool __state)
        {
            // __state: the hud was created just now.
            if (!__state || !BestiaryTrackerPlugin.NameMarker.Value || c.IsPlayer() || c.IsTamed())
                return;

            Achievement achievement = Bestiary.Current;
            if (!Bestiary.Counts(achievement, c.m_name) || Bestiary.IsSlain(achievement, c.m_name))
                return;

            object hud = Huds(__instance)[c];
            TextMeshProUGUI name = Traverse.Create(hud).Field("m_name").GetValue<TextMeshProUGUI>();
            if (name != null)
                name.text += " " + BestiaryTrackerPlugin.MarkerText.Value;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerProfile), nameof(PlayerProfile.IncrementStatEnemy))]
        private static void BeforeKill(PlayerProfile __instance, string name, out bool __state)
        {
            Achievement achievement = Bestiary.Current;
            __state = __instance == Game.instance?.GetPlayerProfile() && Bestiary.Counts(achievement, name) &&
                      !Bestiary.IsSlain(achievement, name);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerProfile), nameof(PlayerProfile.IncrementStatEnemy))]
        private static void AfterKill(string name, bool __state)
        {
            // __state: a creature that counts and wasn't slain yet.
            Achievement achievement = Bestiary.Current;
            if (!__state || !BestiaryTrackerPlugin.KillMessage.Value || !Bestiary.IsSlain(achievement, name) ||
                Player.m_localPlayer == null)
                return;

            string creature = Localization.instance.Localize(name);
            string title = Localization.instance.Localize(achievement.m_name);
            Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft,
                $"New creature slain: {creature} ({title}: {Bestiary.SlainCount(achievement)} / {Bestiary.Creatures(achievement).Count})");
        }
    }
}
