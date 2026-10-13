using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace UnlockAwareness
{
    /// <summary>Records newly learned recipes and build pieces as unseen.</summary>
    [HarmonyPatch]
    internal static class UnlockTrackingPatches
    {
        [HarmonyPatch(typeof(Player), "AddKnownRecipe")]
        [HarmonyPrefix]
        private static void AddKnownRecipe_Prefix(Player __instance, Recipe recipe, out bool __state) =>
            __state = !__instance.IsRecipeKnown(recipe.m_item.m_itemData.m_shared.m_name);

        [HarmonyPatch(typeof(Player), "AddKnownRecipe")]
        [HarmonyPostfix]
        private static void AddKnownRecipe_Postfix(Player __instance, Recipe recipe, bool __state)
        {
            string name = recipe.m_item.m_itemData.m_shared.m_name;
            if (__state && __instance == Player.m_localPlayer && __instance.IsRecipeKnown(name))
                UnseenStore.MarkUnseen(UnlockKind.Recipe, name);
        }

        [HarmonyPatch(typeof(Player), "AddKnownPiece")]
        [HarmonyPrefix]
        private static void AddKnownPiece_Prefix(Player __instance, Piece piece, out bool __state) =>
            __state = !__instance.IsRecipeKnown(piece.m_name);

        [HarmonyPatch(typeof(Player), "AddKnownPiece")]
        [HarmonyPostfix]
        private static void AddKnownPiece_Postfix(Player __instance, Piece piece, bool __state)
        {
            if (__state && __instance == Player.m_localPlayer && __instance.IsRecipeKnown(piece.m_name))
                UnseenStore.MarkUnseen(UnlockKind.Piece, piece.m_name);
        }
    }

    /// <summary>
    /// Badges and the "Mark all as seen" button in the crafting panel. The button only covers the
    /// recipes listed at the current crafting station (or crafting by hand when there is no station).
    /// </summary>
    [HarmonyPatch(typeof(InventoryGui))]
    internal static class CraftingPatches
    {
        private static MarkAllButton s_markAllButton;

        // Unseen recipe keys shown in the currently built recipe list.
        private static readonly HashSet<string> s_listedUnseen = new HashSet<string>();

        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        private static void Awake_Postfix(InventoryGui __instance)
        {
            s_listedUnseen.Clear();
            s_markAllButton = UiFactory.CreateCraftingMarkAllButton(__instance, () => MarkAllSeen(__instance));
            RefreshButton();
        }

        [HarmonyPatch("UpdateRecipeList")]
        [HarmonyPrefix]
        private static void UpdateRecipeList_Prefix() => s_listedUnseen.Clear();

        [HarmonyPatch("UpdateRecipeList")]
        [HarmonyPostfix]
        private static void UpdateRecipeList_Postfix() => RefreshButton();

        // The row created by AddRecipeToList is always the last child of the recipe list.
        [HarmonyPatch("AddRecipeToList")]
        [HarmonyPostfix]
        private static void AddRecipeToList_Postfix(InventoryGui __instance, Recipe recipe, ItemDrop.ItemData item)
        {
            Transform root = __instance.m_recipeListRoot;
            if (root.childCount == 0) return;
            Transform row = root.GetChild(root.childCount - 1);
            Transform icon = row.Find("icon") ?? row;

            UnseenBadge badge = UiFactory.GetOrCreateBadge(icon, 16f, new Vector2(1f, 1f), new Vector2(-4f, -6f));
            string name = recipe.m_item.m_itemData.m_shared.m_name;
            // Only the Craft tab (item == null) shows badges; the Upgrade tab lists items you already own.
            if (item == null && UnseenStore.IsUnseen(UnlockKind.Recipe, name))
            {
                string key = UnseenStore.MakeKey(UnlockKind.Recipe, name);
                badge.Show(key);
                s_listedUnseen.Add(key);
            }
            else
            {
                badge.Hide();
            }
        }

        [HarmonyPatch("OnSelectedRecipe")]
        [HarmonyPostfix]
        private static void OnSelectedRecipe_Postfix(GameObject button)
        {
            UnseenBadge badge = button != null ? button.GetComponentInChildren<UnseenBadge>(true) : null;
            if (badge == null) return;

            string key = badge.Key;
            if (!badge.Consume()) return;

            // The same item can appear in more than one row (e.g. different amounts); clear them all.
            foreach (UnseenBadge other in button.transform.parent.GetComponentsInChildren<UnseenBadge>(true))
                if (other.Key == key) other.Hide();
            s_listedUnseen.Remove(key);
            RefreshButton();
        }

        private static void MarkAllSeen(InventoryGui gui)
        {
            UnseenStore.MarkSeen(s_listedUnseen);
            s_listedUnseen.Clear();
            foreach (UnseenBadge badge in gui.m_recipeListRoot.GetComponentsInChildren<UnseenBadge>(true))
                badge.Hide();
            RefreshButton();
        }

        private static void RefreshButton() =>
            s_markAllButton?.Refresh(s_listedUnseen.Count);
    }

    /// <summary>
    /// Badges and the "Mark all as seen" button in the build menu. The button only covers the pieces
    /// of the build tool currently in hand (hammer, hoe, cultivator, ...).
    /// </summary>
    [HarmonyPatch]
    internal static class BuildPatches
    {
        private static readonly AccessTools.FieldRef<BuildUi, RectTransform> SpecialButtonsRef =
            AccessTools.FieldRefAccess<BuildUi, RectTransform>("m_specialButtonsContainer");
        private static readonly AccessTools.FieldRef<BuildUi, GameObject> PieceButtonPrefabRef =
            AccessTools.FieldRefAccess<BuildUi, GameObject>("m_pieceButtonPrefab");

        private static MarkAllButton s_markAllButton;

        static BuildPatches()
        {
            UnseenStore.Changed += RefreshButton;
        }

        [HarmonyPatch(typeof(BuildUiPieceButton), nameof(BuildUiPieceButton.Setup))]
        [HarmonyPostfix]
        private static void PieceButtonSetup_Postfix(BuildUiPieceButton __instance, Piece pieceInfo)
        {
            UnseenBadge badge = UiFactory.GetOrCreateBadge(__instance.transform, 20f, new Vector2(1f, 1f), new Vector2(-10f, -13f));
            // Buttons are pooled and reused, so always reset the badge state.
            if (pieceInfo != null && UnseenStore.IsUnseen(UnlockKind.Piece, pieceInfo.m_name))
                badge.Show(UnseenStore.MakeKey(UnlockKind.Piece, pieceInfo.m_name));
            else
                badge.Hide();
        }

        [HarmonyPatch(typeof(BuildUi), nameof(BuildUi.OnSelectPiece))]
        [HarmonyPrefix]
        private static void OnSelectPiece_Prefix(BuildUi __instance, Piece piece)
        {
            if (piece == null) return;
            string key = UnseenStore.MakeKey(UnlockKind.Piece, piece.m_name);
            UnseenStore.MarkSeen(key);
            HideBadges(__instance, key);
        }

        [HarmonyPatch(typeof(BuildUi), nameof(BuildUi.OpenBuildMenu))]
        [HarmonyPostfix]
        private static void OpenBuildMenu_Postfix(BuildUi __instance)
        {
            if (s_markAllButton?.Button == null)
            {
                // The special buttons (repair tool) sit in the top bar next to the search field.
                RectTransform special = SpecialButtonsRef(__instance);
                s_markAllButton = UiFactory.CreateBuildMarkAllButton(PieceButtonPrefabRef(__instance),
                    special != null ? special.parent : null, () => MarkAllSeen(__instance));
            }
            RefreshButton();
        }

        private static IEnumerable<string> UnseenKeysForCurrentTool()
        {
            PieceTable tool = Player.m_localPlayer != null ? Player.m_localPlayer.GetBuildTool() : null;
            if (tool == null) yield break;
            foreach (Piece piece in tool.m_availablePieces)
                if (piece != null && UnseenStore.IsUnseen(UnlockKind.Piece, piece.m_name))
                    yield return UnseenStore.MakeKey(UnlockKind.Piece, piece.m_name);
        }

        private static void MarkAllSeen(BuildUi buildUi)
        {
            UnseenStore.MarkSeen(UnseenKeysForCurrentTool().ToList());
            HideBadges(buildUi, null);
        }

        /// <summary>Hides badges with the given key, or all badges when key is null.</summary>
        private static void HideBadges(BuildUi buildUi, string key)
        {
            foreach (UnseenBadge badge in buildUi.GetComponentsInChildren<UnseenBadge>(true))
                if (key == null || badge.Key == key) badge.Hide();
        }

        private static void RefreshButton() =>
            s_markAllButton?.Refresh(UnseenKeysForCurrentTool().Count());
    }
}
