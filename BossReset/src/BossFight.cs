// Finds the boss fight near the player, what was used to summon it, and removes it.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BossReset
{
    internal class BossFight
    {
        public Character Boss;
        public Vector3 Arena;
        public OfferingBowl Altar;
        public List<Character> Minions = new List<Character>();
        public List<KeyValuePair<ItemDrop, int>> GiveBack = new List<KeyValuePair<ItemDrop, int>>();

        /// <summary>The nearest living boss within BossRange of the point, with its altar and minions; or null.</summary>
        public static BossFight Find(Vector3 point)
        {
            float range = BossResetPlugin.BossRange.Value;
            var boss = Character.GetAllCharacters()
                .Where(c => c != null && c.IsBoss() && !c.IsDead() && Vector3.Distance(c.transform.position, point) < range)
                .OrderBy(c => Vector3.Distance(c.transform.position, point))
                .FirstOrDefault();
            if (boss == null) return null;

            // Where the boss spawned (the altar spawns it up to 40 m from its spawn area), kept in its ZDO.
            var zdo = boss.GetComponent<ZNetView>()?.GetZDO();
            var fight = new BossFight
            {
                Boss = boss,
                Arena = zdo != null ? zdo.GetVec3(ZDOVars.s_spawnPoint, boss.transform.position) : boss.transform.position,
            };
            fight.Altar = FindAltar(fight.Arena);
            fight.GiveBack = SummonItems(fight.Altar);

            if (BossResetPlugin.RemoveMinions.Value)
            {
                var player = Player.m_localPlayer;
                float minionRange = BossResetPlugin.MinionRange.Value;
                fight.Minions = Character.GetAllCharacters()
                    .Where(c => c != null && c != boss && !c.IsPlayer() && !c.IsDead() && !c.IsTamed() && !c.IsBoss() &&
                                c.GetBaseAI() != null && player != null && BaseAI.IsEnemy(c, player) &&
                                Vector3.Distance(c.transform.position, fight.Arena) < minionRange)
                    .ToList();
            }
            return fight;
        }

        /// <summary>The nearest boss altar whose spawn area is within 100 m of the point, or null.</summary>
        private static OfferingBowl FindAltar(Vector3 point) =>
            UnityEngine.Object.FindObjectsByType<OfferingBowl>(FindObjectsSortMode.None)
                .Where(b => b != null && b.m_bossPrefab != null && Utils.DistanceXZ(SpawnArea(b), point) < 100f)
                .OrderBy(b => Utils.DistanceXZ(SpawnArea(b), point))
                .FirstOrDefault();

        // Same as OfferingBowl.GetSpawnPosition, without picking a random spawn point.
        private static Vector3 SpawnArea(OfferingBowl bowl)
        {
            if (bowl.m_spawnPoints.Count > 0 && bowl.m_spawnPoints[0] != null)
                return bowl.m_spawnPoints[0].transform.position;
            return bowl.transform.position + (Vector3)(bowl.transform.localToWorldMatrix * bowl.m_spawnAreaOffset);
        }

        /// <summary>
        /// What summoning used up. Altars with item stands (OfferingBowl.FindItemStands) emptied their stands,
        /// so each empty stand that takes one kind of item gives back that item. Other altars took
        /// m_bossItems of m_bossItem from the inventory.
        /// </summary>
        private static List<KeyValuePair<ItemDrop, int>> SummonItems(OfferingBowl bowl)
        {
            var items = new List<KeyValuePair<ItemDrop, int>>();
            if (bowl == null) return items;

            if (bowl.m_useItemStands)
            {
                foreach (var stand in UnityEngine.Object.FindObjectsByType<ItemStand>(FindObjectsSortMode.None))
                {
                    if (stand == null || Vector3.Distance(bowl.transform.position, stand.transform.position) > bowl.m_itemstandMaxRange ||
                        !stand.gameObject.name.StartsWith(bowl.m_itemStandPrefix, StringComparison.Ordinal) ||
                        stand.HaveAttachment() || stand.m_supportedItems.Count != 1 || stand.m_supportedItems[0] == null)
                        continue;
                    items.Add(new KeyValuePair<ItemDrop, int>(stand.m_supportedItems[0], 1));
                }
                // Same item on several stands: one line with the total.
                return items.GroupBy(i => i.Key)
                    .Select(g => new KeyValuePair<ItemDrop, int>(g.Key, g.Sum(i => i.Value)))
                    .ToList();
            }

            if (bowl.m_bossItem != null && bowl.m_bossItems > 0)
                items.Add(new KeyValuePair<ItemDrop, int>(bowl.m_bossItem, bowl.m_bossItems));
            return items;
        }

        /// <summary>Removes the creature for everyone. A boss that was counted as active is uncounted first.</summary>
        public static bool Remove(Character c)
        {
            try
            {
                var nview = c.GetComponent<ZNetView>();
                if (nview == null || !nview.IsValid()) return false;
                if (!nview.IsOwner()) nview.ClaimOwnership();

                // Like Character.OnDeath: an alerted boss raised the activeBosses key (blocks portals on some
                // world settings); only its death lowers it again.
                if (c.IsBoss() && nview.GetZDO().GetBool(ZDOVars.s_bossCount))
                {
                    ZoneSystem.instance.GetGlobalKey(GlobalKeys.activeBosses, out float active);
                    ZoneSystem.instance.SetGlobalKey(GlobalKeys.activeBosses, Mathf.Max(0f, active - 1f));
                    nview.GetZDO().Set(ZDOVars.s_bossCount, false);
                }
                ZNetScene.instance.Destroy(c.gameObject);
                return true;
            }
            catch (Exception e)
            {
                BossResetPlugin.Log.LogWarning($"Couldn't remove {Name(c)}: {e.Message}");
                return false;
            }
        }

        public static void Give(Player player, ItemDrop prefab, int amount)
        {
            var inv = player.GetInventory();
            if (inv.CanAddItem(prefab.gameObject, amount) && inv.AddItem(prefab.gameObject, amount)) return;

            // Full inventory: drop it at your feet instead.
            var drop = UnityEngine.Object.Instantiate(prefab.gameObject,
                player.transform.position + Vector3.up, Quaternion.identity).GetComponent<ItemDrop>();
            if (drop != null) drop.SetStack(amount);
        }

        public static string Name(Character c) => Localize(c.m_name);

        public static string Localize(string token)
        {
            try { return Localization.instance != null ? Localization.instance.Localize(token) : token; }
            catch { return token; }
        }
    }
}
