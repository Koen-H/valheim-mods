using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace UnlockAwareness
{
    /// <summary>Records newly learned recipes, build pieces and items as unseen.</summary>
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

        // The game calls AddKnownItem for everything that lands in the inventory, and only shows its
        // "new item" message the first time. Items owned before installing are already known, so they stay unmarked.
        [HarmonyPatch(typeof(Player), nameof(Player.AddKnownItem))]
        [HarmonyPrefix]
        private static void AddKnownItem_Prefix(Player __instance, ItemDrop.ItemData item, out bool __state) =>
            __state = item != null && !__instance.IsKnownMaterial(item.m_shared.m_name);

        [HarmonyPatch(typeof(Player), nameof(Player.AddKnownItem))]
        [HarmonyPostfix]
        private static void AddKnownItem_Postfix(Player __instance, ItemDrop.ItemData item, bool __state)
        {
            if (__state && UnlockAwarenessPlugin.InventoryBadges.Value && __instance == Player.m_localPlayer)
                UnseenStore.MarkUnseen(UnlockKind.Item, item.m_shared.m_name);
        }
    }

    /// <summary>
    /// Badges on items in the inventory and in an open chest: "!" on new items until clicked, and "?" on chest
    /// items this character has never had. With LearnFromContainers, opening a chest learns its items instead.
    /// </summary>
    [HarmonyPatch]
    internal static class InventoryPatches
    {
        private static readonly AccessTools.FieldRef<InventoryGrid, List<InventoryElement>> ElementsRef =
            AccessTools.FieldRefAccess<InventoryGrid, List<InventoryElement>>("m_elements");
        private static readonly AccessTools.FieldRef<InventoryGui, InventoryGrid> ContainerGridRef =
            AccessTools.FieldRefAccess<InventoryGui, InventoryGrid>("m_containerGrid");
        private static readonly System.Action<Player> UpdateKnownRecipesList =
            AccessTools.MethodDelegate<System.Action<Player>>(AccessTools.Method(typeof(Player), "UpdateKnownRecipesList"));

        // Badges per slot, remembered after they are first made: the grids update every frame while the
        // inventory is open. Slots are destroyed when a grid changes size; their entries are then dropped.
        private static readonly Dictionary<InventoryElement, UnseenBadge> s_badges = new Dictionary<InventoryElement, UnseenBadge>();
        private static readonly Dictionary<InventoryElement, GameObject> s_unknownBadges = new Dictionary<InventoryElement, GameObject>();
        private static ItemDrop.ItemData[] s_slots = new ItemDrop.ItemData[0];
        private static int s_width;

        // Runs every frame while the inventory is open, for the player grid and an open chest's grid.
        [HarmonyPatch(typeof(InventoryGrid), "UpdateGui")]
        [HarmonyPostfix]
        private static void UpdateGui_Postfix(InventoryGrid __instance)
        {
            InventoryGui gui = InventoryGui.instance;
            if (gui == null) return;
            bool container = __instance == ContainerGridRef(gui);
            if (!container && __instance != gui.m_playerGrid) return;

            if (container && UnlockAwarenessPlugin.LearnFromContainers.Value) LearnItems(__instance.GetInventory());
            FillSlots(__instance.GetInventory());
            UpdateNewBadges(__instance);
            if (container) UpdateUnknownBadges(__instance);
        }

        // Grid position -> item, once per call instead of a search through the inventory for every slot.
        private static void FillSlots(Inventory inventory)
        {
            if (inventory == null)
            {
                s_width = 0;
                System.Array.Clear(s_slots, 0, s_slots.Length);
                return;
            }
            s_width = inventory.GetWidth();
            int size = s_width * inventory.GetHeight();
            if (s_slots.Length < size) s_slots = new ItemDrop.ItemData[size];
            System.Array.Clear(s_slots, 0, s_slots.Length);
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                int index = item.m_gridPos.y * s_width + item.m_gridPos.x;
                if (index >= 0 && index < size) s_slots[index] = item;
            }
        }

        private static ItemDrop.ItemData ItemIn(InventoryElement element)
        {
            if (!element.m_used || s_width == 0) return null;
            int index = element.Position.y * s_width + element.Position.x;
            return index >= 0 && index < s_slots.Length ? s_slots[index] : null;
        }

        // Opening a chest counts as having its items: the game learns them exactly like a pickup, so the
        // "new item" message, the "!" (via AddKnownItem) and any recipes or pieces they unlock all follow.
        private static void LearnItems(Inventory inventory)
        {
            Player player = Player.m_localPlayer;
            if (player == null || inventory == null) return;
            bool learned = false;
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (player.IsKnownMaterial(item.m_shared.m_name)) continue;
                player.AddKnownItem(item);
                learned = true;
            }
            // Picked-up items trigger this through the inventory; chest items don't, so do it here.
            if (learned) UpdateKnownRecipesList(player);
        }

        private static void UpdateNewBadges(InventoryGrid grid)
        {
            bool show = UnlockAwarenessPlugin.InventoryBadges.Value && UnseenStore.HasAnyUnseen;

            foreach (InventoryElement element in ElementsRef(grid))
            {
                ItemDrop.ItemData item = show ? ItemIn(element) : null;
                bool unseen = item != null && UnseenStore.IsUnseenItem(item.m_shared.m_name);

                if (!s_badges.TryGetValue(element, out UnseenBadge badge) || badge == null)
                {
                    if (!unseen) continue;
                    // Top center: the corners hold the hotbar number, quality and other game markers.
                    badge = UiFactory.GetOrCreateBadge(element.transform, 18f, new Vector2(0.5f, 1f), new Vector2(0f, -10f));
                    Remember(s_badges, element, badge);
                }

                if (unseen)
                {
                    // The key is only built when the badge starts showing this item.
                    if (!ShowsItem(badge, item.m_shared.m_name))
                        badge.Show(UnseenStore.MakeKey(UnlockKind.Item, item.m_shared.m_name));
                }
                else if (badge.gameObject.activeSelf)
                {
                    badge.Hide();
                }
            }
        }

        private static bool ShowsItem(UnseenBadge badge, string name) =>
            badge.gameObject.activeSelf && badge.Key != null && badge.Key.Length == name.Length + 2 &&
            badge.Key.StartsWith("i:", System.StringComparison.Ordinal) &&
            badge.Key.EndsWith(name, System.StringComparison.Ordinal);

        // A "?" on chest items this character has never had. Taking one makes the game learn it
        // (AddKnownItem), which swaps the "?" for the "!" in the player's inventory.
        private static void UpdateUnknownBadges(InventoryGrid grid)
        {
            Player player = Player.m_localPlayer;
            bool show = UnlockAwarenessPlugin.ContainerBadges.Value && player != null;

            foreach (InventoryElement element in ElementsRef(grid))
            {
                ItemDrop.ItemData item = show ? ItemIn(element) : null;
                bool unknown = item != null && !player.IsKnownMaterial(item.m_shared.m_name);

                if (!s_unknownBadges.TryGetValue(element, out GameObject badge) || badge == null)
                {
                    if (!unknown) continue;
                    badge = UiFactory.GetOrCreateUnknownBadge(element.transform, element.m_amount, 22f, new Vector2(0.5f, 1f), new Vector2(0f, -11f));
                    Remember(s_unknownBadges, element, badge);
                }
                if (badge.activeSelf != unknown) badge.SetActive(unknown);
            }
        }

        private static void Remember<T>(Dictionary<InventoryElement, T> badges, InventoryElement element, T badge) where T : Object
        {
            badges[element] = badge;
            if (badges.Count <= 256) return;
            var gone = new List<InventoryElement>();
            foreach (KeyValuePair<InventoryElement, T> pair in badges)
            {
                if (pair.Key == null || pair.Value == null) gone.Add(pair.Key);
            }
            foreach (InventoryElement key in gone) badges.Remove(key);
        }

        // Left click (or gamepad select) and right click (use/equip) both count as looking at the item.
        [HarmonyPatch(typeof(InventoryGui), "OnSelectedItem")]
        [HarmonyPrefix]
        private static void OnSelectedItem_Prefix(InventoryGui __instance, InventoryGrid grid, ItemDrop.ItemData item) =>
            MarkSeen(__instance, grid, item);

        [HarmonyPatch(typeof(InventoryGui), "OnRightClickItem")]
        [HarmonyPrefix]
        private static void OnRightClickItem_Prefix(InventoryGui __instance, InventoryGrid grid, ItemDrop.ItemData item) =>
            MarkSeen(__instance, grid, item);

        // The badges themselves follow on the next UpdateGui, which also clears other stacks of the same item.
        private static void MarkSeen(InventoryGui gui, InventoryGrid grid, ItemDrop.ItemData item)
        {
            if (item != null && (grid == gui.m_playerGrid || grid == ContainerGridRef(gui)))
                UnseenStore.MarkSeen(UnseenStore.MakeKey(UnlockKind.Item, item.m_shared.m_name));
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
