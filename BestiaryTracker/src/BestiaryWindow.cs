// The Bestiary window (see Shared/InventoryWindow.cs): every creature The Hunter needs (The Master Hunter on hard
// difficulty or harder), slain ones in colour with their kill count, the others greyed out with where they live.
// Creatures of later biomes stay hidden until the boss before them is defeated, unless ShowAll is on.
// The picture is the creature's trophy; creatures without one get a paw print.

using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Koendertjh.Shared;
using UnityEngine;

namespace BestiaryTracker
{
    internal class CreatureInfo
    {
        public string Name;
        public Sprite Trophy;
        public Heightmap.Biome Biomes;
        public Stage Stage;
    }

    [HarmonyPatch(typeof(InventoryGui))]
    internal static class BestiaryWindow
    {
        private const string Title = "Bestiary";

        // Bosses and the parts of the last boss fight don't spawn on their own.
        private static readonly Dictionary<string, Stage> FixedStages = new Dictionary<string, Stage>
        {
            { "$enemy_eikthyr", Stage.Meadows },
            { "$enemy_gdking", Stage.BlackForest },
            { "$enemy_bonemass", Stage.Swamp },
            { "$enemy_dragon", Stage.Mountain },
            { "$enemy_goblinking", Stage.Plains },
            { "$enemy_seekerqueen", Stage.Mistlands },
            { "$enemy_fader", Stage.Ashlands },
        };

        private static readonly InventoryWindow Window = new InventoryWindow("BestiaryTracker", Title,
            "Every creature to slay for The Hunter", PawIcon, Fill);

        private static Sprite s_paw;
        private static Dictionary<string, CreatureInfo> s_creatures;
        private static ZNetScene s_scene;

        [HarmonyPostfix]
        [HarmonyPatch("Awake")]
        private static void Create(InventoryGui __instance) => Window.Create(__instance, BestiaryTrackerPlugin.Log);

        [HarmonyPostfix]
        [HarmonyPatch(nameof(InventoryGui.Hide))]
        private static void AfterHide() => Window.Close();

        [HarmonyPostfix]
        [HarmonyPatch(nameof(InventoryGui.OnOpenTrophies))]
        private static void AfterOpenTrophies() => Window.Close();

        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        private static bool BeforeUpdate() => !Window.HandleEscape();

        // A paw: a big pad and four toes (y = 0 is the bottom).
        private static Sprite PawIcon() =>
            s_paw != null
                ? s_paw
                : s_paw = ShapeIcon.Create("BestiaryTracker_Paw", (x, y) =>
                    ShapeIcon.Ellipse(x, y, 64f, 42f, 30f, 25f) ||
                    ShapeIcon.Ellipse(x, y, 26f, 76f, 11f, 14f) ||
                    ShapeIcon.Ellipse(x, y, 49f, 96f, 11f, 15f) ||
                    ShapeIcon.Ellipse(x, y, 79f, 96f, 11f, 15f) ||
                    ShapeIcon.Ellipse(x, y, 102f, 76f, 11f, 14f));

        private static string Fill(List<WindowEntry> entries)
        {
            Achievement achievement = Bestiary.Current;
            Dictionary<string, float> kills = AchievementStats.StatsFor(achievement)?.m_enemyStats[0];
            Dictionary<string, CreatureInfo> creatures = Creatures();
            int slain = 0;

            var list = Bestiary.Creatures(achievement)
                .Select(name => creatures.TryGetValue(name, out CreatureInfo info) ? info : Unknown(name))
                .OrderBy(c => c.Stage)
                .ThenBy(c => Localization.instance.Localize(c.Name));

            foreach (CreatureInfo creature in list)
            {
                float count = 0f;
                bool done = kills != null && kills.TryGetValue(creature.Name, out count) && count >= 1f;
                if (done)
                    slain++;
                if (done && !BestiaryTrackerPlugin.ShowSlain.Value)
                    continue;

                bool revealed = done || BestiaryTrackerPlugin.ShowAll.Value || Stages.IsRevealed(creature.Stage);
                string where = creature.Biomes != Heightmap.Biome.None
                    ? "Lives in " + Stages.BiomeName(Stages.Earliest(creature.Biomes))
                    : "Found in special places";
                entries.Add(new WindowEntry
                {
                    Icon = creature.Trophy != null ? creature.Trophy : PawIcon(),
                    Name = Localization.instance.Localize(creature.Name),
                    Description = done
                        ? $"Slain {count:0} time{(count >= 2f ? "s" : "")}"
                        : where + "\n<i>Not slain yet</i>",
                    State = done ? EntryState.Done : revealed ? EntryState.Missing : EntryState.Hidden
                });
            }

            int total = Bestiary.Creatures(achievement).Count;
            string title = achievement != null ? Localization.instance.Localize(achievement.m_name) : Title;
            return $"{title} {slain} / {total}";
        }

        private static CreatureInfo Unknown(string name) =>
            new CreatureInfo { Name = name, Stage = StageOf(name, Heightmap.Biome.None) };

        // Creature name -> trophy and biomes, from the creature prefabs and the world spawners.
        private static Dictionary<string, CreatureInfo> Creatures()
        {
            if (s_creatures != null && s_scene == ZNetScene.instance)
                return s_creatures;

            s_scene = ZNetScene.instance;
            s_creatures = new Dictionary<string, CreatureInfo>();
            if (s_scene == null)
                return s_creatures;

            var biomesByPrefab = new Dictionary<string, Heightmap.Biome>();
            foreach (SpawnSystemList list in Resources.FindObjectsOfTypeAll<SpawnSystemList>())
            {
                foreach (SpawnSystem.SpawnData spawn in list.m_spawners)
                {
                    if (spawn?.m_prefab == null || !spawn.m_enabled)
                        continue;
                    biomesByPrefab.TryGetValue(spawn.m_prefab.name, out Heightmap.Biome biomes);
                    biomesByPrefab[spawn.m_prefab.name] = biomes | spawn.m_biome;
                }
            }

            foreach (GameObject prefab in s_scene.m_prefabs)
            {
                Character character = prefab != null ? prefab.GetComponent<Character>() : null;
                if (character == null)
                    continue;
                if (!s_creatures.TryGetValue(character.m_name, out CreatureInfo info))
                    s_creatures[character.m_name] = info = new CreatureInfo { Name = character.m_name };

                if (biomesByPrefab.TryGetValue(prefab.name, out Heightmap.Biome biomes))
                    info.Biomes |= biomes;
                if (info.Trophy == null && prefab.GetComponent<CharacterDrop>() is CharacterDrop drops)
                {
                    foreach (CharacterDrop.Drop drop in drops.m_drops)
                    {
                        ItemDrop item = drop.m_prefab != null ? drop.m_prefab.GetComponent<ItemDrop>() : null;
                        if (item != null && item.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophy)
                        {
                            info.Trophy = item.m_itemData.GetIcon();
                            break;
                        }
                    }
                }
            }

            foreach (CreatureInfo info in s_creatures.Values)
                info.Stage = StageOf(info.Name, info.Biomes);
            return s_creatures;
        }

        private static Stage StageOf(string name, Heightmap.Biome biomes)
        {
            if (FixedStages.TryGetValue(name, out Stage stage))
                return stage;
            if (name.StartsWith("$enemy_aspect_") || name.StartsWith("$enemy_frozenking"))
                return Stage.DeepNorth;
            if (biomes != Heightmap.Biome.None)
                return Stages.Earliest(biomes);
            // Creatures only found in dungeons and special places: not before the Plains, to avoid spoilers.
            return Stage.Plains;
        }
    }
}
