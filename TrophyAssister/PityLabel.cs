// The dry-kill progress message gets its own text line just below the game's
// top-left message. The game shows top-left messages one at a time from a shared
// queue, so item pickups right after a kill would replace ours after about a second.
//
// The label is a copy of the game's own top-left message text (same font and style),
// shows the last few updates, and fades each one out after a few seconds.

using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace TrophyAssister
{
    internal static class PityLabel
    {
        private const float ShowSeconds = 6f;
        private const float FadeSeconds = 1f;
        private const int MaxLines = 3;

        private class Line { public string Text; public float Until; public int Alpha = -1; }
        private static readonly List<Line> Lines = new List<Line>();
        private static readonly StringBuilder Text = new StringBuilder();
        private static TMP_Text _label;
        // Set when lines were added or removed: the text must be rebuilt even if no alpha changed.
        private static bool _changed;

        public static void Show(string text)
        {
            Lines.Add(new Line { Text = text, Until = Time.time + ShowSeconds });
            if (Lines.Count > MaxLines) Lines.RemoveAt(0);
            _changed = true;
            Refresh();
        }

        // Called every frame from the plugin; does nothing while there's nothing to show.
        public static void Tick()
        {
            if (Lines.Count == 0) return;
            for (int i = Lines.Count - 1; i >= 0; i--)
            {
                if (Lines[i].Until > Time.time) continue;
                Lines.RemoveAt(i);
                _changed = true;
            }
            Refresh();
        }

        private static void Refresh()
        {
            var label = Label();
            if (label == null) return;

            bool show = Lines.Count > 0 && !global::Hud.IsUserHidden();
            if (label.gameObject.activeSelf != show) label.gameObject.SetActive(show);
            if (!show) return;

            // Only the last second of a line fades; before that nothing changes and the text stays as it is.
            bool changed = _changed;
            foreach (Line line in Lines)
            {
                int alpha = Mathf.RoundToInt(Mathf.Clamp01((line.Until - Time.time) / FadeSeconds) * 255f);
                if (alpha != line.Alpha)
                {
                    line.Alpha = alpha;
                    changed = true;
                }
            }
            if (!changed) return;
            _changed = false;

            Text.Length = 0;
            foreach (Line line in Lines)
            {
                if (Text.Length > 0) Text.Append('\n');
                Text.Append("<alpha=#").Append(line.Alpha.ToString("X2")).Append('>').Append(line.Text);
            }
            label.text = Text.ToString();
        }

        private static TMP_Text Label()
        {
            if (_label != null) return _label; // Unity null: recreated after the HUD is rebuilt (e.g. logout)

            var source = MessageHud.instance != null ? MessageHud.instance.m_messageText : null;
            if (source == null) return null;

            var go = Object.Instantiate(source.gameObject, source.transform.parent);
            go.name = "TrophyAssister_pity";
            foreach (Transform child in go.transform) Object.Destroy(child.gameObject);

            _label = go.GetComponent<TMP_Text>();
            _label.canvasRenderer.SetAlpha(1f); // the game fades its own line; ours fades per line
            _label.fontSize = source.fontSize * 0.8f;
            _label.text = "";

            var rt = (RectTransform)go.transform;
            var sourceRt = (RectTransform)source.transform;
            rt.anchoredPosition = sourceRt.anchoredPosition - new Vector2(0f, sourceRt.rect.height);
            return _label;
        }
    }
}
