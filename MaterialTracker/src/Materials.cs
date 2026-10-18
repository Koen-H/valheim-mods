// Every material: each item that some enabled recipe or build piece asks for, except trophies. For each one the
// game data tells where it comes from (dropped by, crafted at, smelted from, mined, chopped, picked, sold) and,
// with StageTable, the boss stage that reveals it.

using System.Collections.Generic;
using System.Linq;
using Koendertjh.Shared;
using UnityEngine;

namespace MaterialTracker
{
    internal class MaterialInfo
    {
        public string Name;
        public ItemDrop Item;
        public Stage Stage;
        public string Hint;
    }

    internal static class Materials
    {
        private static List<MaterialInfo> s_all;
        private static ObjectDB s_db;

        // Where things come from, by item name.
        private static readonly Dictionary<string, List<string>> s_droppedBy = new Dictionary<string, List<string>>();
        private static readonly Dictionary<string, Recipe> s_recipes = new Dictionary<string, Recipe>();
        private static readonly Dictionary<string, KeyValuePair<string, ItemDrop>> s_converted = new Dictionary<string, KeyValuePair<string, ItemDrop>>();
        private static readonly Dictionary<string, string> s_found = new Dictionary<string, string>();
        // Creature name -> earliest stage it spawns in.
        private static readonly Dictionary<string, Stage> s_creatureStage = new Dictionary<string, Stage>();
        private static readonly Dictionary<string, Stage> s_stages = new Dictionary<string, Stage>();

        internal static List<MaterialInfo> All
        {
            get
            {
                if (s_all == null || s_db != ObjectDB.instance)
                    Build();
                return s_all;
            }
        }

        private static void Build()
        {
            s_all = new List<MaterialInfo>();
            s_db = ObjectDB.instance;
            if (s_db == null || ZNetScene.instance == null)
                return;

            ReadSources();

            var items = new Dictionary<string, ItemDrop>();
            foreach (Recipe recipe in s_db.m_recipes)
            {
                if (recipe != null && recipe.m_enabled && recipe.m_item != null)
                    AddRequirements(items, recipe.m_resources);
            }
            foreach (GameObject prefab in s_db.m_items)
            {
                PieceTable table = prefab != null ? prefab.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_buildPieces : null;
                if (table == null)
                    continue;
                foreach (GameObject piece in table.m_pieces)
                {
                    Piece component = piece != null ? piece.GetComponent<Piece>() : null;
                    if (component != null && component.m_enabled)
                        AddRequirements(items, component.m_resources);
                }
            }

            foreach (KeyValuePair<string, ItemDrop> pair in items)
            {
                s_all.Add(new MaterialInfo
                {
                    Name = pair.Key,
                    Item = pair.Value,
                    Stage = StageOf(pair.Value, new HashSet<string>()),
                    Hint = HintFor(pair.Key)
                });
            }
            s_all = s_all.OrderBy(m => m.Stage).ThenBy(m => Localization.instance.Localize(m.Name)).ToList();
        }

        private static void AddRequirements(Dictionary<string, ItemDrop> items, Piece.Requirement[] requirements)
        {
            if (requirements == null)
                return;
            foreach (Piece.Requirement requirement in requirements)
            {
                ItemDrop item = requirement?.m_resItem;
                if (item == null || item.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophy)
                    continue;
                items[item.m_itemData.m_shared.m_name] = item;
            }
        }

        private static string NameOf(GameObject prefab) =>
            prefab != null ? prefab.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_name : null;

        private static void Found(GameObject prefab, string how)
        {
            string name = NameOf(prefab);
            if (name != null && !s_found.ContainsKey(name))
                s_found[name] = how;
        }

        private static void FoundAll(DropTable table, string how)
        {
            if (table?.m_drops == null)
                return;
            foreach (DropTable.DropData drop in table.m_drops)
                Found(drop.m_item, how);
        }

