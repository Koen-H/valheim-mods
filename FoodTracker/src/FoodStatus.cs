// Food progress from the game's own achievements: The Gourmet (eat every edible item) and The Master Chef
// (cook every dish). Items are matched by their shared name, the key the game counts them by.

using System.Collections.Generic;
using Koendertjh.Shared;

namespace FoodTracker
{
    internal static class FoodStatus
    {
        internal const string GourmetId = "AllFoodEaten";
        internal const string MasterChefId = "AllFoodCooked";

        private static Achievement s_gourmet;
        private static Achievement s_masterChef;

        internal static Achievement Gourmet => s_gourmet != null ? s_gourmet : s_gourmet = AchievementStats.Find(GourmetId);
        internal static Achievement MasterChef => s_masterChef != null ? s_masterChef : s_masterChef = AchievementStats.Find(MasterChefId);

        internal static HashSet<string> EdibleItems => AchievementStats.Keys(Gourmet?.m_foodEatenTriggers);
        internal static HashSet<string> CookableItems => AchievementStats.Keys(MasterChef?.m_itemCraftTriggers);

        internal static Dictionary<string, float> EatenStats => AchievementStats.StatsFor(Gourmet)?.m_foodEatenStats;
        internal static Dictionary<string, float> CookedStats => AchievementStats.StatsFor(MasterChef)?.m_itemCraftStats;

        /// <summary>Counts for The Gourmet and the player hasn't eaten it yet.</summary>
        internal static bool NotEatenYet(string itemName) =>
            EdibleItems.Contains(itemName) && !AchievementStats.Has(EatenStats, itemName);

        /// <summary>Counts for The Master Chef and the player hasn't made it yet.</summary>
        internal static bool NotCookedYet(string itemName) =>
            CookableItems.Contains(itemName) && !AchievementStats.Has(CookedStats, itemName);
    }
}
