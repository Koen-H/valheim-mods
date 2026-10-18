// Adds "You haven't eaten this yet" and "You haven't made this yet" to item tooltips. The game builds every
// item tooltip (inventory, chests, crafting, traders) in ItemDrop.ItemData.GetTooltip.

using System.Text;
using HarmonyLib;
using Koendertjh.Shared;

namespace FoodTracker
{
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip),
        typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool))]
    internal static class TooltipPatch
    {
        private const string HintColor = "#ffb347";
        private const string ProgressColor = "#a0a0a0";

        private static void Postfix(ItemDrop.ItemData item, ref string __result)
        {
            if (!FoodTrackerPlugin.TooltipHints.Value || item?.m_shared == null || Game.instance == null)
                return;

            string name = item.m_shared.m_name;
            bool edible = FoodStatus.EdibleItems.Contains(name);
            bool cookable = FoodStatus.CookableItems.Contains(name);
            if (!edible && !cookable)
                return;

            var text = new StringBuilder(__result);
            if (FoodStatus.NotEatenYet(name))
                text.Append($"\n\n<color={HintColor}>You haven't eaten this yet. I wonder what it tastes like...</color>");
            if (FoodStatus.NotCookedYet(name))
                text.Append($"\n<color={HintColor}>You haven't made this yet.</color>");

            if (FoodTrackerPlugin.ShowProgress.Value)
            {
                if (edible)
                    text.Append(Progress(FoodStatus.Gourmet, AchievementStats.CountDone(FoodStatus.EdibleItems, FoodStatus.EatenStats),
                        FoodStatus.EdibleItems.Count));
                if (cookable)
                    text.Append(Progress(FoodStatus.MasterChef, AchievementStats.CountDone(FoodStatus.CookableItems, FoodStatus.CookedStats),
                        FoodStatus.CookableItems.Count));
            }
            __result = text.ToString();
        }

        private static string Progress(Achievement achievement, int done, int total) =>
            $"\n<color={ProgressColor}>{Localization.instance.Localize(achievement.m_name)}: {done} / {total}</color>";
    }
}
