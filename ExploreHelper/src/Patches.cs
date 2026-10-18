// 1. PlayerProfile.IncrementStat: a far-direction stat that just reached one second gets a message.
// 2. Minimap.Update: keeps the four pins on the map and crosses them off. The pins are not saved, so they are
//    re-added whenever the map was cleared (loading a world).

using System.Collections.Generic;
using HarmonyLib;

namespace ExploreHelper
{
    [HarmonyPatch]
    internal static class Patches
    {
        private static readonly AccessTools.FieldRef<Minimap, List<Minimap.PinData>> PinsRef =
            AccessTools.FieldRefAccess<Minimap, List<Minimap.PinData>>("m_pins");

        private static readonly Minimap.PinData[] s_pins = new Minimap.PinData[Directions.All.Length];

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerProfile), nameof(PlayerProfile.IncrementStat))]
        private static void BeforeStat(PlayerProfile __instance, PlayerStatType stat, out bool __state)
        {
            Direction direction = Directions.ByStat(stat, out bool noMap);
            __state = direction != null && __instance == Game.instance?.GetPlayerProfile() &&
                      Directions.Value(noMap ? Directions.Pathfinder : Directions.Explorer, stat) < 1f;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerProfile), nameof(PlayerProfile.IncrementStat))]
        private static void AfterStat(PlayerStatType stat, bool __state)
        {
            // __state: a far-direction stat that wasn't reached yet.
            if (!__state || !ExploreHelperPlugin.ReachedMessage.Value || Player.m_localPlayer == null)
                return;

            Direction direction = Directions.ByStat(stat, out bool noMap);
            Achievement achievement = noMap ? Directions.Pathfinder : Directions.Explorer;
            if (achievement == null || Directions.Value(achievement, stat) < 1f)
                return;

            string title = Localization.instance.Localize(achievement.m_name);
            Player.m_localPlayer.Message(MessageHud.MessageType.Center,
                $"You reached the {direction.Name.ToLowerInvariant()}! ({title}: {Directions.ReachedCount(achievement, noMap)} / 4)");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Minimap), "Update")]
        private static void UpdatePins(Minimap __instance)
        {
            if (Game.instance == null || Player.m_localPlayer == null)
                return;

            List<Minimap.PinData> pins = PinsRef(__instance);
            for (int i = 0; i < Directions.All.Length; i++)
            {
                Direction direction = Directions.All[i];
                Minimap.PinData pin = s_pins[i];
                bool present = pin != null && pins.Contains(pin);

                if (!ExploreHelperPlugin.ShowPins.Value)
                {
                    if (present)
                        __instance.RemovePin(pin);
                    s_pins[i] = null;
                    continue;
                }

                bool reached = Directions.Reached(direction);
                if (!present)
                    s_pins[i] = __instance.AddPin(direction.PinPosition, Minimap.PinType.Icon3, direction.Name, false, reached);
                else if (pin.m_checked != reached)
                    pin.m_checked = reached;
            }
        }
    }
}
