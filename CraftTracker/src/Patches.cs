using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CraftTracker
{
    /// <summary>
    /// Adds a "Not crafted" tab next to Craft and Upgrade. While it is active the game stays in its
    /// Craft mode (InCraftTab returns true, so crafting works as usual), but the recipe list is filtered
    /// down to items this character has never crafted.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGui))]
    internal static class CraftingPatches
    {
        private static readonly MethodInfo UpdateCraftingPanelMethod =
            AccessTools.Method(typeof(InventoryGui), "UpdateCraftingPanel");
        private static readonly MethodInfo SetActiveGroupMethod =
            AccessTools.Method(typeof(InventoryGui), "SetActiveGroup", new[] { typeof(int), typeof(bool) });

        // Index of the crafting panel group, as used by the vanilla tab handlers.
        private const int CraftingGroup = 3;

        private static Button s_tab;
        private static StatusOverlay s_overlay;
        private static bool s_active;
        private static bool s_refreshPending;

        // Tab positions when the tabs are placed by hand: after Upgrade, or in its place when Upgrade is hidden.
        private static bool s_manualLayout;
        private static Vector2 s_slotAfterUpgrade;
        private static Vector2 s_slotUpgrade;

        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        private static void Awake_Postfix(InventoryGui __instance)
        {
            s_active = false;
            s_refreshPending = false;

            var craftRect = (RectTransform)__instance.m_tabCraft.transform;
            var upgradeRect = (RectTransform)__instance.m_tabUpgrade.transform;
            RectTransform tabRect = Object.Instantiate(upgradeRect, upgradeRect.parent);
            tabRect.name = "CraftTracker_NotCraftedTab";
            tabRect.SetSiblingIndex(upgradeRect.GetSiblingIndex() + 1);

            s_manualLayout = upgradeRect.parent.GetComponent<LayoutGroup>() == null;
            s_slotUpgrade = upgradeRect.anchoredPosition;
            s_slotAfterUpgrade = upgradeRect.anchoredPosition + (upgradeRect.anchoredPosition - craftRect.anchoredPosition);
            if (s_manualLayout)
                tabRect.anchoredPosition = s_slotAfterUpgrade;

            // The vanilla tab labels are upper case (CRAFT, UPGRADE).
            foreach (TMP_Text text in tabRect.GetComponentsInChildren<TMP_Text>(true))
                text.text = CraftTrackerPlugin.TabName.Value.ToUpperInvariant();

            // The vanilla tabs call OnTabUpgradePressed through persistent listeners; drop the copied one.
            s_tab = tabRect.GetComponent<Button>();
            s_tab.onClick = new Button.ButtonClickedEvent();
            s_tab.onClick.AddListener(() => OnNotCraftedPressed(__instance));
            s_tab.interactable = true;

            var listParent = (RectTransform)__instance.m_recipeListRoot.parent;
            s_overlay = new StatusOverlay(listParent, __instance.m_tabCraft.GetComponentInChildren<TMP_Text>(true));
        }

        private static void OnNotCraftedPressed(InventoryGui gui)
        {
            s_active = true;
            if (gui.m_uiGroups.Length > CraftingGroup)
                SetActiveGroupMethod?.Invoke(gui, new object[] { CraftingGroup, true });
            UpdateCraftingPanel(gui);
        }

        private static void UpdateCraftingPanel(InventoryGui gui) =>
            UpdateCraftingPanelMethod.Invoke(gui, new object[] { false });

        private static bool IsActive(InventoryGui gui) => s_active && gui.m_tabCraft.gameObject.activeSelf;

        [HarmonyPatch(nameof(InventoryGui.OnTabCraftPressed))]
        [HarmonyPrefix]
        private static void OnTabCraftPressed_Prefix() => s_active = false;

        [HarmonyPatch(nameof(InventoryGui.OnTabUpgradePressed))]
        [HarmonyPrefix]
        private static void OnTabUpgradePressed_Prefix() => s_active = false;

        [HarmonyPatch(nameof(InventoryGui.InCraftTab))]
        [HarmonyPostfix]
        private static void InCraftTab_Postfix(InventoryGui __instance, ref bool __result)
        {
            if (IsActive(__instance))
                __result = true;
        }

        [HarmonyPatch("UpdateCraftingPanel")]
        [HarmonyPostfix]
        private static void UpdateCraftingPanel_Postfix(InventoryGui __instance)
        {
            if (s_tab == null) return;

            // Stations without a Craft tab (upgrade only) don't get a Not crafted tab either.
            bool available = __instance.m_tabCraft.gameObject.activeSelf;
            s_tab.gameObject.SetActive(available);
            if (!available)
                s_active = false;
            if (s_manualLayout)
                ((RectTransform)s_tab.transform).anchoredPosition =
                    __instance.m_tabUpgrade.gameObject.activeSelf ? s_slotAfterUpgrade : s_slotUpgrade;

            // Like the vanilla tabs, the selected tab is the one that can't be clicked.
            if (s_active)
            {
                __instance.m_tabCraft.interactable = true;
                __instance.m_tabUpgrade.interactable = true;
            }
            s_tab.interactable = !s_active;
        }

        [HarmonyPatch("UpdateRecipeList")]
        [HarmonyPrefix]
        private static void UpdateRecipeList_Prefix(InventoryGui __instance, List<Recipe> recipes)
        {
            // The list is the game's own temporary copy, rebuilt on every refresh.
            if (IsActive(__instance))
                recipes.RemoveAll(r => r.m_noCraftOnlyUpgrade || CraftedItems.IsCrafted(r));
        }

        [HarmonyPatch("UpdateRecipeList")]
        [HarmonyPostfix]
        private static void UpdateRecipeList_Postfix(InventoryGui __instance, List<Recipe> recipes)
        {
            if (s_overlay == null) return;
            if (!IsActive(__instance))
            {
                s_overlay.Hide();
                return;
            }

            int undiscovered = CraftedItems.CountUndiscovered(Player.m_localPlayer);
            s_overlay.ShowUndiscovered(CraftTrackerPlugin.ShowUndiscovered.Value ? undiscovered : 0);
            s_overlay.ShowCongrats(recipes.Count > 0 ? null
                : undiscovered == 0 ? CraftTrackerPlugin.CongratsText.Value
                : CraftTrackerPlugin.CongratsSoFarText.Value);

            // Leave room after the last recipe so the bar never covers it.
            if (s_overlay.BarVisible)
            {
                RectTransform root = __instance.m_recipeListRoot;
                float needed = recipes.Count * __instance.m_recipeListSpace + StatusOverlay.BarHeight;
                if (needed > root.rect.height)
                    root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, needed);
            }
        }

        // The game refreshes the panel before it records the craft, so refresh once more afterwards.
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        private static void Update_Postfix(InventoryGui __instance)
        {
            if (!s_refreshPending) return;
            s_refreshPending = false;
            if (IsActive(__instance) && InventoryGui.IsVisible())
                UpdateCraftingPanel(__instance);
        }

        internal static void RequestRefresh() => s_refreshPending = true;
    }

    [HarmonyPatch(typeof(PlayerProfile), nameof(PlayerProfile.IncrementStatItemCraft))]
    internal static class ItemCraftedPatch
    {
        private static void Postfix() => CraftingPatches.RequestRefresh();
    }
}
