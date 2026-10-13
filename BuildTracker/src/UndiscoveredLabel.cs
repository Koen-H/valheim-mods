using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BuildTracker
{
    /// <summary>
    /// "N not discovered yet" bar along the bottom of the build menu's piece grid. While shown, the
    /// scroll viewport is shortened by the bar's height so it never covers piece buttons.
    /// Also owns the centered congratulations message for when nothing is left to build.
    /// </summary>
    internal sealed class UndiscoveredLabel
    {
        private const float Height = 30f;

        private readonly GameObject _root;
        private readonly TMP_Text _text;
        private readonly TMP_Text _congrats;
        private readonly RectTransform _viewport;
        private readonly float _viewportBottom;

        public UndiscoveredLabel(RectTransform pieceView, ScrollRect scrollRect, TMP_Text styleSource)
        {
            _root = new GameObject("BuildTracker_Undiscovered", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)_root.transform;
            rect.SetParent(pieceView, false);
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(0f, Height);
            rect.anchoredPosition = Vector2.zero;
            Image background = _root.GetComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.45f);
            background.raycastTarget = false;

            var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            var textRect = (RectTransform)textObject.transform;
            textRect.SetParent(rect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 0f);
            textRect.offsetMax = new Vector2(-10f, 0f);
            _text = textObject.GetComponent<TextMeshProUGUI>();
            if (styleSource != null)
            {
                _text.font = styleSource.font;
                _text.fontSharedMaterial = styleSource.fontSharedMaterial;
            }
            _text.fontSize = 18f;
            _text.fontStyle = FontStyles.Italic;
            _text.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            _text.alignment = TextAlignmentOptions.Center;
            _text.raycastTarget = false;

            var congratsObject = new GameObject("BuildTracker_Congrats", typeof(RectTransform), typeof(TextMeshProUGUI));
            var congratsRect = (RectTransform)congratsObject.transform;
            congratsRect.SetParent(pieceView, false);
            congratsRect.anchorMin = Vector2.zero;
            congratsRect.anchorMax = Vector2.one;
            congratsRect.offsetMin = new Vector2(20f, Height);
            congratsRect.offsetMax = new Vector2(-20f, 0f);
            _congrats = congratsObject.GetComponent<TextMeshProUGUI>();
            if (styleSource != null)
            {
                _congrats.font = styleSource.font;
                _congrats.fontSharedMaterial = styleSource.fontSharedMaterial;
            }
            _congrats.fontSize = 26f;
            _congrats.color = new Color(1f, 0.83f, 0.35f, 1f);
            _congrats.alignment = TextAlignmentOptions.Center;
            _congrats.textWrappingMode = TextWrappingModes.Normal;
            _congrats.raycastTarget = false;
            congratsObject.SetActive(false);

            // Only shrink a viewport that sits inside the piece view; shrinking the piece view itself
            // would move the bar along with it.
            RectTransform viewport = scrollRect != null ? (scrollRect.viewport ?? (RectTransform)scrollRect.transform) : null;
            if (viewport != null && viewport != pieceView && viewport.IsChildOf(pieceView))
            {
                _viewport = viewport;
                _viewportBottom = viewport.offsetMin.y;
            }

            _root.SetActive(false);
        }

        public void Show(int count)
        {
            bool show = count > 0;
            if (show)
                _text.text = Format(count);
            _root.SetActive(show);
            _root.transform.SetAsLastSibling();
            if (_viewport != null)
                _viewport.offsetMin = new Vector2(_viewport.offsetMin.x, _viewportBottom + (show ? Height : 0f));
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
            Show(0);
            ShowCongrats(null);
        }

        private static string Format(int count)
        {
            try { return string.Format(BuildTrackerPlugin.UndiscoveredText.Value, count); }
            catch (System.FormatException) { return count + " not discovered yet"; }
        }
    }
}
