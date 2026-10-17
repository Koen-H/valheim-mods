using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UnlockAwareness
{
    /// <summary>The "new" marker attached to a recipe row or build piece button.</summary>
    internal class UnseenBadge : MonoBehaviour
    {
        public string Key;

        public void Show(string key)
        {
            Key = key;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            Key = null;
            gameObject.SetActive(false);
        }

        /// <summary>Marks the unlock this badge belongs to as seen. Returns false if it was not showing.</summary>
        public bool Consume()
        {
            if (!gameObject.activeSelf || Key == null) return false;
            UnseenStore.MarkSeen(Key);
            Hide();
            return true;
        }
    }

    /// <summary>A square "Mark all as seen" icon button with a tooltip.</summary>
    internal class MarkAllButton
    {
        private const string TooltipText = "Mark all as seen";
        private static Color EyeTint => UiFactory.EyeColor;

        public Button Button;
        public Image Icon;
        public GameObject Glow;
        public UITooltip Tooltip;

        public void Refresh(int count)
        {
            if (Button == null) return;
            Button.interactable = count > 0;
            if (Icon != null) Icon.color = count > 0 ? EyeTint : new Color(EyeTint.r, EyeTint.g, EyeTint.b, 0.35f);
            if (Glow != null) Glow.SetActive(count > 0);
            if (Tooltip != null) Tooltip.m_text = count > 0 ? $"{TooltipText} ({count})" : TooltipText;
        }

        public void SetTooltip(UITooltip tooltip)
        {
            Tooltip = tooltip;
            Tooltip.m_topic = "";
            Tooltip.m_text = TooltipText;
        }
    }

    internal static class UiFactory
    {
        private const string BadgeName = "UnlockAwareness_Badge";
        private const string UnknownBadgeName = "UnlockAwareness_Unknown";

        // Sprites from Valheim's own UI atlas.
        private const string BadgeSpriteName = "exclamation_mark";
        // Warm off-white, close to the game's own icon color.
        internal static readonly Color EyeColor = new Color(0.93f, 0.88f, 0.78f, 1f);

        private static readonly AccessTools.FieldRef<BuildUiPieceButton, Image> PieceIconRef =
            AccessTools.FieldRefAccess<BuildUiPieceButton, Image>("m_icon");
        private static readonly AccessTools.FieldRef<BuildUiPieceButton, Image> PieceUpgradeArrowRef =
            AccessTools.FieldRefAccess<BuildUiPieceButton, Image>("m_upgradeArrow");
        private static readonly AccessTools.FieldRef<BuildUiPieceButton, Image> PieceFavoriteStarRef =
            AccessTools.FieldRefAccess<BuildUiPieceButton, Image>("m_favoriteStar");

        private static readonly Dictionary<string, Sprite> s_sprites = new Dictionary<string, Sprite>();
        private static Sprite s_eyeSprite;

        /// <summary>Returns the badge under <paramref name="parent"/>, creating it if needed.</summary>
        public static UnseenBadge GetOrCreateBadge(Transform parent, float height, Vector2 anchor, Vector2 offset)
        {
            Transform existing = parent.Find(BadgeName);
            if (existing != null) return existing.GetComponent<UnseenBadge>();

            var go = new GameObject(BadgeName, typeof(RectTransform), typeof(Image), typeof(UnseenBadge));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = offset;
            rt.SetAsLastSibling();

            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = UnlockAwarenessPlugin.BadgeColor.Value;
            Sprite sprite = FindGameSprite(BadgeSpriteName);
            if (sprite != null)
            {
                image.sprite = sprite;
                rt.sizeDelta = new Vector2(height * sprite.rect.width / sprite.rect.height, height);
            }
            else
            {
                image.sprite = CreateDotSprite();
                rt.sizeDelta = new Vector2(height * 0.8f, height * 0.8f);
            }

            // Dark outline keeps the marker readable on bright icons.
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            var badge = go.GetComponent<UnseenBadge>();
            badge.Hide();
            return badge;
        }

        /// <summary>
        /// Returns the "?" marker under <paramref name="parent"/>, creating it if needed. Uses the font of
        /// <paramref name="styleSource"/> (the slot's amount text) so it matches the game's own lettering.
        /// </summary>
        public static GameObject GetOrCreateUnknownBadge(Transform parent, TMP_Text styleSource, float fontSize, Vector2 anchor, Vector2 offset)
        {
            Transform existing = parent.Find(UnknownBadgeName);
            if (existing != null) return existing.gameObject;

            var go = new GameObject(UnknownBadgeName, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(fontSize, fontSize);
            rt.SetAsLastSibling();

            var text = go.GetComponent<TextMeshProUGUI>();
            if (styleSource != null)
            {
                text.font = styleSource.font;
                text.fontSharedMaterial = styleSource.fontSharedMaterial;
            }
            text.text = "?";
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.color = UnlockAwarenessPlugin.BadgeColor.Value;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            // Dark edge like the "!" outline, so it stays readable on bright icons.
            text.outlineColor = new Color(0f, 0f, 0f, 0.9f);
            text.outlineWidth = 0.25f;

            go.SetActive(false);
            return go;
        }

        /// <summary>
        /// Clones the crafting panel's repair button (and its wooden backing) and places the copy right below it.
        /// </summary>
        public static MarkAllButton CreateCraftingMarkAllButton(InventoryGui gui, Action onClick)
        {
            Button repair = gui.m_repairButton;
            if (repair == null) return null;

            var repairRt = (RectTransform)repair.transform;
            var backing = gui.m_crafting.Find("RepairSimple") as RectTransform;
            float step = backing != null ? backing.sizeDelta.y : repairRt.sizeDelta.y + 16f;

            if (backing != null)
            {
                GameObject backingCopy = Object.Instantiate(backing.gameObject, backing.parent, false);
                backingCopy.name = "UnlockAwareness_MarkAllBacking";
                var copyRt = (RectTransform)backingCopy.transform;
                copyRt.anchoredPosition = backing.anchoredPosition - new Vector2(0f, step);
                copyRt.SetSiblingIndex(backing.GetSiblingIndex() + 1);
                backingCopy.SetActive(true);
            }

            GameObject go = Object.Instantiate(repair.gameObject, repair.transform.parent, false);
            go.name = "UnlockAwareness_MarkAllCrafting";
            ((RectTransform)go.transform).anchoredPosition = repairRt.anchoredPosition - new Vector2(0f, step);

            // The repair button has a gamepad shortcut; ours should not trigger on that key.
            foreach (UIGamePad pad in go.GetComponentsInChildren<UIGamePad>(true))
                Object.DestroyImmediate(pad);
            Transform hint = go.transform.Find("gamepad_hint");
            if (hint != null) Object.Destroy(hint.gameObject);

            Image icon = go.transform.Find("Image")?.GetComponent<Image>();
            SetEyeIcon(icon, 46f);

            var result = new MarkAllButton
            {
                Button = go.GetComponent<Button>(),
                Icon = icon,
                Glow = go.transform.Find("Glow")?.gameObject,
            };
            UITooltip tooltip = go.GetComponent<UITooltip>() ?? go.AddComponent<UITooltip>();
            if (tooltip.m_tooltipPrefab == null) tooltip.m_tooltipPrefab = repair.GetComponent<UITooltip>()?.m_tooltipPrefab;
            result.SetTooltip(tooltip);

            result.Button.onClick = new Button.ButtonClickedEvent();
            result.Button.onClick.AddListener(() => onClick());
            go.SetActive(true);
            return result;
        }

        /// <summary>
        /// Creates a square button in the build menu's top bar (next to the search field and repair tool),
        /// styled like the piece buttons.
        /// </summary>
        public static MarkAllButton CreateBuildMarkAllButton(GameObject pieceButtonPrefab, Transform topBar, Action onClick)
        {
            if (pieceButtonPrefab == null || topBar == null) return null;

            // Instantiate under an inactive holder so the piece button's own Awake never runs.
            var holder = new GameObject("UnlockAwareness_Holder");
            holder.SetActive(false);
            GameObject go = Object.Instantiate(pieceButtonPrefab, holder.transform, false);
            go.name = "UnlockAwareness_MarkAllBuild";
            var pieceButton = go.GetComponent<BuildUiPieceButton>();
            Image icon = PieceIconRef(pieceButton);
            // Hide the piece button's extras; we only want the square with an icon.
            PieceUpgradeArrowRef(pieceButton)?.gameObject.SetActive(false);
            PieceFavoriteStarRef(pieceButton)?.gameObject.SetActive(false);
            Object.DestroyImmediate(pieceButton);
            Object.DestroyImmediate(go.GetComponent<UIInputHandler>());

            go.transform.SetParent(topBar, false);
            Object.Destroy(holder);

            var rt = (RectTransform)go.transform;
            float size = Mathf.Max(32f, ((RectTransform)topBar).rect.height);
            rt.sizeDelta = new Vector2(size, size);
            var layout = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            layout.minWidth = layout.preferredWidth = size;
            layout.minHeight = layout.preferredHeight = size;
            layout.flexibleWidth = layout.flexibleHeight = 0f;
            go.transform.SetAsLastSibling();

            SetEyeIcon(icon, size * 0.75f);

            var result = new MarkAllButton { Button = go.GetComponent<Button>(), Icon = icon };
            // Keep the square looking like the other piece buttons when disabled; the dimmed eye shows the state.
            ColorBlock colors = result.Button.colors;
            colors.disabledColor = colors.normalColor;
            result.Button.colors = colors;
            UITooltip tooltip = go.AddComponent<UITooltip>();
            tooltip.m_tooltipPrefab = InventoryGui.instance?.m_repairButton?.GetComponent<UITooltip>()?.m_tooltipPrefab;
            result.SetTooltip(tooltip);

            result.Button.onClick = new Button.ButtonClickedEvent();
            result.Button.onClick.AddListener(() => onClick());
            go.SetActive(true);
            return result;
        }

        private static void SetEyeIcon(Image image, float width)
        {
            if (image == null) return;
            Sprite eye = s_eyeSprite != null ? s_eyeSprite : (s_eyeSprite = CreateEyeSprite());
            image.sprite = eye;
            image.preserveAspect = true;
            image.color = EyeColor;
            RectTransform rt = image.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            image.rectTransform.sizeDelta = new Vector2(width, width * eye.rect.height / eye.rect.width);
        }

        private static Sprite FindGameSprite(string name)
        {
            // A miss is cached too: scanning every loaded sprite for each badge would stutter the UI.
            if (s_sprites.TryGetValue(name, out Sprite cached) && (cached || ReferenceEquals(cached, null))) return cached;
            foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
            {
                if (sprite.name == name)
                    return s_sprites[name] = sprite;
            }
            UnlockAwarenessPlugin.Log.LogWarning($"Sprite '{name}' not found, using fallback");
            return s_sprites[name] = null;
        }

        private static Sprite CreateDotSprite() => CreateSprite(32, 32, (x, y) =>
        {
            float dist = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f));
            return Mathf.Clamp01(15f - dist + 0.5f);
        });

        /// <summary>
        /// High-resolution eye (almond outline, iris, highlight) drawn with signed distances so the edges are
        /// anti-aliased. The game's own sneak eye is only 32x15 and looks blurry when scaled up.
        /// </summary>
        private static Sprite CreateEyeSprite()
        {
            const int width = 256, height = 128;
            const float halfWidth = 118f, halfHeight = 54f, stroke = 9f, iris = 33f, gap = 7f;
            // The almond is the overlap of two circles.
            float radius = (halfWidth * halfWidth + halfHeight * halfHeight) / (2f * halfHeight);
            float offset = radius - halfHeight;
            float cx = width / 2f - 0.5f, cy = height / 2f - 0.5f;

            return CreateSprite(width, height, (x, y) =>
            {
                float px = x - cx, py = y - cy;
                float lens = Mathf.Max(Hypot(px, py + offset) - radius, Hypot(px, py - offset) - radius);
                float outline = Mathf.Abs(lens + stroke / 2f) - stroke / 2f;
                float irisDisc = Mathf.Max(Hypot(px, py) - iris, lens + stroke + gap);
                float highlight = Hypot(px - 11f, py - 11f) - 6.5f;
                irisDisc = Mathf.Max(irisDisc, -highlight);
                return Mathf.Max(Coverage(outline), Coverage(irisDisc));
            }, mipmaps: true);
        }

        private static float Hypot(float x, float y) => Mathf.Sqrt(x * x + y * y);

        private static float Coverage(float signedDistance) => Mathf.Clamp01(0.5f - signedDistance);

        private static Sprite CreateSprite(int width, int height, Func<int, int, float> alpha, bool mipmaps = false)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, mipmaps)
            {
                filterMode = mipmaps ? FilterMode.Trilinear : FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha(x, y)));
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }
    }
}
