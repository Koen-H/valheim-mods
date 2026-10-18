// Secret achievements: vanilla shows them as "Concealed" and can't open them until they are unlocked
// (InventoryGui.UpdateAchievementsList and AchievementsGui.OnOpenAchievementDetails check m_isSecret).
// While those two run, the revealed secrets are marked as not secret, so vanilla shows them like any
// locked achievement. Their names are tinted so you can still tell they are secrets.

using System.Collections.Generic;
using HarmonyLib;
using Koendertjh.Shared;
using TMPro;
using UnityEngine;

namespace AchievementPeek
{
    [HarmonyPatch]
    internal static class SecretAchievements
    {
        // Progressive: secrets that wait for a boss. The others show right away.
        private static readonly Dictionary<string, Boss> RevealAfter = new Dictionary<string, Boss>
        {
            { "Boss2Elder", Boss.Eikthyr },
            { "Boss3Bonemass", Boss.Elder },
            { "Boss4Moder", Boss.Bonemass },
            { "Boss5Yagluth", Boss.Moder },
            { "Boss6Queen", Boss.Yagluth },
            { "Boss7Fader", Boss.Queen },
            { "Boss8FrozenKing", Boss.Fader },
            { "DeathByObliterator", Boss.Queen },
            { "BuildDyrnwyn", Boss.Queen },
        };

        private static readonly Color SecretTint = new Color(0.78f, 0.7f, 0.95f);

        private static bool IsRevealed(Achievement achievement)
        {
            switch (AchievementPeekPlugin.SecretAchievements.Value)
            {
                case SecretMode.All:
                    return true;
                case SecretMode.Progressive:
                    return !RevealAfter.TryGetValue(achievement.m_id, out Boss boss) || BossProgress.IsDefeated(boss);
                default:
                    return false;
            }
        }

        private static bool ShouldUnhide(Achievement achievement) =>
            achievement != null && achievement.m_isSecret && !achievement.m_unlocked && IsRevealed(achievement);

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InventoryGui), "UpdateAchievementsList")]
        private static void BeforeList(out List<Achievement> __state)
        {
            __state = new List<Achievement>();
            if (!AchievementPeekPlugin.Active || Achievements.m_instance == null)
                return;

            foreach (AchievementList list in Achievements.m_instance.m_achievementLists)
            {
                foreach (Achievement achievement in list.m_achievements)
                {
                    if (ShouldUnhide(achievement))
                    {
                        achievement.m_isSecret = false;
                        __state.Add(achievement);
                    }
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(InventoryGui), "UpdateAchievementsList")]
        private static void TintSecrets(List<Achievement> __state, AchievementsGui ___m_achievementsPanel)
        {
            if (__state.Count == 0)
                return;

            var names = new HashSet<string>();
            foreach (Achievement achievement in __state)
                names.Add(Localization.instance.Localize(achievement.m_name));

            foreach (GameObject element in ___m_achievementsPanel.m_achievementsList)
            {
                TMP_Text name = element.transform.Find("name")?.GetComponent<TMP_Text>();
                if (name != null && names.Contains(name.text))
                    name.color = SecretTint;
            }
        }

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(InventoryGui), "UpdateAchievementsList")]
        private static void AfterList(List<Achievement> __state) => Rehide(__state);

        [HarmonyPrefix]
        [HarmonyPatch(typeof(AchievementsGui), nameof(AchievementsGui.OnOpenAchievementDetails))]
        private static void BeforeDetails(Achievement achievement, out List<Achievement> __state)
        {
            __state = new List<Achievement>();
            if (AchievementPeekPlugin.Active && ShouldUnhide(achievement))
            {
                achievement.m_isSecret = false;
                __state.Add(achievement);
            }
        }

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(AchievementsGui), nameof(AchievementsGui.OnOpenAchievementDetails))]
        private static void AfterDetails(List<Achievement> __state) => Rehide(__state);

        private static void Rehide(List<Achievement> unhidden)
        {
            if (unhidden == null)
                return;
            foreach (Achievement achievement in unhidden)
                achievement.m_isSecret = true;
        }
    }
}
