using System;
using System.Collections.Generic;

namespace UnlockAwareness
{
    public enum UnlockKind
    {
        Recipe,
        Piece,
        Item
    }

    /// <summary>
    /// Tracks unlocks the player has not looked at yet. Stored per character in Player.m_customData,
    /// so it is saved together with the character file.
    /// </summary>
    internal static class UnseenStore
    {
        private const string CustomDataKey = "UnlockAwareness_Unseen";
        private const string FlaggedAllKey = "UnlockAwareness_FlaggedAllKnown";
        private const char Separator = ';';

        private static readonly HashSet<string> s_unseen = new HashSet<string>();
        private static Player s_owner;

        public static event Action Changed;

        public static string MakeKey(UnlockKind kind, string name) =>
            (kind == UnlockKind.Recipe ? "r:" : kind == UnlockKind.Piece ? "p:" : "i:") + name;

        /// <summary>True if anything at all is unseen; a cheap check before building keys every frame.</summary>
        public static bool HasAnyUnseen => Sync() && s_unseen.Count > 0;

        public static bool IsUnseen(UnlockKind kind, string name) =>
            Sync() && s_unseen.Contains(MakeKey(kind, name));

        public static void MarkUnseen(UnlockKind kind, string name)
        {
            if (Sync() && s_unseen.Add(MakeKey(kind, name))) Save();
        }

        /// <summary>Marks the given keys as unseen, once per character. Returns false if it already ran.</summary>
        public static bool FlagAllOnce(IEnumerable<string> keys)
        {
            if (!Sync() || s_owner.m_customData.ContainsKey(FlaggedAllKey)) return false;
            s_owner.m_customData[FlaggedAllKey] = "1";
            foreach (string key in keys) s_unseen.Add(key);
            Save();
            return true;
        }

        public static void MarkSeen(string key)
        {
            if (Sync() && s_unseen.Remove(key)) Save();
        }

        public static void MarkSeen(IEnumerable<string> keys)
        {
            if (!Sync()) return;
            bool changed = false;
            foreach (string key in keys) changed |= s_unseen.Remove(key);
            if (changed) Save();
        }

        /// <summary>Makes sure the cache belongs to the current local player.</summary>
        private static bool Sync()
        {
            Player player = Player.m_localPlayer;
            if (player == null) return false;
            if (s_owner == player) return true;

            s_owner = player;
            s_unseen.Clear();
            if (player.m_customData.TryGetValue(CustomDataKey, out string raw) && !string.IsNullOrEmpty(raw))
            {
                foreach (string key in raw.Split(Separator))
                {
                    if (key.Length > 0) s_unseen.Add(key);
                }
            }
            return true;
        }

        private static void Save()
        {
            if (s_unseen.Count == 0)
                s_owner.m_customData.Remove(CustomDataKey);
            else
                s_owner.m_customData[CustomDataKey] = string.Join(Separator.ToString(), s_unseen);
            Changed?.Invoke();
        }
    }
}
