using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace EssenceForAll
{
    /// <summary>Remembers how many players were around when a creature with essence drops spawned.</summary>
    [HarmonyPatch]
    internal static class SpawnPatches
    {
        internal const string PlayersKey = "EssenceForAll_Players";

        // Character.Awake also runs when a saved creature loads back in; the stored count is kept then.
        [HarmonyPatch(typeof(Character), "Awake")]
        [HarmonyPostfix]
        private static void Awake_Postfix(Character __instance)
        {
            if (__instance.IsPlayer()) return;
            CharacterDrop drops = __instance.GetComponent<CharacterDrop>();
            if (drops == null || !DropsEssence(drops)) return;

            ZNetView nview = __instance.GetComponent<ZNetView>();
            ZDO zdo = nview != null && nview.IsValid() ? nview.GetZDO() : null;
            if (zdo == null || !nview.IsOwner() || zdo.GetInt(PlayersKey) > 0) return;
            zdo.Set(PlayersKey, CountPlayers(__instance.transform.position));
        }

        // Runs for every creature that spawns or loads, so a plain loop instead of LINQ with a new delegate.
        private static bool DropsEssence(CharacterDrop drops)
        {
            foreach (CharacterDrop.Drop drop in drops.m_drops)
            {
                if (EssenceForAllPlugin.IsEssence(drop))
                    return true;
            }
            return false;
        }

        internal static int CountPlayers(Vector3 position)
        {
            float range = EssenceForAllPlugin.PlayerRange.Value;
            if (range <= 0f && ZNet.instance != null) return Mathf.Max(1, ZNet.instance.GetNrOfPlayers());
            int count = Player.GetAllPlayers().Count(p => p != null && Vector3.Distance(p.transform.position, position) <= range);
            return Mathf.Max(1, count);
        }
    }

    /// <summary>Makes every essence drop and multiplies it by the stored player count.</summary>
    [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
    internal static class DropPatches
    {
        [HarmonyPrefix]
        private static void Prefix(CharacterDrop __instance, out List<KeyValuePair<CharacterDrop.Drop, float>> __state)
        {
            __state = null;
            if (!EssenceForAllPlugin.Enabled.Value) return;
            foreach (CharacterDrop.Drop drop in __instance.m_drops)
            {
                if (!EssenceForAllPlugin.IsEssence(drop)) continue;
                (__state ??= new List<KeyValuePair<CharacterDrop.Drop, float>>()).Add(new KeyValuePair<CharacterDrop.Drop, float>(drop, drop.m_chance));
                drop.m_chance = 1f;
            }
        }

        [HarmonyPostfix]
        private static void Postfix(CharacterDrop __instance, List<KeyValuePair<GameObject, int>> __result,
            List<KeyValuePair<CharacterDrop.Drop, float>> __state)
        {
            if (__state == null) return;
            // Put the original chances back: the drop list belongs to this creature, but leave no trace.
            foreach (KeyValuePair<CharacterDrop.Drop, float> saved in __state) saved.Key.m_chance = saved.Value;
            if (__result == null) return;

            int players = 1;
            if (EssenceForAllPlugin.MultiplyByPlayers.Value)
            {
                ZNetView nview = __instance.GetComponent<ZNetView>();
                int stored = nview != null && nview.IsValid() ? nview.GetZDO().GetInt(SpawnPatches.PlayersKey) : 0;
                // Creatures that spawned before the mod was installed have no count; use the players around now.
                players = stored > 0 ? stored : SpawnPatches.CountPlayers(__instance.transform.position);
            }

            var log = EssenceForAllPlugin.LogDrops.Value ? new StringBuilder() : null;
            for (int i = 0; i < __result.Count; i++)
            {
                GameObject prefab = __result[i].Key;
                CharacterDrop.Drop drop = __state.FirstOrDefault(s => s.Key.m_prefab == prefab).Key;
                // "One per player" drops already give every player one.
                if (drop == null || drop.m_onePerPlayer) continue;
                int amount = __result[i].Value * players;
                __result[i] = new KeyValuePair<GameObject, int>(prefab, amount);
                log?.Append($" {prefab.name} x{amount}");
            }
            if (log != null) EssenceForAllPlugin.Log.LogInfo($"{__instance.name}: {players} player(s):{log}");
        }
    }

    /// <summary>Logs which creatures carry essence drops, so the config is easy to check.</summary>
    [HarmonyPatch(typeof(ZNetScene), "Awake")]
    internal static class ScenePatches
    {
        [HarmonyPostfix]
        private static void Postfix(ZNetScene __instance)
        {
            foreach (GameObject prefab in __instance.m_prefabs)
            {
                CharacterDrop drops = prefab != null ? prefab.GetComponent<CharacterDrop>() : null;
                if (drops == null) continue;
                string essences = string.Join(", ", drops.m_drops.Where(EssenceForAllPlugin.IsEssence)
                    .Select(d => $"{d.m_prefab.name} {d.m_amountMin}-{d.m_amountMax} at {d.m_chance:P0}"));
                if (essences.Length > 0) EssenceForAllPlugin.Log.LogInfo($"{prefab.name} drops {essences}");
            }
        }
    }
}
