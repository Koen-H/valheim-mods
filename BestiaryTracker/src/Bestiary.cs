// Which creatures count for the hunter achievements, and which of them this character has slain.
// Kills are counted by the creature's name ("$enemy_greydwarf") in the profile stats of the achievement's
// difficulty; the game also counts a kill for every easier difficulty.

using System.Collections.Generic;
using Koendertjh.Shared;

namespace BestiaryTracker
{
    internal static class Bestiary
    {
        private static Achievement s_hunter;
        private static Achievement s_masterHunter;

        private static Achievement Hunter => s_hunter != null ? s_hunter : s_hunter = AchievementStats.Find("KillAllCreatures");
        private static Achievement MasterHunter => s_masterHunter != null ? s_masterHunter : s_masterHunter = AchievementStats.Find("KillAllCreaturesHard");

        /// <summary>The Master Hunter on hard difficulty or harder (until it is done), otherwise The Hunter.</summary>
        internal static Achievement Current
        {
            get
            {
                Achievement master = MasterHunter;
                if (master != null && !master.m_unlocked &&
                    Achievements.GetCurrentAchievementDifficulty() >= master.m_difficultyRequirement)
                    return master;
                return Hunter;
            }
        }

        internal static HashSet<string> Creatures(Achievement achievement) =>
            AchievementStats.Keys(achievement?.m_enemyStatsTriggers);

        private static Dictionary<string, float> Kills(Achievement achievement) =>
            AchievementStats.StatsFor(achievement)?.m_enemyStats[0];

        internal static bool Counts(Achievement achievement, string creature) => Creatures(achievement).Contains(creature);

        internal static bool IsSlain(Achievement achievement, string creature) =>
            AchievementStats.Has(Kills(achievement), creature);

        internal static int SlainCount(Achievement achievement) =>
            AchievementStats.CountDone(Creatures(achievement), Kills(achievement));
    }
}
