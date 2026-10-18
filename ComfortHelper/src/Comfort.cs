// Works out the comfort here and what would raise it, the same way SE_Rested.CalculateComfortLevel counts:
// per comfort group (or per piece, for pieces without a group) only the best piece within 10 m counts.

using System.Collections.Generic;
using System.Linq;
using Koendertjh.Shared;
using UnityEngine;

namespace ComfortHelper
{
    internal class Suggestion
    {
        public Piece Piece;
        public int Gain;
        public Piece Replaces;
    }

    internal class ComfortReport
    {
        public bool InShelter;
        public int Current;
        public int BestKnown;
        public int Locked;
        public float MaxReached;
        public readonly List<Suggestion> Suggestions = new List<Suggestion>();
    }

    internal static class Comfort
    {
        private const float Radius = 10f;
        private static readonly List<Piece> s_nearby = new List<Piece>();
        private static List<Piece> s_comfortPrefabs;
        private static ZNetScene s_scene;

        /// <summary>Pieces of one group don't stack; pieces without a group count once per kind.</summary>
        internal static string GroupOf(Piece piece) =>
            piece.m_comfortGroup != Piece.ComfortGroup.None ? piece.m_comfortGroup.ToString() : piece.m_name;

        /// <summary>The piece that counts for each group within 10 m, as the game counts it.</summary>
        internal static Dictionary<string, Piece> BestNearby(Vector3 position)
        {
            var best = new Dictionary<string, Piece>();
            s_nearby.Clear();
            Piece.GetAllComfortPiecesInRadius(position, Radius, s_nearby);
            foreach (Piece piece in s_nearby)
            {
                string group = GroupOf(piece);
                if (!best.TryGetValue(group, out Piece current) || piece.GetComfort() > current.GetComfort())
                    best[group] = piece;
            }
            return best;
        }

        /// <summary>Names of the comfort pieces within 10 m, counting or not.</summary>
        internal static HashSet<string> NamesNearby(Vector3 position)
        {
            var names = new HashSet<string>();
            s_nearby.Clear();
            Piece.GetAllComfortPiecesInRadius(position, Radius, s_nearby);
            foreach (Piece piece in s_nearby)
                names.Add(piece.m_name);
            return names;
        }

        internal static ComfortReport Build(Player player)
        {
            var report = new ComfortReport
            {
                InShelter = player.InShelter(),
                Current = SE_Rested.CalculateComfortLevel(player),
                MaxReached = MaxComfort()
            };

            Dictionary<string, Piece> best = BestNearby(player.transform.position);

            // Best known and best overall piece of each group.
            var bestKnown = new Dictionary<string, Piece>();
            var bestAny = new Dictionary<string, int>();
            foreach (Piece prefab in ComfortPrefabs())
            {
                string group = GroupOf(prefab);
                if (!bestAny.TryGetValue(group, out int any) || prefab.m_comfort > any)
                    bestAny[group] = prefab.m_comfort;
                if (player.IsRecipeKnown(prefab.m_name) &&
                    (!bestKnown.TryGetValue(group, out Piece knownPiece) || prefab.m_comfort > knownPiece.m_comfort))
                    bestKnown[group] = prefab;
            }

            int known = 0;
            foreach (KeyValuePair<string, Piece> pair in bestKnown)
            {
                known += pair.Value.m_comfort;
                best.TryGetValue(pair.Key, out Piece here);
                int gain = pair.Value.m_comfort - (here != null ? here.GetComfort() : 0);
                if (gain > 0)
                    report.Suggestions.Add(new Suggestion { Piece = pair.Value, Gain = gain, Replaces = here });
            }
            report.BestKnown = 2 + known;
            report.Locked = bestAny.Sum(p => p.Value) - known;
            report.Suggestions.Sort((a, b) => b.Gain.CompareTo(a.Gain));
            return report;
        }

        private static float MaxComfort()
        {
            Achievement achievement = AchievementStats.Find("GrindComfort");
            PlayerProfile.PlayerStats stats = AchievementStats.StatsFor(achievement);
            return stats != null && stats.m_stats.TryGetValue(PlayerStatType.MaxComfort, out float value) ? value : 0f;
        }

        private static List<Piece> ComfortPrefabs()
        {
            if (s_comfortPrefabs != null && s_scene == ZNetScene.instance)
                return s_comfortPrefabs;

            s_scene = ZNetScene.instance;
            s_comfortPrefabs = new List<Piece>();
            if (s_scene == null || ObjectDB.instance == null)
                return s_comfortPrefabs;

            // Only pieces a tool can build (hammer, cultivator...), not dungeon furniture.
            var names = new HashSet<string>();
            foreach (GameObject item in ObjectDB.instance.m_items)
            {
                PieceTable table = item != null ? item.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_buildPieces : null;
                if (table == null)
                    continue;
                foreach (GameObject prefab in table.m_pieces)
                {
                    Piece piece = prefab != null ? prefab.GetComponent<Piece>() : null;
                    if (piece != null && piece.m_comfort > 0 && names.Add(piece.m_name))
                        s_comfortPrefabs.Add(piece);
                }
            }
            return s_comfortPrefabs;
        }

        internal static string Describe(Suggestion suggestion)
        {
            string name = Localization.instance.Localize(suggestion.Piece.m_name);
            return suggestion.Replaces == null
                ? $"+{suggestion.Gain} {name}"
                : $"+{suggestion.Gain} {name} (instead of {Localization.instance.Localize(suggestion.Replaces.m_name)})";
        }
    }
}
