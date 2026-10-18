// The Materials window: a copy of the Trophies window, opened by a copy of the Trophies button placed next to it.
// The copies are made once per inventory (InventoryGui.Awake). Their click handlers are wired in the game's UI
// prefab to OnOpenTrophies / OnCloseTrophies; those are switched off and replaced by ours.
// Entries use the trophy element (icon, name, description) on the same grid as the trophies.

using System.Collections.Generic;
using HarmonyLib;
using Koendertjh.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MaterialTracker
{
    [HarmonyPatch(typeof(InventoryGui))]
    internal static class MaterialsPanel
    {
        private const string Title = "Materials";

        private static readonly Color GreyIcon = new Color(0.25f, 0.25f, 0.25f, 0.85f);
        private static readonly Color GreyText = new Color(0.55f, 0.55f, 0.55f, 1f);

        private static GameObject s_panel;
        private static RectTransform s_listRoot;
        private static Scrollbar s_scrollbar;
        private static TMP_Text s_title;
        private static float s_baseHeight;

        // ---- creation ----

        [HarmonyPostfix]
        [HarmonyPatch("Awake")]
        private static void Create(InventoryGui __instance)
        {
            try
            {
                CreatePanel(__instance);
                CreateButton(__instance);
            }
            catch (System.Exception e)
            {
                MaterialTrackerPlugin.Log.LogWarning($"Couldn't add the Materials window: {e}");
                if (s_panel != null)
                    Object.Destroy(s_panel);
                s_panel = null;
            }
        }

        private static void CreatePanel(InventoryGui gui)
        {
            GameObject trophies = gui.m_trophiesPanel;
            string rootPath = PathFrom(trophies.transform, gui.m_trophieListRoot);
            string scrollPath = PathFrom(trophies.transform, gui.m_trophyListScroll.transform);

            s_panel = Object.Instantiate(trophies, trophies.transform.parent);
            s_panel.name = "MaterialTrackerPanel";
            s_panel.SetActive(false);

            s_listRoot = (RectTransform)s_panel.transform.Find(rootPath);
            s_scrollbar = s_panel.transform.Find(scrollPath)?.GetComponent<Scrollbar>();
            s_baseHeight = gui.m_trophieListRoot.rect.height;

            // Its own gamepad group would fight with the inventory's.
            foreach (UIGroupHandler group in s_panel.GetComponentsInChildren<UIGroupHandler>(true))
                Object.Destroy(group);

            string trophiesTitle = Localization.instance.Localize("$inventory_trophies");
            foreach (TMP_Text text in s_panel.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.transform.IsChildOf(s_listRoot) ||
                    (text.text != trophiesTitle && text.text != "$inventory_trophies"))
                    continue;
                // The Localize component would put "Trophies" back whenever the window opens.
                foreach (Component localize in text.GetComponents<Component>())
                {
                    if (localize != null && localize.GetType().Name == "Localize")
                        Object.Destroy(localize);
                }
                text.text = Title;
                s_title = text;
            }

            foreach (Button button in s_panel.GetComponentsInChildren<Button>(true))
            {
                if (Rewire(button.onClick, "OnCloseTrophies"))
                    button.onClick.AddListener(Close);
            }
        }

        private static void CreateButton(InventoryGui gui)
        {
            Button trophies = null;
            foreach (Button button in gui.GetComponentsInChildren<Button>(true))
            {
                if (HasPersistent(button.onClick, "OnOpenTrophies") && !button.transform.IsChildOf(s_panel.transform))
                {
                    trophies = button;
                    break;
                }
            }
            if (trophies == null)
            {
                MaterialTrackerPlugin.Log.LogWarning("Couldn't find the Trophies button; the Materials window has no button.");
                return;
            }

            GameObject copy = Object.Instantiate(trophies.gameObject, trophies.transform.parent);
            copy.name = "MaterialTrackerButton";
            Button button2 = copy.GetComponent<Button>();
            Rewire(button2.onClick, "OnOpenTrophies");
            button2.onClick.AddListener(() => Open(gui));

            // The row (Texts, Skills, Trophies, Achievements, PvP) is placed by hand, 100 apart. Put Materials
            // right after Trophies and spread the row evenly over the panel.
            copy.transform.SetSiblingIndex(trophies.transform.GetSiblingIndex() + 1);
            if (copy.transform.parent.GetComponent<LayoutGroup>() == null)
                SpreadRow((RectTransform)trophies.transform, (RectTransform)copy.transform);

            UITooltip tooltip = copy.GetComponent<UITooltip>();
            if (tooltip != null)
                tooltip.Set(Title, "Every crafting material, and where to find it");

            // The row's icons are light shapes tinted gold by their Image colour; a white bar shape gets the same look.
            Image picture = copy.transform.Find("Image")?.GetComponent<Image>();
            if (picture != null)
            {
                picture.sprite = BarIcon.Get();
                picture.preserveAspect = true;
            }
        }

        // Every button of the row: same height and size as the Trophies button.
        private static void SpreadRow(RectTransform trophies, RectTransform materials)
        {
            var row = new List<RectTransform>();
            foreach (Transform child in trophies.parent)
            {
                if (child is RectTransform rt && child != materials &&
                    Mathf.Abs(rt.anchoredPosition.y - trophies.anchoredPosition.y) < 1f &&
                    (rt.sizeDelta - trophies.sizeDelta).sqrMagnitude < 1f)
                    row.Add(rt);
            }
            row.Sort((a, b) => a.anchoredPosition.x.CompareTo(b.anchoredPosition.x));
            row.Insert(row.IndexOf(trophies) + 1, materials);
            if (row.Count < 2)
                return;

            float first = row[0].anchoredPosition.x;
            float last = row[row.Count - 1].anchoredPosition.x;
            float spacing = Mathf.Max(last - first, 0f) / (row.Count - 2);
            float width = ((RectTransform)trophies.parent).rect.width;
            // Keep the old spacing if it fits, otherwise squeeze the row into the panel.
            if (width > 0f)
                spacing = Mathf.Min(spacing, (width - trophies.sizeDelta.x - 8f) / (row.Count - 1));

            float center = (first + last) / 2f;
            float x = center - spacing * (row.Count - 1) / 2f;
            foreach (RectTransform button in row)
            {
                button.anchoredPosition = new Vector2(x, trophies.anchoredPosition.y);
                x += spacing;
            }
        }

        private static string PathFrom(Transform root, Transform target)
        {
            var parts = new List<string>();
            for (Transform t = target; t != null && t != root; t = t.parent)
                parts.Insert(0, t.name);
            return string.Join("/", parts);
        }

        private static bool HasPersistent(UnityEventBase onClick, string method)
        {
            for (int i = 0; i < onClick.GetPersistentEventCount(); i++)
            {
                if (onClick.GetPersistentMethodName(i) == method)
                    return true;
            }
            return false;
        }

        /// <summary>Switches off the prefab's handler; true when it had one.</summary>
        private static bool Rewire(UnityEventBase onClick, string method)
        {
            bool found = false;
            for (int i = 0; i < onClick.GetPersistentEventCount(); i++)
            {
                if (onClick.GetPersistentMethodName(i) != method)
                    continue;
                onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
                found = true;
            }
            return found;
        }

        // ---- open / close ----

        private static void Open(InventoryGui gui)
        {
            if (s_panel == null)
                return;
            gui.m_trophiesPanel.SetActive(false);
            gui.m_achievementsPanel.gameObject.SetActive(false);
            s_panel.SetActive(true);
            Fill(gui);
        }

        private static void Close()
        {
            if (s_panel != null)
                s_panel.SetActive(false);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(InventoryGui.Hide))]
        private static void AfterHide() => Close();

        [HarmonyPostfix]
        [HarmonyPatch(nameof(InventoryGui.OnOpenTrophies))]
        private static void AfterOpenTrophies() => Close();

        // Escape or B closes the window first, like the Trophies window; that frame the inventory does nothing else.
        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        private static bool BeforeUpdate()
        {
            if (s_panel == null || !s_panel.activeSelf ||
                !(ZInput.GetButtonDown("JoyButtonB") || ZInput.GetKeyDown(KeyCode.Escape)))
                return true;
            Close();
            return false;
        }

        // ---- list ----

        private static void Fill(InventoryGui gui)
        {
            Player player = Player.m_localPlayer;
            if (player == null || s_listRoot == null)
                return;

            for (int i = s_listRoot.childCount - 1; i >= 0; i--)
                Object.Destroy(s_listRoot.GetChild(i).gameObject);

            float space = gui.m_trophieListSpace;
            int columns = Columns();
            int index = 0;
            int found = 0;
            List<MaterialInfo> all = Materials.All;

            foreach (MaterialInfo material in all)
            {
                bool have = player.IsMaterialKnown(material.Name);
                if (have)
                    found++;
                if (have && !MaterialTrackerPlugin.ShowFound.Value)
                    continue;

                GameObject element = Object.Instantiate(gui.m_trophieElementPrefab, s_listRoot);
                element.SetActive(true);
                var rt = (RectTransform)element.transform;
                rt.anchoredPosition = new Vector2(index % columns * space, index / columns * -space);
                index++;

                Image icon = rt.Find("icon_bkg/icon")?.GetComponent<Image>();
                TMP_Text name = rt.Find("name")?.GetComponent<TMP_Text>();
                TMP_Text description = rt.Find("description")?.GetComponent<TMP_Text>();
                string hint = MaterialTrackerPlugin.ShowHints.Value ? material.Hint : "";
                bool revealed = have || MaterialTrackerPlugin.ShowAll.Value ||
                                BossProgress.IsDefeated(StageTable.RevealedBy(material.Stage));

                if (have)
                {
                    if (icon != null) icon.sprite = material.Item.m_itemData.GetIcon();
                    if (name != null) name.text = Localization.instance.Localize(material.Name);
                    if (description != null) description.text = hint;
                }
                else if (revealed)
                {
                    if (icon != null)
                    {
                        icon.sprite = material.Item.m_itemData.GetIcon();
                        icon.color = GreyIcon;
                    }
                    SetText(name, Localization.instance.Localize(material.Name));
                    SetText(description, (hint.Length > 0 ? hint + "\n" : "") + "<i>Not found yet</i>");
                }
                else
                {
                    ShowQuestionMark(icon, name);
                    SetText(name, "??????");
                    SetText(description, "<i>Only Odin knows what is hidden here.</i>");
                }
            }

            if (s_title != null)
                s_title.text = $"{Title} {found} / {all.Count}";

            int rows = (index + columns - 1) / columns;
            s_listRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(s_baseHeight, rows * space));
            if (s_scrollbar != null)
                s_scrollbar.value = 1f;
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text == null)
                return;
            text.text = value;
            text.color = GreyText;
        }

        // Same number of columns as the trophy grid.
        private static int Columns()
        {
            int columns = 1;
            foreach (GameObject prefab in ObjectDB.instance.m_items)
            {
                ItemDrop item = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
                if (item != null && item.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophy)
                    columns = Mathf.Max(columns, item.m_itemData.m_shared.m_trophyPos.x + 1);
            }
            return columns;
        }

        // A "?" in place of the icon, in the font of the name.
        private static void ShowQuestionMark(Image icon, TMP_Text styleSource)
        {
            if (icon == null || styleSource == null)
                return;
            icon.enabled = false;

            GameObject mark = Object.Instantiate(styleSource.gameObject, icon.transform);
            var rt = (RectTransform)mark.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;

            TMP_Text text = mark.GetComponent<TMP_Text>();
            float height = ((RectTransform)icon.transform).rect.height;
            text.enableAutoSizing = false;
            text.fontSize = height > 0f ? height * 0.8f : 48f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = GreyText;
            text.text = "?";
        }
    }
}
