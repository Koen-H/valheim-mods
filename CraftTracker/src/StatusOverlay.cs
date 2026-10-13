using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CraftTracker
{
    /// <summary>
    /// "N not discovered yet" bar along the bottom of the recipe list, plus the centered congratulations
    /// message for when nothing is left to craft. The recipe list reserves <see cref="BarHeight"/> at its
    /// end while the bar is shown (see the UpdateRecipeList patch), so the bar never hides a recipe.
    /// </summary>
    internal sealed class StatusOverlay
    {
        public const float BarHeight = 30f;

        private readonly GameObject _bar;
        private readonly TMP_Text _barText;
        private readonly TMP_Text _congrats;

        public StatusOverlay(RectTransform parent, TMP_Text styleSource)
        {
            _bar = new GameObject("CraftTracker_Undiscovered", typeof(RectTransform), typeof(Image));
            var barRect = (RectTransform)_bar.transform;
            barRect.SetParent(parent, false);
            barRect.anchorMin = new Vector2(0f, 0f);
            barRect.anchorMax = new Vector2(1f, 0f);
            barRect.pivot = new Vector2(0.5f, 0f);
            barRect.sizeDelta = new Vector2(0f, BarHeight);
            barRect.anchoredPosition = Vector2.zero;
            Image background = _bar.GetComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.45f);
            background.raycastTarget = false;

            _barText = CreateText(barRect, "Text", styleSource, 18f, new Color(0.85f, 0.85f, 0.85f, 1f));
            _barText.fontStyle = FontStyles.Italic;
            var barTextRect = (RectTransform)_barText.transform;
            barTextRect.offsetMin = new Vector2(10f, 0f);
            barTextRect.offsetMax = new Vector2(-10f, 0f);

            _congrats = CreateText(parent, "CraftTracker_Congrats", styleSource, 24f, new Color(1f, 0.83f, 0.35f, 1f));
            var congratsRect = (RectTransform)_congrats.transform;
            congratsRect.offsetMin = new Vector2(20f, BarHeight);
            congratsRect.offsetMax = new Vector2(-20f, 0f);
            _congrats.textWrappingMode = TextWrappingModes.Normal;

            _bar.SetActive(false);
            _congrats.gameObject.SetActive(false);
        }

        public bool BarVisible => _bar.activeSelf;

        /// <summary>Shows "N not discovered yet", or hides the bar when <paramref name="count"/> is 0.</summary>
        public void ShowUndiscovered(int count)
        {
            if (count > 0)
                _barText.text = Format(count);
            _bar.SetActive(count > 0);
            _bar.transform.SetAsLastSibling();
        }

        /// <summary>Shows the centered message, or hides it when <paramref name="message"/> is null.</summary>
        public void ShowCongrats(string message)
        {
            if (message != null)
                _congrats.text = message;
            _congrats.gameObject.SetActive(message != null);
            _congrats.transform.SetAsLastSibling();
        }

        public void Hide()
        {
            ShowUndiscovered(0);
            ShowCongrats(null);
        }

        private static TMP_Text CreateText(RectTransform parent, string name, TMP_Text styleSource, float size, Color color)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = (RectTransform)textObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            TMP_Text text = textObject.GetComponent<TextMeshProUGUI>();
            if (styleSource != null)
            {
                text.font = styleSource.font;
                text.fontSharedMaterial = styleSource.fontSharedMaterial;
            }
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private static string Format(int count)
        {
            try { return string.Format(CraftTrackerPlugin.UndiscoveredText.Value, count); }
            catch (System.FormatException) { return count + " not discovered yet"; }
        }
    }
}
