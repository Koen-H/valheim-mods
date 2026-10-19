// Seasonal recipes and build pieces all year (off by default).
//
// The game keeps a list of seasonal groups on the player (Player.m_seasonalItemGroups). Each group has dates and
// a list of recipes and build pieces that are disabled (m_enabled = false) outside those dates; during the event
// the player treats that one group's entries as enabled. We switch every group's recipes and pieces on.
// You still learn them the normal way: by having the materials (recipes) or the required items (pieces). Once
// on, seasonal pieces count like any other piece in the "Not built" tab and the "not discovered yet" bar.
// The original values are kept, so turning a setting off puts them back.

using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BuildTracker
{
    [HarmonyPatch]
    internal static class Seasons
    {
        private static readonly AccessTools.FieldRef<Player, List<SeasonalItemGroup>> GroupsRef =
            AccessTools.FieldRefAccess<Player, List<SeasonalItemGroup>>("m_seasonalItemGroups");

        // Vanilla m_enabled of every seasonal recipe and piece we've seen.
        private static readonly Dictionary<Recipe, bool> RecipeDefaults = new Dictionary<Recipe, bool>();
        private static readonly Dictionary<Piece, bool> PieceDefaults = new Dictionary<Piece, bool>();

        // The groups live on the player prefab, so they're known as soon as a player spawns. The player only
        // looks at m_enabled when it updates its recipe lists, which happens after Awake.
        [HarmonyPatch(typeof(Player), "Awake")]
        [HarmonyPostfix]
        private static void Player_Awake(Player __instance)
        {
            List<SeasonalItemGroup> groups = GroupsRef(__instance);
            if (groups == null)
                return;

            bool added = false;
            foreach (SeasonalItemGroup group in groups)
            {
                if (group == null)
                    continue;
                foreach (Recipe recipe in group.Recipes)
                {
                    if (recipe == null || RecipeDefaults.ContainsKey(recipe))
                        continue;
                    RecipeDefaults[recipe] = recipe.m_enabled;
                    added = true;
                }
                foreach (GameObject prefab in group.Pieces)
                {
                    Piece piece = prefab != null ? prefab.GetComponent<Piece>() : null;
                    if (piece == null || PieceDefaults.ContainsKey(piece))
                        continue;
                    PieceDefaults[piece] = piece.m_enabled;
                    added = true;
                }
            }

            if (added)
                BuildTrackerPlugin.Log.LogInfo(
                    $"Seasonal events: {groups.Count}, recipes: {RecipeDefaults.Count}, build pieces: {PieceDefaults.Count}");
            Apply();
        }

        public static void Apply()
        {
            foreach (KeyValuePair<Recipe, bool> r in RecipeDefaults)
            {
                if (r.Key != null)
                    r.Key.m_enabled = r.Value || BuildTrackerPlugin.SeasonalRecipes.Value;
            }
            foreach (KeyValuePair<Piece, bool> p in PieceDefaults)
            {
                if (p.Key != null)
                    p.Key.m_enabled = p.Value || BuildTrackerPlugin.SeasonalPieces.Value;
            }
        }

        public static void Restore()
        {
            foreach (KeyValuePair<Recipe, bool> r in RecipeDefaults)
            {
                if (r.Key != null)
                    r.Key.m_enabled = r.Value;
            }
            foreach (KeyValuePair<Piece, bool> p in PieceDefaults)
            {
                if (p.Key != null)
                    p.Key.m_enabled = p.Value;
            }
        }
    }
}
