// The details panel of an achievement: one row per condition, created by AchievementsGui.CreateStatRow from
// several coroutines over a few frames. Vanilla greys out unmet rows as "??? / ???"; this shows their name and
// progress, or counts them in an "Only Odin knows" row that is kept at the bottom.
// Achievements made of other achievements have no rows in vanilla; they get one per part.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace AchievementPeek
{
    [HarmonyPatch(typeof(AchievementsGui))]
    internal static class DetailRows
    {
        // Stats without an English name in the game's localization.
        private static readonly Dictionary<string, string> StatNames = new Dictionary<string, string>
        {
            { "BossKillMultiplayer", "Bosses killed together with a friend" },
            { "BossKillSolo", "Bosses killed alone" },
            { "BuildClusterArchitecture", "Architecture pieces in one building" },
            { "BuildClusterBuilding", "Buildings in one village" },
            { "BuildClusterDecor", "Decorations in one building" },
            { "BuildClusterFloor", "Floors in one building" },
            { "BuildClusterFurniture", "Furniture in one building" },
            { "BuildClusterLighting", "Lights in one building" },
            { "BuildClusterRoof", "Roof pieces in one building" },
            { "BuildClusterStorage", "Storage in one building" },
            { "BuildClusterWall", "Walls in one building" },
            { "DeathByAshlandsLava", "Death by lava" },
            { "Deaths", "Deaths" },
            { "FishCaughtTier4", "Four star fish caught" },
            { "LeviathanSink", "Leviathans awakened" },
            { "PlayerSpawn", "Times spawned" },
        };

        private static Transform s_odinRow;
        private static int s_hidden;
        private static int s_missing;

        [HarmonyPrefix]
        [HarmonyPatch(nameof(AchievementsGui.OnOpenAchievementDetails))]
        private static void BeforeOpen(AchievementsGui __instance, out bool __state)
        {
            // The panel was already open: vanilla does nothing.
            __state = __instance.m_achievementDetails.activeSelf;
            if (!__state)
            {
                s_odinRow = null;
                s_hidden = 0;
                s_missing = 0;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(AchievementsGui.OnOpenAchievementDetails))]
        private static void AfterOpen(Achievement achievement, bool clickable, bool __state,
            AchievementDetailUnlockCondition ___m_achievementDetailsElementPrefab,
            RectTransform ___m_achievementDetailsListRoot)
        {
            if (__state || !clickable || !AchievementPeekPlugin.Active)
                return;

            foreach (Achievement part in achievement.m_otherAchievementTriggers)
            {
                if (part == null)
                    continue;
                AddRow(___m_achievementDetailsElementPrefab, ___m_achievementDetailsListRoot,
                    Localization.instance.Localize(part.m_name), part.m_unlocked ? 1f : 0f, 1f);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("CreateStatRow")]
        private static bool CreateStatRow(string statKey, float currentAmount, float totalAmount,
            AchievementDetailUnlockCondition ___m_achievementDetailsElementPrefab,
            RectTransform ___m_achievementDetailsListRoot)
        {
            RowMode mode = AchievementPeekPlugin.MissingRows.Value;
            if (currentAmount >= totalAmount || mode == RowMode.Off || !AchievementPeekPlugin.Active)
                return true;

            if (mode == RowMode.Discovered && !Discovery.IsDiscovered(statKey))
            {
                s_hidden++;
                UpdateOdinRow(___m_achievementDetailsElementPrefab, ___m_achievementDetailsListRoot);
                return false;
            }

            AddRow(___m_achievementDetailsElementPrefab, ___m_achievementDetailsListRoot,
                RowName(statKey), currentAmount, totalAmount);
            return false;
        }

        // Also after vanilla rows (met conditions), so the Odin row stays last.
        [HarmonyPostfix]
        [HarmonyPatch("CreateStatRow")]
        private static void KeepOdinRowLast()
        {
            if (s_odinRow != null)
                s_odinRow.SetAsLastSibling();
        }

        private static void AddRow(AchievementDetailUnlockCondition prefab, RectTransform root, string name,
            float current, float total)
        {
            bool met = current >= total;
            AchievementDetailUnlockCondition row = NewRow(prefab, root, met ? Color.green : Color.gray);
            row.StatName.text = name;
            row.Progress.text = $"{Format(current)} / {Format(total)}";
            if (!met && AchievementPeekPlugin.MissingFirst.Value)
                row.transform.SetSiblingIndex(s_missing++);
            KeepOdinRowLast();
        }

        private static void UpdateOdinRow(AchievementDetailUnlockCondition prefab, RectTransform root)
        {
            if (s_odinRow == null)
            {
                AchievementDetailUnlockCondition row = NewRow(prefab, root, Color.gray);
                row.StatName.fontStyle |= FontStyles.Italic;
                row.Progress.text = "";
                s_odinRow = row.transform;
            }

            s_odinRow.GetComponent<AchievementDetailUnlockCondition>().StatName.text =
                $"Only Odin knows what else is hidden here ({s_hidden})";
            s_odinRow.SetAsLastSibling();
        }

        private static AchievementDetailUnlockCondition NewRow(AchievementDetailUnlockCondition prefab,
            RectTransform root, Color color)
        {
            AchievementDetailUnlockCondition row = UnityEngine.Object.Instantiate(prefab, root);
            row.gameObject.SetActive(true);
            row.StatName.color = color;
            row.Progress.color = color;
            return row;
        }

        private static string RowName(string statKey)
        {
            if (!Discovery.IsStat(statKey))
                return Localized(statKey) ?? statKey;

            if (StatNames.TryGetValue(statKey, out string name))
                return name;
            if (Localized("$stat_" + statKey) is string localized)
                return localized;
            // "DistanceSailHelm" -> "Distance sail helm" when the game has no name for it.
            string spaced = Regex.Replace(statKey, "(?<=[a-z])(?=[A-Z])", " ");
            return spaced.Substring(0, 1) + spaced.Substring(1).ToLowerInvariant();
        }

        // Missing keys come back unchanged or in [brackets].
        private static string Localized(string key)
        {
            string text = Localization.instance.Localize(key);
            if (string.IsNullOrEmpty(text) || text == key || text.StartsWith("[", StringComparison.Ordinal))
                return null;
            return text;
        }

        private static string Format(float amount) =>
            Mathf.Floor(amount).ToString("N0", CultureInfo.InvariantCulture);
    }
}
