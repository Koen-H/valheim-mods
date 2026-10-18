// Has the player come across the thing an achievement condition is about? Decides which missing
// conditions MissingRows = Discovered shows.
//
// Condition keys are the names the game counts stats by:
// - PlayerStatType names (deaths, distance, comfort...): nothing to discover, always shown.
// - creature names ($enemy_*): killed on any difficulty, or one of its drops (its trophy too) is known.
// - fish names ($animal_fish*): the fish item is known.
// - item and piece names: the recipe, piece or material is known.

using System;
using System.Collections.Generic;
using UnityEngine;
using Koendertjh.Shared;

namespace AchievementPeek
{
    internal static class Discovery
    {
        // Creature name -> shared names of the items it can drop.
        private static Dictionary<string, HashSet<string>> s_creatureDrops;
        // Fish name -> shared name of its item.
        private static Dictionary<string, string> s_fishItems;
        private static ZNetScene s_scene;

        internal static bool IsStat(string key) => Enum.TryParse(key, out PlayerStatType _);

        internal static bool IsDiscovered(string key)
        {
            if (IsStat(key))
                return true;

            Player player = Player.m_localPlayer;
            if (player == null)
                return false;

            Build();
            if (s_creatureDrops.TryGetValue(key, out HashSet<string> drops))
            {
                if (BossProgress.HasKilled(key))
                    return true;
                foreach (string drop in drops)
                {
                    if (player.IsMaterialKnown(drop))
                        return true;
                }
                return false;
            }
            if (s_fishItems.TryGetValue(key, out string fishItem) && player.IsMaterialKnown(fishItem))
                return true;

            return player.IsRecipeKnown(key) || player.IsMaterialKnown(key);
        }

        private static void Build()
        {
            if (s_creatureDrops != null && s_scene == ZNetScene.instance)
                return;

            s_creatureDrops = new Dictionary<string, HashSet<string>>();
            s_fishItems = new Dictionary<string, string>();
            s_scene = ZNetScene.instance;
            if (s_scene == null)
                return;

            foreach (GameObject prefab in s_scene.m_prefabs)
            {
                if (prefab == null)
                    continue;

                Character character = prefab.GetComponent<Character>();
                if (character != null)
                {
                    if (!s_creatureDrops.TryGetValue(character.m_name, out HashSet<string> drops))
                        s_creatureDrops[character.m_name] = drops = new HashSet<string>();

                    CharacterDrop characterDrop = prefab.GetComponent<CharacterDrop>();
                    if (characterDrop != null)
                    {
                        foreach (CharacterDrop.Drop drop in characterDrop.m_drops)
                        {
                            ItemDrop item = drop.m_prefab != null ? drop.m_prefab.GetComponent<ItemDrop>() : null;
                            if (item != null)
                                drops.Add(item.m_itemData.m_shared.m_name);
                        }
                    }
                }

                Fish fish = prefab.GetComponent<Fish>();
                ItemDrop fishDrop = prefab.GetComponent<ItemDrop>();
                if (fish != null && fishDrop != null)
                    s_fishItems[fish.m_name] = fishDrop.m_itemData.m_shared.m_name;
            }
        }
    }
}
