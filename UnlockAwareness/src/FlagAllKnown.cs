using System.Collections.Generic;
using HarmonyLib;

namespace UnlockAwareness
{
    /// <summary>Optional one-time pass that marks everything a character already knows as unseen.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    internal static class FlagAllKnown
    {
        private static readonly AccessTools.FieldRef<Player, HashSet<string>> KnownRecipesRef =
            AccessTools.FieldRefAccess<Player, HashSet<string>>("m_knownRecipes");

        [HarmonyPostfix]
        private static void Postfix(Player __instance)
        {
            if (!UnlockAwarenessPlugin.FlagAllKnownOnce.Value || __instance != Player.m_localPlayer || ObjectDB.instance == null) return;

            if (UnseenStore.FlagAllOnce(CollectKnownKeys(__instance)))
                UnlockAwarenessPlugin.Log.LogInfo("Flagged all known recipes and pieces as new for this character");
        }

        private static List<string> CollectKnownKeys(Player player)
        {
            HashSet<string> known = KnownRecipesRef(player);
            var keys = new List<string>();

            foreach (Recipe recipe in ObjectDB.instance.m_recipes)
            {
                if (recipe == null || recipe.m_item == null) continue;
                string name = recipe.m_item.m_itemData.m_shared.m_name;
                if (known.Contains(name)) keys.Add(UnseenStore.MakeKey(UnlockKind.Recipe, name));
            }

            // Build pieces live in the piece tables of build tools (hammer, hoe, cultivator, ...).
            foreach (UnityEngine.GameObject itemPrefab in ObjectDB.instance.m_items)
            {
                PieceTable table = itemPrefab?.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_buildPieces;
                if (table == null) continue;
                foreach (UnityEngine.GameObject piecePrefab in table.m_pieces)
                {
                    Piece piece = piecePrefab != null ? piecePrefab.GetComponent<Piece>() : null;
                    if (piece != null && known.Contains(piece.m_name))
                        keys.Add(UnseenStore.MakeKey(UnlockKind.Piece, piece.m_name));
                }
            }
            return keys;
        }
    }
}
