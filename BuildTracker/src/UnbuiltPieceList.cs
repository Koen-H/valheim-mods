using System;
using System.Collections.Generic;
using System.Linq;

namespace BuildTracker
{
    /// <summary>
    /// Build menu list with the pieces this character has never placed, grouped by the same usage
    /// categories as the "By usage" tab.
    /// </summary>
    internal sealed class UnbuiltPieceList : IPieceList
    {
        private readonly Piece.UsageTagFlags[] _usageTags =
            (Piece.UsageTagFlags[])Enum.GetValues(typeof(Piece.UsageTagFlags));
        private readonly string[] _usageTagDisplayNames;
        private readonly List<int> _availableTags = new List<int>();

        public UnbuiltPieceList(string displayName)
        {
            DisplayName = displayName;
            _usageTagDisplayNames = _usageTags.Select(GetDisplayName).ToArray();
        }

        public string DisplayName { get; }

        public bool ShowTags => BuildTrackerPlugin.ShowCategories.Value;

        public bool CanCustomizeTags => false;

        public int TagCount => _availableTags.Count;

        public int TagSeparatorIndex => -1;

        public string GetTagDisplayName(int index) => _usageTagDisplayNames[_availableTags[index]];

        public int GetTagIdByIndex(int index) => _availableTags[index];

        public void UpdateAvailableTags(PieceTable pieceTable)
        {
            Piece.UsageTagFlags used = 0;
            foreach (Piece piece in pieceTable.m_availablePieces)
                if (!IsSpecial(piece) && !BuiltPieces.IsBuilt(piece))
                    used |= piece.m_usage;

            _availableTags.Clear();
            for (int i = 0; i < _usageTags.Length; i++)
                if (used.HasFlag(_usageTags[i]))
                    _availableTags.Add(i);
        }

        public void GetAvailablePiecesWithTag(int tagId, PieceTable pieceTable, IList<Piece> resultOut)
        {
            foreach (Piece piece in pieceTable.m_availablePieces)
            {
                // Repair/remove are only shown by the build menu when the category list is visible.
                if (IsSpecial(piece))
                    resultOut.Add(piece);
                else if (!BuiltPieces.IsBuilt(piece) && (tagId == -1 || piece.m_usage.HasFlag(_usageTags[tagId])))
                    resultOut.Add(piece);
            }
        }

        /// <summary>
        /// True when every discovered piece of this tool has been placed. Checks all known pieces, not just
        /// the listed ones, because the game can hide pieces you can't afford right now.
        /// </summary>
        public bool AllDiscoveredBuilt(PieceTable pieceTable)
        {
            Player player = Player.m_localPlayer;
            if (player == null) return false;

            foreach (Piece piece in pieceTable.m_enabledPieces)
            {
                bool discovered = pieceTable.m_availablePieces.Contains(piece) || player.IsRecipeKnown(piece.m_name);
                if (!IsSpecial(piece) && discovered && !BuiltPieces.IsBuilt(piece))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Number of pieces of this tool (in the given category, -1 = all) the player has not discovered yet.
        /// Counted by name, because that is how the game remembers which pieces are known.
        /// </summary>
        public int CountUndiscovered(PieceTable pieceTable, int tagId)
        {
            Player player = Player.m_localPlayer;
            if (player == null) return 0;

            var names = new HashSet<string>();
            foreach (Piece piece in pieceTable.m_enabledPieces)
            {
                if (IsSpecial(piece) || pieceTable.m_availablePieces.Contains(piece) || player.IsRecipeKnown(piece.m_name))
                    continue;
                if (tagId == -1 || piece.m_usage.HasFlag(_usageTags[tagId]))
                    names.Add(piece.m_name);
            }
            return names.Count;
        }

        private static bool IsSpecial(Piece piece) => piece.m_repairPiece || piece.m_removePiece;

        // The game's DisplayNameAttribute lives in another assembly; read it by name instead of referencing it.
        private static string GetDisplayName(Piece.UsageTagFlags flag)
        {
            object attribute = typeof(Piece.UsageTagFlags).GetField(flag.ToString())?
                .GetCustomAttributes(false)
                .FirstOrDefault(a => a.GetType().Name == "DisplayNameAttribute");
            return attribute?.GetType().GetProperty("DisplayName")?.GetValue(attribute) as string ?? flag.ToString();
        }
    }

    /// <summary>Reads the per-piece "placed" stats that the game saves in the character profile.</summary>
    internal static class BuiltPieces
    {
        public static bool IsBuilt(Piece piece)
        {
            PlayerProfile profile = Game.instance != null ? Game.instance.GetPlayerProfile() : null;
            if (profile == null) return false;
            // Index 0 holds the totals for every placement, regardless of difficulty or cheats.
            return profile.m_playerStats[0].m_piecesPlacedStats.TryGetValue(piece.m_name, out float count) && count > 0f;
        }
    }
}
