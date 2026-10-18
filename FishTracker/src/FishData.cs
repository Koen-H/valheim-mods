// What the game knows about fish: which bait each fish takes, in which biomes it lives, and which fish this
// character has caught for The Fisherman (counted by the fish's name, e.g. "$animal_fish1").

using System.Collections.Generic;
using Koendertjh.Shared;
using UnityEngine;

namespace FishTracker
{
    internal class FishInfo
    {
        public GameObject Prefab;
        public Fish Fish;
        public string ItemName;
        public Heightmap.Biome Biomes;
        // Bait item shared name -> chance the fish takes it.
        public readonly Dictionary<string, float> Baits = new Dictionary<string, float>();
    }

    internal static class FishData
    {
        internal const string FishermanId = "GrindFish";

        private static List<FishInfo> s_fish;
        private static bool s_biomesLoaded;
        private static Achievement s_fisherman;

        internal static Achievement Fisherman => s_fisherman != null ? s_fisherman : s_fisherman = AchievementStats.Find(FishermanId);

        internal static List<FishInfo> All
        {
            get
            {
                if (s_fish == null || s_fish.Count == 0)
                    Build();
                if (!s_biomesLoaded)
                    LoadBiomes();
                return s_fish;
            }
        }

        internal static bool IsCaught(FishInfo fish) =>
            AchievementStats.Has(AchievementStats.StatsFor(Fisherman)?.m_pickableStats, fish.Fish.m_name);

        internal static FishInfo ByItemName(string itemName) => All.Find(f => f.ItemName == itemName);

        internal static int CaughtCount()
        {
            HashSet<string> keys = AchievementStats.Keys(Fisherman?.m_pickableTriggers);
            return AchievementStats.CountDone(keys, AchievementStats.StatsFor(Fisherman)?.m_pickableStats);
        }

        internal static int TotalCount() => AchievementStats.Keys(Fisherman?.m_pickableTriggers).Count;

        private static void Build()
        {
            s_fish = new List<FishInfo>();
            if (ObjectDB.instance == null)
                return;

            foreach (GameObject prefab in ObjectDB.instance.m_items)
            {
                Fish fish = prefab != null ? prefab.GetComponent<Fish>() : null;
                ItemDrop item = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
                if (fish == null || item == null)
                    continue;

                var info = new FishInfo { Prefab = prefab, Fish = fish, ItemName = item.m_itemData.m_shared.m_name };
                foreach (Fish.BaitSetting bait in fish.m_baits)
                {
                    if (bait.m_bait != null)
                        info.Baits[bait.m_bait.m_itemData.m_shared.m_name] = bait.m_chance;
                }
                s_fish.Add(info);
            }
            s_biomesLoaded = false;
        }

        // Where fish live comes from the world spawners, which only exist once a world is loaded.
        private static void LoadBiomes()
        {
            SpawnSystemList[] lists = Resources.FindObjectsOfTypeAll<SpawnSystemList>();
            if (lists.Length == 0)
                return;
            s_biomesLoaded = true;

            foreach (SpawnSystemList list in lists)
            {
                foreach (SpawnSystem.SpawnData spawn in list.m_spawners)
                {
                    if (spawn?.m_prefab == null || !spawn.m_enabled)
                        continue;
                    FishInfo info = s_fish.Find(f => f.Prefab.name == spawn.m_prefab.name);
                    if (info != null)
                        info.Biomes |= spawn.m_biome;
                }
            }
        }
    }
}