        private static void ReadSources()
        {
            s_droppedBy.Clear();
            s_recipes.Clear();
            s_converted.Clear();
            s_found.Clear();
            s_creatureStage.Clear();
            s_stages.Clear();

            foreach (Recipe recipe in s_db.m_recipes)
            {
                string name = recipe?.m_item != null ? recipe.m_item.m_itemData.m_shared.m_name : null;
                if (name != null && recipe.m_enabled && !s_recipes.ContainsKey(name))
                    s_recipes[name] = recipe;
            }

            var spawnStage = new Dictionary<string, Stage>();
            foreach (SpawnSystemList list in Resources.FindObjectsOfTypeAll<SpawnSystemList>())
            {
                foreach (SpawnSystem.SpawnData spawn in list.m_spawners)
                {
                    if (spawn?.m_prefab == null || !spawn.m_enabled)
                        continue;
                    Stage stage = Stages.Earliest(spawn.m_biome);
                    if (!spawnStage.TryGetValue(spawn.m_prefab.name, out Stage known) || stage < known)
                        spawnStage[spawn.m_prefab.name] = stage;
                }
            }

            foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
            {
                if (prefab == null)
                    continue;

                Character character = prefab.GetComponent<Character>();
                CharacterDrop drops = prefab.GetComponent<CharacterDrop>();
                if (character != null && drops != null)
                {
                    if (spawnStage.TryGetValue(prefab.name, out Stage stage) &&
                        (!s_creatureStage.TryGetValue(character.m_name, out Stage known) || stage < known))
                        s_creatureStage[character.m_name] = stage;
                    foreach (CharacterDrop.Drop drop in drops.m_drops)
                    {
                        string item = NameOf(drop.m_prefab);
                        if (item == null)
                            continue;
                        if (!s_droppedBy.TryGetValue(item, out List<string> by))
                            s_droppedBy[item] = by = new List<string>();
                        if (!by.Contains(character.m_name))
                            by.Add(character.m_name);
                    }
                }

                Smelter smelter = prefab.GetComponent<Smelter>();
                if (smelter != null)
                    AddConversions(smelter.m_conversion?.Select(c => (c.m_from, c.m_to)), smelter.m_name);
                Fermenter fermenter = prefab.GetComponent<Fermenter>();
                if (fermenter != null)
                    AddConversions(fermenter.m_conversion?.Select(c => (c.m_from, c.m_to)), fermenter.m_name);
                CookingStation cooking = prefab.GetComponent<CookingStation>();
                if (cooking != null)
                    AddConversions(cooking.m_conversion?.Select(c => (c.m_from, c.m_to)), cooking.m_name);

                FoundAll(prefab.GetComponent<MineRock>()?.m_dropItems, "Mined from rocks and deposits");
                FoundAll(prefab.GetComponent<MineRock5>()?.m_dropItems, "Mined from rocks and deposits");
                FoundAll(prefab.GetComponent<TreeBase>()?.m_dropWhenDestroyed, "Chopped from trees");
                FoundAll(prefab.GetComponent<TreeLog>()?.m_dropWhenDestroyed, "Chopped from trees");
                Pickable pickable = prefab.GetComponent<Pickable>();
                if (pickable != null)
                    Found(pickable.m_itemPrefab, "Picked up in the wild");
                FoundAll(prefab.GetComponent<DropOnDestroyed>()?.m_dropWhenDestroyed, "Found by breaking things in the world");
                Trader trader = prefab.GetComponent<Trader>();
                if (trader != null)
                {
                    foreach (Trader.TradeItem ware in trader.m_items)
                        Found(ware.m_prefab != null ? ware.m_prefab.gameObject : null, "Sold by " + Localization.instance.Localize(trader.m_name));
                }
            }
        }

        private static void AddConversions(IEnumerable<(ItemDrop from, ItemDrop to)> conversions, string station)
        {
            if (conversions == null)
                return;
            foreach ((ItemDrop from, ItemDrop to) in conversions)
            {
                if (from == null || to == null)
                    continue;
                string name = to.m_itemData.m_shared.m_name;
                if (!s_converted.ContainsKey(name))
                    s_converted[name] = new KeyValuePair<string, ItemDrop>(station ?? "", from);
            }
        }

        // Table first, then the earliest creature that drops it, then the latest of what it is made from.
        private static Stage StageOf(ItemDrop item, HashSet<string> visiting)
        {
            string name = item.m_itemData.m_shared.m_name;
            if (s_stages.TryGetValue(name, out Stage cached))
                return cached;
            if (StageTable.Raw.TryGetValue(name, out Stage raw))
                return s_stages[name] = raw;
            if (!visiting.Add(name))
                return Stage.Meadows;

            Stage? stage = null;
            if (s_droppedBy.TryGetValue(name, out List<string> creatures))
            {
                foreach (string creature in creatures)
                {
                    if (s_creatureStage.TryGetValue(creature, out Stage creatureStage) && (stage == null || creatureStage < stage))
                        stage = creatureStage;
                }
            }
            if (stage == null && s_recipes.TryGetValue(name, out Recipe recipe))
            {
                Stage latest = Stage.Meadows;
                foreach (Piece.Requirement requirement in recipe.m_resources)
                {
                    if (requirement?.m_resItem != null)
                    {
                        Stage input = StageOf(requirement.m_resItem, visiting);
                        if (input > latest)
                            latest = input;
                    }
                }
                stage = latest;
            }
            if (stage == null && s_converted.TryGetValue(name, out KeyValuePair<string, ItemDrop> conversion))
                stage = StageOf(conversion.Value, visiting);
            if (stage == null)
            {
                MaterialTrackerPlugin.Log.LogDebug($"No stage known for {name}; using Plains.");
                stage = Stage.Plains;
            }
            return s_stages[name] = stage.Value;
        }

        private static string HintFor(string name)
        {
            Localization loc = Localization.instance;
            if (s_droppedBy.TryGetValue(name, out List<string> creatures))
            {
                string by = string.Join(", ", creatures.Take(2).Select(c => loc.Localize(c)));
                return "Dropped by " + by + (creatures.Count > 2 ? " and others" : "");
            }
            if (s_converted.TryGetValue(name, out KeyValuePair<string, ItemDrop> conversion))
                return $"Made in a {loc.Localize(conversion.Key).ToLowerInvariant()} from {loc.Localize(conversion.Value.m_itemData.m_shared.m_name)}";
            if (s_recipes.TryGetValue(name, out Recipe recipe))
                return recipe.m_craftingStation != null
                    ? "Crafted at the " + loc.Localize(recipe.m_craftingStation.m_name).ToLowerInvariant()
                    : "Crafted by hand";
            if (s_found.TryGetValue(name, out string how))
                return how;
            return "Found somewhere in the world";
        }
    }
}
