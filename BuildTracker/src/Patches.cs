using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BuildTracker
{
    /// <summary>
    /// Adds the "Not built" tab after the vanilla build menu tabs. BuildUi.Awake wires one piece list
    /// to each child of its tab container in order, so the cloned tab is added in a prefix (the game
    /// hooks up its click handler) and the matching piece list in the postfix.
    /// </summary>
    [HarmonyPatch(typeof(BuildUi))]
    internal static class BuildUiPatches
    {
        private static readonly AccessTools.FieldRef<BuildUi, RectTransform> TabContainerRef =
            AccessTools.FieldRefAccess<BuildUi, RectTransform>("m_tabContainer");
        private static readonly AccessTools.FieldRef<BuildUi, TabHandler> TabHandlerRef =
            AccessTools.FieldRefAccess<BuildUi, TabHandler>("m_tabHandler");
        private static readonly AccessTools.FieldRef<BuildUi, List<IPieceList>> PieceListsRef =
            AccessTools.FieldRefAccess<BuildUi, List<IPieceList>>("m_pieceLists");
        private static readonly AccessTools.FieldRef<BuildUi, int> AvailablePieceCountRef =
            AccessTools.FieldRefAccess<BuildUi, int>("m_availablePieceCount");
        private static readonly AccessTools.FieldRef<BuildUi, int> CurrentPieceListRef =
            AccessTools.FieldRefAccess<BuildUi, int>("m_currentPieceList");
        private static readonly AccessTools.FieldRef<BuildUi, int> CurrentTagIdRef =
            AccessTools.FieldRefAccess<BuildUi, int>("m_currentTagId");
        private static readonly AccessTools.FieldRef<BuildUi, PieceTable> CurrentBuildToolRef =
            AccessTools.FieldRefAccess<BuildUi, PieceTable>("m_currentBuildTool");
        private static readonly AccessTools.FieldRef<BuildUi, RectTransform> PieceViewRef =
            AccessTools.FieldRefAccess<BuildUi, RectTransform>("m_pieceView");
        private static readonly AccessTools.FieldRef<BuildUi, ScrollRect> PieceScrollRectRef =
            AccessTools.FieldRefAccess<BuildUi, ScrollRect>("m_pieceScrollRect");

        private static UndiscoveredLabel s_undiscoveredLabel;

        [HarmonyPatch("Awake")]
        [HarmonyPrefix]
        private static void Awake_Prefix(BuildUi __instance, out bool __state)
        {
            __state = false;
            RectTransform container = TabContainerRef(__instance);
            TabHandler tabHandler = TabHandlerRef(__instance);
            // Every tab must map 1:1 to a piece list; bail out if a game update changes that.
            if (container == null || tabHandler == null || container.childCount < 2 ||
                tabHandler.m_tabs.Count != container.childCount)
            {
                BuildTrackerPlugin.Log.LogWarning("Build menu tabs have an unexpected layout; the Not built tab is disabled.");
                return;
            }

            RectTransform last = (RectTransform)container.GetChild(container.childCount - 1);
            RectTransform previous = (RectTransform)container.GetChild(container.childCount - 2);
            RectTransform tab = Object.Instantiate(last, container);
            tab.name = "BuildTracker_NotBuiltTab";

            // Without a layout group the tabs are placed by hand; continue the row with the same spacing.
            if (container.GetComponent<LayoutGroup>() == null)
                tab.anchoredPosition = last.anchoredPosition + (last.anchoredPosition - previous.anchoredPosition);

            foreach (TMP_Text text in tab.GetComponentsInChildren<TMP_Text>(true))
                text.text = BuildTrackerPlugin.TabName.Value;

            Button button = tab.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            tabHandler.m_tabs.Add(new TabHandler.Tab { m_button = button });
            __state = true;
        }

        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        private static void Awake_Postfix(BuildUi __instance, bool __state)
        {
            s_undiscoveredLabel = null;
            if (!__state) return;

            PieceListsRef(__instance).Add(new UnbuiltPieceList(BuildTrackerPlugin.TabName.Value));
            RectTransform pieceView = PieceViewRef(__instance);
            if (pieceView != null)
                s_undiscoveredLabel = new UndiscoveredLabel(pieceView, PieceScrollRectRef(__instance),
                    TabContainerRef(__instance).GetComponentInChildren<TMP_Text>(true));
        }

        // Runs whenever the visible pieces change: tab, category or tool switch, or a refresh.
        [HarmonyPatch("UpdatePieceButtons")]
        [HarmonyPostfix]
        private static void UpdatePieceButtons_Postfix(BuildUi __instance)
        {
            if (s_undiscoveredLabel == null) return;

            List<IPieceList> lists = PieceListsRef(__instance);
            int index = CurrentPieceListRef(__instance);
            PieceTable tool = CurrentBuildToolRef(__instance);
            if (tool != null && !tool.m_hideAdvancedMenu &&
                index >= 0 && index < lists.Count && lists[index] is UnbuiltPieceList unbuilt)
            {
                s_undiscoveredLabel.Show(BuildTrackerPlugin.ShowUndiscovered.Value
                    ? unbuilt.CountUndiscovered(tool, CurrentTagIdRef(__instance)) : 0);

                string congrats = null;
                if (unbuilt.AllDiscoveredBuilt(tool))
                    congrats = unbuilt.CountUndiscovered(tool, -1) == 0
                        ? BuildTrackerPlugin.CongratsText.Value
                        : BuildTrackerPlugin.CongratsSoFarText.Value;
                s_undiscoveredLabel.ShowCongrats(congrats);
            }
            else
                s_undiscoveredLabel.Hide();
        }

        /// <summary>Makes the build menu rebuild its lists on the next frame.</summary>
        internal static void Refresh()
        {
            BuildUi buildUi = Hud.instance != null ? Hud.instance.m_buildUi : null;
            if (buildUi != null)
                AvailablePieceCountRef(buildUi) = -1;
        }
    }

    /// <summary>Drops a piece from the Not built tab as soon as it is placed for the first time.</summary>
    [HarmonyPatch(typeof(PlayerProfile), nameof(PlayerProfile.IncrementStatBuildPiecePlaced))]
    internal static class PiecePlacedPatch
    {
        private static void Postfix() => BuildUiPatches.Refresh();
    }
}
