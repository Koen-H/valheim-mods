// Chum: one item per fishing bait, crafted by hand from that bait. Using it while facing water spawns a few
// fish that take that bait and live in the biome of that water.
//
// The chum prefabs are copies of the bait prefabs, kept under an inactive holder so they never wake up, and
// added to ObjectDB (items and recipes) and ZNetScene (network prefabs) before those build their lookups.
// Fish are ordinary game fish, so other players see and catch them without the mod.

using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace FishTracker
{
    [HarmonyPatch]
    internal static class Chum
    {
        private const string PrefabPrefix = "Chum_";
        private const string TetraPrefab = "Fish4_cave";

        private static GameObject s_holder;
        // Chum prefab -> its bait prefab.
        private static readonly Dictionary<GameObject, GameObject> s_chums = new Dictionary<GameObject, GameObject>();
        // Chum item name -> bait item name.
        private static readonly Dictionary<string, string> s_baitOf = new Dictionary<string, string>();
        private static readonly List<Recipe> s_recipes = new List<Recipe>();

        internal static string BaitOf(string chumName) => s_baitOf.TryGetValue(chumName, out string bait) ? bait : null;

        // ---- registration ----

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ObjectDB), "Awake")]
        private static void ObjectDBAwake(ObjectDB __instance) => AddToObjectDB(__instance);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
        private static void ObjectDBCopy(ObjectDB __instance)
        {
            if (AddToObjectDB(__instance))
                AccessTools.Method(typeof(ObjectDB), "UpdateRegisters").Invoke(__instance, null);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        private static void ZNetSceneAwake(ZNetScene __instance)
        {
            if (!FishTrackerPlugin.ChumEnabled.Value)
                return;
            CreateChums(__instance.m_prefabs);
            foreach (GameObject chum in s_chums.Keys)
            {
                if (!__instance.m_prefabs.Contains(chum))
                    __instance.m_prefabs.Add(chum);
            }
        }

        /// <summary>Adds the chum items and recipes; true when something was added.</summary>
        private static bool AddToObjectDB(ObjectDB db)
        {
            if (!FishTrackerPlugin.ChumEnabled.Value || db.m_items == null || db.m_items.Count == 0)
                return false;

            CreateChums(db.m_items);
            bool added = false;
            foreach (GameObject chum in s_chums.Keys)
            {
                if (!db.m_items.Contains(chum))
                {
                    db.m_items.Add(chum);
                    added = true;
                }
            }
            foreach (Recipe recipe in s_recipes)
            {
                if (!db.m_recipes.Contains(recipe))
                    db.m_recipes.Add(recipe);
            }
            return added;
        }

        // Every item some fish takes as bait gets a chum. Done once, from whichever prefab list comes first.
        private static void CreateChums(List<GameObject> prefabs)
        {
            if (s_holder != null)
                return;

            var baits = new List<GameObject>();
            foreach (GameObject prefab in prefabs)
            {
                Fish fish = prefab != null ? prefab.GetComponent<Fish>() : null;
                if (fish == null)
                    continue;
                foreach (Fish.BaitSetting bait in fish.m_baits)
                {
                    if (bait.m_bait != null && !baits.Contains(bait.m_bait.gameObject))
                        baits.Add(bait.m_bait.gameObject);
                }
            }
            if (baits.Count == 0)
                return;

            s_holder = new GameObject("FishTracker_Chum");
            s_holder.SetActive(false);
            Object.DontDestroyOnLoad(s_holder);

            foreach (GameObject bait in baits)
            {
                GameObject chum = Object.Instantiate(bait, s_holder.transform);
                chum.name = PrefabPrefix + bait.name;

                ItemDrop.ItemData.SharedData baitShared = bait.GetComponent<ItemDrop>().m_itemData.m_shared;
                ItemDrop.ItemData.SharedData shared = chum.GetComponent<ItemDrop>().m_itemData.m_shared;
                shared.m_name = ChumName(baitShared.m_name);
                shared.m_description = "Bait, mashed up and ready to throw into the water. Use it while facing the " +
                                       "water and the fish that take this bait come to you.";
                shared.m_itemType = ItemDrop.ItemData.ItemType.Material;
                shared.m_ammoType = "";
                shared.m_maxStackSize = 20;
                s_chums[chum] = bait;
                s_baitOf[shared.m_name] = baitShared.m_name;

                Recipe recipe = ScriptableObject.CreateInstance<Recipe>();
                recipe.name = "Recipe_" + chum.name;
                recipe.m_item = chum.GetComponent<ItemDrop>();
                recipe.m_amount = 1;
                recipe.m_enabled = true;
                recipe.m_minStationLevel = 1;
                recipe.m_resources = new[]
                {
                    new Piece.Requirement
                    {
                        m_resItem = bait.GetComponent<ItemDrop>(),
                        m_amount = FishTrackerPlugin.BaitPerChum.Value,
                        m_recover = true
                    }
                };
                s_recipes.Add(recipe);
            }
            FishTrackerPlugin.Log.LogInfo($"Added {s_chums.Count} kinds of chum.");
        }

        // "Mossy Fishing Bait" -> "Mossy Chum".
        private static string ChumName(string baitName)
        {
            string name = Localization.instance != null ? Localization.instance.Localize(baitName) : baitName;
            if (name.StartsWith("$"))
                return "Chum";
            return name.Contains("Fishing Bait") ? name.Replace("Fishing Bait", "Chum") : name + " Chum";
        }

        // ---- use ----

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UseItem))]
        private static bool UseItem(Humanoid __instance, Inventory inventory, ItemDrop.ItemData item)
        {
            if (item?.m_shared == null || !s_baitOf.TryGetValue(item.m_shared.m_name, out string bait) ||
                __instance != Player.m_localPlayer)
                return true;

            Player player = Player.m_localPlayer;
            if (inventory == null)
                inventory = player.GetInventory();
            if (TrySpawnFish(player, bait))
                inventory.RemoveOneItem(item);
            return false;
        }

        private static bool TrySpawnFish(Player player, string bait)
        {
            if (!FindWater(player, out Vector3 point, out float waterLevel))
            {
                player.Message(MessageHud.MessageType.Center, "Face deeper water to throw the chum in.");
                return false;
            }

            Heightmap.Biome biome = WorldGenerator.instance.GetBiome(point);
            var candidates = new List<FishInfo>();
            foreach (FishInfo fish in FishData.All)
            {
                if (fish.Baits.ContainsKey(bait) && LivesIn(fish, biome))
                    candidates.Add(fish);
            }
            if (candidates.Count == 0)
            {
                player.Message(MessageHud.MessageType.Center, "No fish here take this chum. Try another biome.");
                return false;
            }

            for (int i = 0; i < FishTrackerPlugin.FishPerChum.Value; i++)
            {
                FishInfo fish = Pick(candidates, bait);
                Vector2 offset = Random.insideUnitCircle * 3f;
                var position = new Vector3(point.x + offset.x, waterLevel - 1f, point.z + offset.y);
                GameObject spawned = Object.Instantiate(fish.Prefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                int level = RollLevel();
                if (level > 1)
                    spawned.GetComponent<ItemDrop>()?.SetQuality(level);
            }
            player.Message(MessageHud.MessageType.Center, "The water stirs...");
            return true;
        }

        private static bool LivesIn(FishInfo fish, Heightmap.Biome biome) =>
            (fish.Biomes & biome) != 0 ||
            (biome == Heightmap.Biome.DeepNorth && FishTrackerPlugin.TetraInDeepNorth.Value && fish.Prefab.name == TetraPrefab);

        // A point in front of the player with at least 2 m of water.
        private static bool FindWater(Player player, out Vector3 point, out float waterLevel)
        {
            Vector3 forward = player.transform.forward;
            forward.y = 0f;
            forward.Normalize();

            for (float distance = 4f; distance <= 14f; distance += 2f)
            {
                point = player.transform.position + forward * distance;
                waterLevel = Floating.GetLiquidLevel(point, 1f, LiquidType.Water);
                float ground = ZoneSystem.instance.GetGroundHeight(point);
                if (waterLevel - ground >= 2f)
                    return true;
            }
            point = Vector3.zero;
            waterLevel = 0f;
            return false;
        }

        // Weighted by how much the fish likes the bait, with a bonus for fish not caught yet.
        private static FishInfo Pick(List<FishInfo> candidates, string bait)
        {
            float total = 0f;
            var weights = new float[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
            {
                float weight = Mathf.Max(0.05f, candidates[i].Baits[bait]);
                if (!FishData.IsCaught(candidates[i]))
                    weight *= FishTrackerPlugin.UncaughtWeight.Value;
                weights[i] = weight;
                total += weight;
            }

            float roll = Random.Range(0f, total);
            for (int i = 0; i < candidates.Count; i++)
            {
                roll -= weights[i];
                if (roll <= 0f)
                    return candidates[i];
            }
            return candidates[candidates.Count - 1];
        }

        private static int RollLevel()
        {
            int level = 1;
            while (level < FishTrackerPlugin.MaxLevel.Value && Random.value < FishTrackerPlugin.LevelUpChance.Value)
                level++;
            return level;
        }
    }
}
