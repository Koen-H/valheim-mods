using System.Collections.Generic;

namespace CraftTracker
{
    /// <summary>Reads the per-item "crafted" stats that the game saves in the character profile.</summary>
    internal static class CraftedItems
    {
        public static bool IsCrafted(Recipe recipe)
        {
            PlayerProfile profile = Game.instance != null ? Game.instance.GetPlayerProfile() : null;
            if (profile == null) return false;
            // Index 0 holds the totals for every craft, regardless of difficulty or cheats.
            return profile.m_playerStats[0].m_itemCraftStats.TryGetValue(NameOf(recipe), out float count) && count > 0f;
        }

        public static string NameOf(Recipe recipe) => recipe.m_item.m_itemData.m_shared.m_name;

        /// <summary>
        /// Number of items craftable at the player's current station (or by hand) that they have not
        /// discovered yet. Mirrors Player.GetAvailableRecipes with the "known" check inverted, and counts
        /// by item name because that is how the game remembers known recipes.
        /// </summary>
        public static int CountUndiscovered(Player player)
        {
            if (player == null || ObjectDB.instance == null || player.NoCostCheat() ||
                (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(GlobalKeys.AllRecipesUnlocked)))
                return 0;

            var names = new HashSet<string>();
            foreach (Recipe recipe in ObjectDB.instance.m_recipes)
            {
                bool seasonal = player.CurrentSeason != null && player.CurrentSeason.Recipes.Contains(recipe);
                if ((!recipe.m_enabled && !seasonal) || !recipe.m_item || recipe.m_noCraftOnlyUpgrade)
                    continue;

                string name = NameOf(recipe);
                string dlc = recipe.m_item.m_itemData.m_shared.m_dlc;
                if (!string.IsNullOrEmpty(dlc) && !DLCMan.instance.IsDLCInstalled(dlc))
                    continue;
                if (player.IsRecipeKnown(name) || !player.RequiredCraftingStation(recipe, 1, checkLevel: false))
                    continue;
                names.Add(name);
            }
            return names.Count;
        }
    }
}
