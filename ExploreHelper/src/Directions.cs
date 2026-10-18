// The four far directions as the game counts them (Player.UpdateStats): time spent beyond 10350 m from the
// centre, stored in the profile stats of The Explorer's difficulty. One second there is enough.

using Koendertjh.Shared;
using UnityEngine;

namespace ExploreHelper
{
    internal class Direction
    {
        public string Name;
        public PlayerStatType Stat;
        public PlayerStatType NoMapStat;
        public Vector3 PinPosition;
    }

    internal static class Directions
    {
        private const float Edge = 10350f;
        // Between the line and 10420 m, where the edge of the world starts pushing you (and boats) back.
        private const float PinDistance = 10385f;

        internal static readonly Direction[] All =
        {
            new Direction { Name = "Far north", Stat = PlayerStatType.ExploreNorth, NoMapStat = PlayerStatType.ExploreNorthNoMap, PinPosition = new Vector3(0f, 0f, PinDistance) },
            new Direction { Name = "Far south", Stat = PlayerStatType.ExploreSouth, NoMapStat = PlayerStatType.ExploreSouthNoMap, PinPosition = new Vector3(0f, 0f, -PinDistance) },
            // Yes, east is negative x: that is how the game counts it.
            new Direction { Name = "Far east", Stat = PlayerStatType.ExploreEast, NoMapStat = PlayerStatType.ExploreEastNoMap, PinPosition = new Vector3(-PinDistance, 0f, 0f) },
            new Direction { Name = "Far west", Stat = PlayerStatType.ExploreWest, NoMapStat = PlayerStatType.ExploreWestNoMap, PinPosition = new Vector3(PinDistance, 0f, 0f) },
        };

        private static Achievement s_explorer;
        private static Achievement s_pathfinder;

        internal static Achievement Explorer => s_explorer != null ? s_explorer : s_explorer = AchievementStats.Find("ExploreNSEW");
        internal static Achievement Pathfinder => s_pathfinder != null ? s_pathfinder : s_pathfinder = AchievementStats.Find("ExploreNSEWNoMap");

        internal static float Value(Achievement achievement, PlayerStatType stat)
        {
            PlayerProfile.PlayerStats stats = AchievementStats.StatsFor(achievement);
            return stats != null && stats.m_stats.TryGetValue(stat, out float value) ? value : 0f;
        }

        internal static bool Reached(Direction direction) => Value(Explorer, direction.Stat) >= 1f;

        internal static int ReachedCount(Achievement achievement, bool noMap)
        {
            int count = 0;
            foreach (Direction direction in All)
            {
                if (Value(achievement, noMap ? direction.NoMapStat : direction.Stat) >= 1f)
                    count++;
            }
            return count;
        }

        internal static Direction ByStat(PlayerStatType stat, out bool noMap)
        {
            foreach (Direction direction in All)
            {
                if (direction.Stat == stat || direction.NoMapStat == stat)
                {
                    noMap = direction.NoMapStat == stat;
                    return direction;
                }
            }
            noMap = false;
            return null;
        }
    }
}
