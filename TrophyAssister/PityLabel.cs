// The dry-kill progress message gets its own text line just below the game's
// top-left message. The game shows top-left messages one at a time from a shared
// queue, so item pickups right after a kill would replace ours after about a second.
//
// The label is a copy of the game's own top-left message text (same font and style),
// shows the last few updates, and fades each one out after a few seconds.

using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace TrophyAssister
{
    internal static class PityLabel
    {
        private const float ShowSeconds = 6f;
        private const float FadeSeconds = 1f;
        private const int MaxLines = 3;

        private class Line { public string Text; public float Until; }
        private static readonly List<Line> Lines = new List<Line>();
        private static TMP_Text _label;

        public static void Show(string text)
        {
            Lines.Add(new Line { Text = text, Until = Time.time + ShowSeconds });
            if (Lines.Count > MaxLines) Lines.RemoveAt(0);
            Refresh();
        }

        // Called every frame from the plugin; does nothing while there's nothing to show.
        public static void Tick()
        {
            if (Lines.Count == 0) return;
            Lines.RemoveAll(l => l.Until <= Time.time);
            Refresh();
        }

        private static void Refresh()
        {
            var label = Label();
            if (label == null) return;

            bool show = Lines.Count > 0 && !global::Hud.IsUserHidden();
            if (label.gameObject.activeSelf != show) label.gameObject.SetActive(show);
            if (!show) return;

            label.text = string.Join("\n", Lines.Select(l =>
            {
                int alpha = Mathf.RoundToInt(Mathf.Clamp01((l.Until - Time.time) / FadeSeconds) * 255f);
                return $"<alpha=#{alpha:X2}>{l.Text}";
            }));
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
