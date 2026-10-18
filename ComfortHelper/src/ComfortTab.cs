// The Comfort tab in the build menu (hammer, cultivator...): every comfort piece the tool can build, grouped by
// comfort group, because only the best piece of a group counts. Check marks show what is within 10 m:
// green = it counts, grey = it is here but a better piece of its group counts instead.
//
// BuildUi.Awake wires one piece list to each child of its tab container in order, so the tab is cloned in a
// prefix (the game hooks up its click handler) and the matching piece list is added in the postfix.
// The hover text of comfort pieces (Hud.SetupPieceInfo) gets their comfort and what they would add here.

using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ComfortHelper
{
    internal static class ComfortGroups
    {
        internal static string Name(Piece.ComfortGroup group)
        {
            switch (group)
            {
                case Piece.ComfortGroup.Fire: return "Fires";
                case Piece.ComfortGroup.Bed: return "Beds";
                case Piece.ComfortGroup.Banner: return "Banners";
                case Piece.ComfortGroup.Chair: return "Seats";
                case Piece.ComfortGroup.Table: return "Tables";
                case Piece.ComfortGroup.Carpet: return "Rugs";
                case Piece.ComfortGroup.Display: return "Displays";
                case Piece.ComfortGroup.Decor: return "Decorations";
                case Piece.ComfortGroup.Garland: return "Garlands";
                case Piece.ComfortGroup.Lantern: return "Lanterns";
                case Piece.ComfortGroup.Leisure: return "Leisure";
                case Piece.ComfortGroup.None: return "Each counts";
                default: return group.ToString();
            }
        }
    }

    /// <summary>Build menu list with the comfort pieces of the tool, one category per comfort group.</summary>
    internal sealed class ComfortPieceList : IPieceList
    {
        private readonly Piece.ComfortGroup[] _groups = (Piece.ComfortGroup[])Enum.GetValues(typeof(Piece.ComfortGroup));
        private readonly List<int> _availableTags = new List<int>();

        public ComfortPieceList(string displayName) => DisplayName = displayName;

        public string DisplayName { get; }

        public bool ShowTags => true;

        public bool CanCustomizeTags => false;

        public int TagCount => _availableTags.Count;

        public int TagSeparatorIndex => -1;

        // "Seats +2" in green when a seat already counts here.
        public string GetTagDisplayName(int index)
        {
            Piece.ComfortGroup group = _groups[_availableTags[index]];
            string name = ComfortGroups.Name(group);
            int comfort = NearbyState.GroupComfort(group);
            return comfort > 0 ? $"<color=#7CFC7C>{name} +{comfort}</color>" : name;
        }

        public int GetTagIdByIndex(int index) => _availableTags[index];

        public void UpdateAvailableTags(PieceTable pieceTable)
        {
            var used = new HashSet<Piece.ComfortGroup>();
            foreach (Piece piece in pieceTable.m_availablePieces)
            {
                if (piece.m_comfort > 0)
                    used.Add(piece.m_comfortGroup);
            }

            _availableTags.Clear();
            for (int i = 0; i < _groups.Length; i++)
            {
                // "Each counts" last: the stacking groups are what this tab is about.
                if (_groups[i] != Piece.ComfortGroup.None && used.Contains(_groups[i]))
                    _availableTags.Add(i);
            }
            int none = Array.IndexOf(_groups, Piece.ComfortGroup.None);
            if (used.Contains(Piece.ComfortGroup.None))
                _availableTags.Add(none);
        }

        public void GetAvailablePiecesWithTag(int tagId, PieceTable pieceTable, IList<Piece> resultOut)
        {
            IEnumerable<Piece> pieces = pieceTable.m_availablePieces.Where(p =>
                p.m_comfort > 0 && (tagId < 0 || tagId >= _groups.Length || p.m_comfortGroup == _groups[tagId]));
            // Best first: that is the one that counts.
            foreach (Piece piece in pieces.OrderByDescending(p => p.m_comfort))
                resultOut.Add(piece);
        }
    }

    /// <summary>What is within 10 m of the player, refreshed about once a second.</summary>
    internal static class NearbyState
    {
        private static Dictionary<string, Piece> s_best = new Dictionary<string, Piece>();
        private static HashSet<string> s_names = new HashSet<string>();
        private static float s_nextUpdate;

        /// <summary>True when what's nearby changed since the last update.</summary>
        internal static bool Update(Player player, bool force = false)
        {
            if (!force && Time.time < s_nextUpdate)
                return false;
            s_nextUpdate = Time.time + 1f;

            Dictionary<string, Piece> best = Comfort.BestNearby(player.transform.position);
            HashSet<string> names = Comfort.NamesNearby(player.transform.position);
            bool changed = !names.SetEquals(s_names) ||
                           !best.Values.Select(p => p.m_name).OrderBy(n => n).SequenceEqual(s_best.Values.Select(p => p.m_name).OrderBy(n => n));
            s_best = best;
            s_names = names;
            return changed;
        }

        internal static bool IsHere(Piece piece) => s_names.Contains(piece.m_name);

        internal static bool Counts(Piece piece) =>
            s_best.TryGetValue(Comfort.GroupOf(piece), out Piece best) && best.m_name == piece.m_name;

        internal static Piece Counting(Piece piece) =>
            s_best.TryGetValue(Comfort.GroupOf(piece), out Piece best) ? best : null;

        internal static int GroupComfort(Piece.ComfortGroup group)
        {
            if (group == Piece.ComfortGroup.None)
                return s_best.Values.Where(p => p.m_comfortGroup == Piece.ComfortGroup.None).Sum(p => p.GetComfort());
            return s_best.TryGetValue(group.ToString(), out Piece best) ? best.GetComfort() : 0;
        }
    }

    [HarmonyPatch]
    internal static class ComfortTabPatches
    {
        private const string CheckName = "ComfortHelperCheck";
        private static readonly Color Green = new Color(0.45f, 0.95f, 0.45f);
        private static readonly Color Grey = new Color(0.62f, 0.62f, 0.62f);

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
        private static readonly AccessTools.FieldRef<BuildUi, List<BuildUiPieceButton>> PieceButtonsRef =
            AccessTools.FieldRefAccess<BuildUi, List<BuildUiPieceButton>>("m_pieceButtons");

        // ---- the tab ----

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BuildUi), "Awake")]
        private static void BeforeAwake(BuildUi __instance, out bool __state)
        {
            __state = false;
            if (!ComfortHelperPlugin.ComfortTab.Value)
                return;

            RectTransform container = TabContainerRef(__instance);
            TabHandler tabHandler = TabHandlerRef(__instance);
            // Every tab must map 1:1 to a piece list; bail out if a game update changes that.
            if (container == null || tabHandler == null || container.childCount < 2 ||
                tabHandler.m_tabs.Count != container.childCount)
            {
                ComfortHelperPlugin.Log.LogWarning("Build menu tabs have an unexpected layout; the Comfort tab is disabled.");
                return;
            }

            RectTransform last = (RectTransform)container.GetChild(container.childCount - 1);
            RectTransform previous = (RectTransform)container.GetChild(container.childCount - 2);
            RectTransform tab = UnityEngine.Object.Instantiate(last, container);
            tab.name = "ComfortHelper_ComfortTab";

            // Without a layout group the tabs are placed by hand; continue the row with the same spacing.
            if (container.GetComponent<LayoutGroup>() == null)
                tab.anchoredPosition = last.anchoredPosition + (last.anchoredPosition - previous.anchoredPosition);

            foreach (TMP_Text text in tab.GetComponentsInChildren<TMP_Text>(true))
                text.text = ComfortHelperPlugin.TabName.Value;

            Button button = tab.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            tabHandler.m_tabs.Add(new TabHandler.Tab { m_button = button });
            __state = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BuildUi), "Awake")]
        private static void AfterAwake(BuildUi __instance, bool __state)
        {
            if (__state)
                PieceListsRef(__instance).Add(new ComfortPieceList(ComfortHelperPlugin.TabName.Value));
        }

        private static bool ComfortTabShown(BuildUi buildUi)
        {
            List<IPieceList> lists = PieceListsRef(buildUi);
            int index = CurrentPieceListRef(buildUi);
            return index >= 0 && index < lists.Count && lists[index] is ComfortPieceList;
        }

        // ---- check marks ----

        // Runs whenever the visible pieces change. Buttons are pooled and shared by all tabs, so every button
        // gets its mark set or cleared.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(BuildUi), "UpdatePieceButtons")]
        private static void AfterUpdatePieceButtons(BuildUi __instance)
        {
            Player player = Player.m_localPlayer;
            bool show = player != null && (ComfortTabShown(__instance) || ComfortHelperPlugin.ChecksInAllTabs.Value);
            if (show)
                NearbyState.Update(player, force: true);

            foreach (BuildUiPieceButton button in PieceButtonsRef(__instance))
            {
                Piece piece = button.Piece;
                bool here = show && piece != null && piece.m_comfort > 0 && NearbyState.IsHere(piece);
                Transform check = button.transform.Find(CheckName);
                if (check == null)
                {
                    if (!here)
                        continue;
                    check = CreateCheck(button.transform);
                }
                check.gameObject.SetActive(here);
                if (here)
                {
                    Color color = NearbyState.Counts(piece) ? Green : Grey;
                    foreach (Image bar in check.GetComponentsInChildren<Image>(true))
                        bar.color = color;
                }
            }
        }

        // Keeps the marks right while you walk around or build, with the Comfort tab open.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Hud), "Update")]
        private static void AfterHudUpdate()
        {
            BuildUi buildUi = Hud.instance != null ? Hud.instance.m_buildUi : null;
            Player player = Player.m_localPlayer;
            if (buildUi == null || player == null || !buildUi.isActiveAndEnabled)
                return;
            if (!ComfortTabShown(buildUi) && !ComfortHelperPlugin.ChecksInAllTabs.Value)
                return;
            if (NearbyState.Update(player))
                AvailablePieceCountRef(buildUi) = -1;
        }

        // A check mark from two bars, so it doesn't depend on the font having a check glyph.
        private static Transform CreateCheck(Transform button)
        {
            var go = new GameObject(CheckName, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(button, false);
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-3f, -3f);
            rt.sizeDelta = new Vector2(18f, 16f);
            rt.SetAsLastSibling();

            Bar(rt, new Vector2(5f, 6f), new Vector2(9f, 4f), -45f);
            Bar(rt, new Vector2(11f, 8f), new Vector2(15f, 4f), 50f);
            go.SetActive(false);
            return rt;
        }

        private static void Bar(RectTransform parent, Vector2 center, Vector2 size, float angle)
        {
            var go = new GameObject("bar", typeof(RectTransform), typeof(Image), typeof(Outline));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = center;
            rt.sizeDelta = size;
            rt.localRotation = Quaternion.Euler(0f, 0f, angle);
            go.GetComponent<Image>().raycastTarget = false;
            Outline outline = go.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        // ---- hover text ----

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Hud), "SetupPieceInfo")]
        private static void AfterSetupPieceInfo(Hud __instance, Piece piece)
        {
            Player player = Player.m_localPlayer;
            if (!ComfortHelperPlugin.HoverInfo.Value || piece == null || piece.m_comfort <= 0 || player == null)
                return;

            NearbyState.Update(player);
            Localization loc = Localization.instance;
            string text = $"\n\n<color=#ffb347>Comfort +{piece.m_comfort}</color> ";
            text += piece.m_comfortGroup == Piece.ComfortGroup.None
                ? "(counts once, next to everything else)"
                : $"({ComfortGroups.Name(piece.m_comfortGroup)}: only the best one nearby counts)";

            Piece counting = NearbyState.Counting(piece);
            if (counting == null)
                text += $"\nNothing like it within 10 m: <color=#7CFC7C>+{piece.m_comfort} here</color>.";
            else if (counting.m_name == piece.m_name)
                text += "\n<color=#7CFC7C>Already counts here.</color>";
            else if (piece.m_comfort > counting.GetComfort())
                text += $"\n{loc.Localize(counting.m_name)} counts here now: <color=#7CFC7C>+{piece.m_comfort - counting.GetComfort()} more</color> with this one.";
            else
                text += $"\n{loc.Localize(counting.m_name)} already counts here; this one adds nothing.";
            __instance.m_pieceDescription.text += text;
        }
    }
}
