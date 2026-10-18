// Shared by several mods (linked into each csproj): what an achievement asks for, and how far this character is.
//
// The lists come from the game's own achievements, so they always match: "every food" for The Gourmet is
// exactly what the game counts. Progress is read from the profile stats for the achievement's difficulty
// (index 1, "Any", for most), which only count while achievements can be earned.

using System.Collections.Generic;

namespace Koendertjh.Shared
{
    internal static class AchievementStats
    {
        // Trigger list -> (its count when read, its keys).
        private static readonly Dictionary<object, KeyValuePair<int, HashSet<string>>> s_keys =
            new Dictionary<object, KeyValuePair<int, HashSet<string>>>();

        internal static Achievement Find(string id)
        {
            if (Achievements.m_instance == null)
                return null;
            foreach (AchievementList list in Achievements.m_instance.m_achievementLists)
            {
                foreach (Achievement achievement in list.m_achievements)
                {
                    if (achievement != null && achievement.m_id == id)
                        return achievement;
                }
            }
            return null;
        }

        /// <summary>The profile stats this achievement counts, or null outside a game.</summary>
        internal static PlayerProfile.PlayerStats StatsFor(Achievement achievement)
        {
            PlayerProfile profile = Game.instance?.GetPlayerProfile();
            if (profile == null || achievement == null)
                return null;
            return profile.m_playerStats[(int)achievement.m_difficultyRequirement];
        }

        /// <summary>The stat names in a trigger list, e.g. every food of The Gourmet.</summary>
        internal static HashSet<string> Keys(List<Achievement.DictStatRequirement> triggers)
        {
            if (triggers == null)
                return new HashSet<string>();
            // The game fills the dynamic lists once it starts; rebuild if the list changed since.
            if (s_keys.TryGetValue(triggers, out KeyValuePair<int, HashSet<string>> cached) && cached.Key == triggers.Count)
                return cached.Value;
            var keys = new HashSet<string>();
            foreach (Achievement.DictStatRequirement trigger in triggers)
                keys.Add(trigger.m_stat);
            s_keys[triggers] = new KeyValuePair<int, HashSet<string>>(triggers.Count, keys);
            return keys;
        }

        internal static HashSet<string> Keys(List<Achievement.EnemyStatRequirement> triggers)
        {
            if (triggers == null)
                return new HashSet<string>();
            if (s_keys.TryGetValue(triggers, out KeyValuePair<int, HashSet<string>> cached) && cached.Key == triggers.Count)
                return cached.Value;
            var keys = new HashSet<string>();
            foreach (Achievement.EnemyStatRequirement trigger in triggers)
                keys.Add(trigger.m_stat);
            s_keys[triggers] = new KeyValuePair<int, HashSet<string>>(triggers.Count, keys);
            return keys;
        }

        internal static bool Has(Dictionary<string, float> stats, string key) =>
            stats != null && stats.TryGetValue(key, out float value) && value >= 1f;

        /// <summary>How many keys of a set have been done in these stats.</summary>
        internal static int CountDone(HashSet<string> keys, Dictionary<string, float> stats)
        {
            int done = 0;
            foreach (string key in keys)
            {
                if (Has(stats, key))
                    done++;
            }
            return done;
        }
    }
}
