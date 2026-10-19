// Hand catches: picking up a fish by hand (one that flopped onto land) counts as catching it, the same way a
// catch with the rod does: The Fisherman, A Catch Worthy of Njord and the "fish caught" stats.
//
// FishingFloat.Catch also ends in Humanoid.Pickup, sometimes a few frames later when the fish belongs to
// another player, so fish caught with the rod are remembered and skipped here.

using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace FishTracker
{
    [HarmonyPatch]
    internal static class HandCatch
    {
        private static readonly HashSet<GameObject> s_rodCatches = new HashSet<GameObject>();

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FishingFloat), nameof(FishingFloat.Catch))]
        private static void RodCatch(Fish fish)
        {
            s_rodCatches.RemoveWhere(go => go == null);
            if (fish != null)
                s_rodCatches.Add(fish.gameObject);
        }

        // The fish is destroyed by the pickup, so read it first.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.Pickup))]
        private static void PickupPrefix(Humanoid __instance, GameObject go, out Fish __state)
        {
            __state = null;
            if (!FishTrackerPlugin.HandCatchCounts.Value || go == null || __instance != Player.m_localPlayer ||
                s_rodCatches.Remove(go))
                return;
            __state = go.GetComponent<Fish>();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.Pickup))]
        private static void PickupPostfix(bool __result, Fish __state)
        {
            if (!__result || __state == null || Game.instance == null)
                return;

            // The same stats FishingFloat.Catch counts.
            Game.instance.GetPlayerProfile().IncrementStatPickable(__state.GetHoverName());
            Game.instance.IncrementPlayerStat(PlayerStatType.FishCaught);
            ItemDrop item = __state.GetComponent<ItemDrop>();
            if (item != null && item.m_itemData.m_quality <= 6)
                Game.instance.IncrementPlayerStat((PlayerStatType)(161 + item.m_itemData.m_quality));
        }
    }
}
