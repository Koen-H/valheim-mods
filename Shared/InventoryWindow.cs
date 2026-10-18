// Shared by several mods (linked into each csproj): an extra window in the inventory, styled like Trophies.
//
// The window is a copy of the Trophies window, opened by a copy of the Trophies button placed right after it
// in the button row (Texts, Skills, Trophies, Achievements, PvP), which is then spread evenly over the panel.
// The copies are made once per inventory, from the mod's InventoryGui.Awake postfix. Their click handlers are
// wired in the game's UI prefab to OnOpenTrophies / OnCloseTrophies; those are switched off and replaced.
// Entries use the trophy element (icon, name, description) on the same grid as the trophies.
// Opening one of these windows closes the others, also those of other mods (found by their name).

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Koendertjh.Shared
{
    internal enum EntryState
    {
        Done,
        Missing,
        Hidden
    }

    internal class WindowEntry
    {
        public Sprite Icon;
        public string Name;
        public string Description;
        public EntryState State;
    }

    internal sealed class InventoryWindow
    {
        private const string PanelSuffix = "_InventoryWindow";
        private const string HiddenText = "<i>Only Odin knows what is hidden here.</i>";

        private static readonly Color GreyIcon = new Color(0.25f, 0.25f, 0.25f, 0.85f);
        private static readonly Color GreyText = new Color(0.55f, 0.55f, 0.55f, 1f);

        private readonly string _id;
        private readonly string _title;
        private readonly string _tooltip;
        private readonly Func<Sprite> _buttonIcon;
        // Fills the list; returns the window title.
        private readonly Func<List<WindowEntry>, string> _fill;

        private GameObject _panel;
        private RectTransform _listRoot;
        private Scrollbar _scrollbar;
        private TMP_Text _titleText;
        private float _baseHeight;

        public InventoryWindow(string id, string title, string tooltip, Func<Sprite> buttonIcon,
            Func<List<WindowEntry>, string> fill)
        {
            _id = id;
            _title = title;
            _tooltip = tooltip;
            _buttonIcon = buttonIcon;
            _fill = fill;
        }

        public bool IsOpen => _panel != null && _panel.activeSelf;

        // ---- creation ----

        /// <summary>Call from an InventoryGui.Awake postfix. Returns false (and logs why) when it couldn't.</summary>
        public bool Create(InventoryGui gui, BepInEx.Logging.ManualLogSource log)
        {
            try
            {
                CreatePanel(gui);
                CreateButton(gui, log);
                return true;
            }
            catch (Exception e)
            {
                log.LogWarning($"Couldn't add the {_title} window: {e}");
                if (_panel != null)
                    UnityEngine.Object.Destroy(_panel);
                _panel = null;
                return false;
            }
        }

        private void CreatePanel(InventoryGui gui)
        {
            GameObject trophies = gui.m_trophiesPanel;
            string rootPath = PathFrom(trophies.transform, gui.m_trophieListRoot);
            string scrollPath = PathFrom(trophies.transform, gui.m_trophyListScroll.transform);

            _panel = UnityEngine.Object.Instantiate(trophies, trophies.transform.parent);
            _panel.name = _id + PanelSuffix;
            _panel.SetActive(false);

            _listRoot = (RectTransform)_panel.transform.Find(rootPath);
            _scrollbar = _panel.transform.Find(scrollPath)?.GetComponent<Scrollbar>();
            _baseHeight = gui.m_trophieListRoot.rect.height;
            // The copied list holds copies of trophy entries.
            for (int i = _listRoot.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(_listRoot.GetChild(i).gameObject);

            // Its own gamepad group would fight with the inventory's.
            foreach (UIGroupHandler group in _panel.GetComponentsInChildren<UIGroupHandler>(true))
                UnityEngine.Object.Destroy(group);

            string trophiesTitle = Localization.instance.Localize("$inventory_trophies");
            foreach (TMP_Text text in _panel.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.transform.IsChildOf(_listRoot) ||
                    (text.text != trophiesTitle && text.text != "$inventory_trophies"))
                    continue;
                // The Localize component would put "Trophies" back whenever the window opens.
                foreach (Component localize in text.GetComponents<Component>())
                {
                    if (localize != null && localize.GetType().Name == "Localize")
                        UnityEngine.Object.Destroy(localize);
                }
                text.text = _title;
                _titleText = text;
            }

            foreach (Button button in _panel.GetComponentsInChildren<Button>(true))
            {
                if (Rewire(button.onClick, "OnCloseTrophies"))
                    button.onClick.AddListener(Close);
            }
        }

        private void CreateButton(InventoryGui gui, BepInEx.Logging.ManualLogSource log)
        {
            Button trophies = null;
            foreach (Button button in gui.GetComponentsInChildren<Button>(true))
            {
                // The original, not a copy in another window or another mod's button.
                if (HasPersistent(button.onClick, "OnOpenTrophies") && !button.name.EndsWith("_Button") &&
                    !button.transform.IsChildOf(_panel.transform))
                {
                    trophies = button;
                    break;
                }
            }
            if (trophies == null)
            {
                log.LogWarning($"Couldn't find the Trophies button; the {_title} window has no button.");
                return;
            }

            GameObject copy = UnityEngine.Object.Instantiate(trophies.gameObject, trophies.transform.parent);
            copy.name = _id + "_Button";
            Button copyButton = copy.GetComponent<Button>();
            Rewire(copyButton.onClick, "OnOpenTrophies");
            copyButton.onClick.AddListener(() => Open(gui));

            copy.transform.SetSiblingIndex(trophies.transform.GetSiblingIndex() + 1);
            if (copy.transform.parent.GetComponent<LayoutGroup>() == null)
                SpreadRow((RectTransform)trophies.transform, (RectTransform)copy.transform);

            UITooltip tooltip = copy.GetComponent<UITooltip>();
            if (tooltip != null)
                tooltip.Set(_title, _tooltip);

            // The row's icons are light shapes tinted gold by their Image colour; a white shape gets the same look.
            Image picture = copy.transform.Find("Image")?.GetComponent<Image>();
            Sprite icon = _buttonIcon?.Invoke();
            if (picture != null && icon != null)
            {
                picture.sprite = icon;
                picture.preserveAspect = true;
            }
        }

        // The row is placed by hand, 100 apart: the new button goes right after Trophies and the row is spread
        // evenly, squeezed if it no longer fits the panel. Row buttons have the size and height of Trophies.
        private static void SpreadRow(RectTransform trophies, RectTransform added)
        {
            var row = new List<RectTransform>();
            foreach (Transform child in trophies.parent)
            {
                if (child is RectTransform rt && child != added &&
                    Mathf.Abs(rt.anchoredPosition.y - trophies.anchoredPosition.y) < 1f &&
                    (rt.sizeDelta - trophies.sizeDelta).sqrMagnitude < 1f)
                    row.Add(rt);
            }
            row.Sort((a, b) => a.anchoredPosition.x.CompareTo(b.anchoredPosition.x));
            row.Insert(row.IndexOf(trophies) + 1, added);
            if (row.Count < 3)
                return;

            float first = row[0].anchoredPosition.x;
            float last = row[row.Count - 1].anchoredPosition.x;
            float spacing = Mathf.Max(last - first, 0f) / (row.Count - 2);
            float width = ((RectTransform)trophies.parent).rect.width;
            if (width > 0f)
                spacing = Mathf.Min(spacing, (width - trophies.sizeDelta.x - 8f) / (row.Count - 1));

            float x = (first + last) / 2f - spacing * (row.Count - 1) / 2f;
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

        private void Open(InventoryGui gui)
        {
            if (_panel == null)
                return;
            gui.m_trophiesPanel.SetActive(false);
            gui.m_achievementsPanel.gameObject.SetActive(false);
            foreach (Transform sibling in _panel.transform.parent)
            {
                if (sibling.gameObject != _panel && sibling.name.EndsWith(PanelSuffix))
                    sibling.gameObject.SetActive(false);
            }
            _panel.SetActive(true);
            Fill(gui);
        }

        public void Close()
        {
            if (_panel != null)
                _panel.SetActive(false);
        }

        /// <summary>
        /// Call from an InventoryGui.Update prefix and skip the original when it returns true: Escape or B closes
        /// the window first, like the Trophies window, and the inventory does nothing else that frame.
        /// </summary>
        public bool HandleEscape()
        {
            if (!IsOpen || !(ZInput.GetButtonDown("JoyButtonB") || ZInput.GetKeyDown(KeyCode.Escape)))
                return false;
            Close();
            return true;
        }

        // ---- list ----

        private void Fill(InventoryGui gui)
        {
            if (Player.m_localPlayer == null || _listRoot == null)
                return;

            for (int i = _listRoot.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(_listRoot.GetChild(i).gameObject);

            var entries = new List<WindowEntry>();
            string title = _fill(entries);

            float space = gui.m_trophieListSpace;
            int columns = Columns();
            for (int index = 0; index < entries.Count; index++)
            {
                WindowEntry entry = entries[index];
                GameObject element = UnityEngine.Object.Instantiate(gui.m_trophieElementPrefab, _listRoot);
                element.SetActive(true);
                var rt = (RectTransform)element.transform;
                rt.anchoredPosition = new Vector2(index % columns * space, index / columns * -space);

                Image icon = rt.Find("icon_bkg/icon")?.GetComponent<Image>();
                TMP_Text name = rt.Find("name")?.GetComponent<TMP_Text>();
                TMP_Text description = rt.Find("description")?.GetComponent<TMP_Text>();

                switch (entry.State)
                {
                    case EntryState.Done:
                        SetIcon(icon, name, entry.Icon, Color.white);
                        if (name != null) name.text = entry.Name;
                        if (description != null) description.text = entry.Description;
                        break;
                    case EntryState.Missing:
                        SetIcon(icon, name, entry.Icon, GreyIcon);
                        SetGrey(name, entry.Name);
                        SetGrey(description, entry.Description);
                        break;
                    default:
                        SetIcon(icon, name, null, GreyIcon);
                        SetGrey(name, "??????");
                        SetGrey(description, HiddenText);
                        break;
                }
            }

            if (_titleText != null)
                _titleText.text = title ?? _title;

            int rows = (entries.Count + columns - 1) / columns;
            _listRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(_baseHeight, rows * space));
            if (_scrollbar != null)
                _scrollbar.value = 1f;
        }

        private static void SetGrey(TMP_Text text, string value)
        {
            if (text == null)
                return;
            text.text = value;
            text.color = GreyText;
        }

        // No sprite: a "?" in the font of the name instead.
        private static void SetIcon(Image icon, TMP_Text styleSource, Sprite sprite, Color color)
        {
            if (icon == null)
                return;
            if (sprite != null)
            {
                icon.sprite = sprite;
                icon.color = color;
                return;
            }
            if (styleSource == null)
                return;
            icon.enabled = false;

            GameObject mark = UnityEngine.Object.Instantiate(styleSource.gameObject, icon.transform);
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
    }
}
