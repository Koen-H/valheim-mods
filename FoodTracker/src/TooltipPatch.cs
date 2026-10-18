// Adds "You haven't eaten this yet" and "You haven't made this yet" to item tooltips. The game builds every
// item tooltip (inventory, chests, crafting, traders) in ItemDrop.ItemData.GetTooltip, at least once per frame
// while the inventory is open, so the added lines are cached per item and worked out again once a second.

using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using Koendertjh.Shared;
using UnityEngine;

namespace FoodTracker
{
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip),
        typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool))]
    internal static class TooltipPatch
    {
        private const string HintColor = "#ffb347";
        private const string ProgressColor = "#a0a0a0";

        // Item name -> the lines added to its tooltip ("" when nothing).
        private static readonly Dictionary<string, string> s_suffixes = new Dictionary<string, string>();
        private static float s_cacheUntil;

        private static void Postfix(ItemDrop.ItemData item, ref string __result)
        {
            if (!FoodTrackerPlugin.TooltipHints.Value || item?.m_shared == null || Game.instance == null)
                return;

            string name = item.m_shared.m_name;
            if (!FoodStatus.EdibleItems.Contains(name) && !FoodStatus.CookableItems.Contains(name))
                return;

            if (Time.time >= s_cacheUntil)
            {
                s_suffixes.Clear();
                s_cacheUntil = Time.time + 1f;
            }
            if (!s_suffixes.TryGetValue(name, out string suffix))
                s_suffixes[name] = suffix = Suffix(name);
            if (suffix.Length > 0)
                __result += suffix;
        }

        private static string Suffix(string name)
        {
            var text = new StringBuilder();
            if (FoodStatus.NotEatenYet(name))
                text.Append($"\n\n<color={HintColor}>You haven't eaten this yet. I wonder what it tastes like...</color>");
            if (FoodStatus.NotCookedYet(name))
                text.Append($"\n<color={HintColor}>You haven't made this yet.</color>");

            if (FoodTrackerPlugin.ShowProgress.Value)
            {
                if (FoodStatus.EdibleItems.Contains(name))
                    text.Append(Progress(FoodStatus.Gourmet, AchievementStats.CountDone(FoodStatus.EdibleItems, FoodStatus.EatenStats),
                        FoodStatus.EdibleItems.Count));
                if (FoodStatus.CookableItems.Contains(name))
                    text.Append(Progress(FoodStatus.MasterChef, AchievementStats.CountDone(FoodStatus.CookableItems, FoodStatus.CookedStats),
                        FoodStatus.CookableItems.Count));
            }
            return text.ToString();
        }

        private static string Progress(Achievement achievement, int done, int total) =>
            $"\n<color={ProgressColor}>{Localization.instance.Localize(achievement.m_name)}: {done} / {total}</color>";
    }
}
