// The "Show high points" toggle on the big map and the peak pins.
// The toggle is a copy of the map's "public position" toggle, placed right below it. The original is wired in
// the game's UI prefab to Minimap.OnTogglePublicPosition; the copy's handler is switched off and replaced.
// Pins are not saved; they are re-added whenever the map was cleared (loading a world) or the selection changed.

using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PeakFinder
{
    [HarmonyPatch(typeof(Minimap))]
    internal static class MapPeaks
    {
        private const string Label = "Show high points";

        private static readonly AccessTools.FieldRef<Minimap, List<Minimap.PinData>> PinsRef =
            AccessTools.FieldRefAccess<Minimap, List<Minimap.PinData>>("m_pins");
        private static readonly Func<Minimap, Vector3, bool> IsExplored =
            AccessTools.MethodDelegate<Func<Minimap, Vector3, bool>>(AccessTools.Method(typeof(Minimap), "IsExplored"));

        private static readonly List<Minimap.PinData> s_pins = new List<Minimap.PinData>();
        private static string s_shown = "";
        private static float s_nextCheck;

        // ---- toggle ----

        [HarmonyPostfix]
        [HarmonyPatch("Start")]
        private static void CreateToggle(Minimap __instance)
        {
            s_pins.Clear();
            s_shown = "";
            try
            {
                Toggle original = __instance.m_publicPosition;
                if (original == null)
                    return;

                GameObject copy = UnityEngine.Object.Instantiate(original.gameObject, original.transform.parent);
                copy.name = "PeakFinder_Toggle";
                var rt = (RectTransform)copy.transform;
                var originalRt = (RectTransform)original.transform;
                rt.anchoredPosition = originalRt.anchoredPosition - new Vector2(0f, originalRt.rect.height + 4f);

                Toggle toggle = copy.GetComponent<Toggle>();
                for (int i = 0; i < toggle.onValueChanged.GetPersistentEventCount(); i++)
                    toggle.onValueChanged.SetPersistentListenerState(i, UnityEventCallState.Off);
                toggle.isOn = PeakFinderPlugin.ShowPeaks.Value;
                toggle.onValueChanged.AddListener(on => PeakFinderPlugin.ShowPeaks.Value = on);

                foreach (TMP_Text text in copy.GetComponentsInChildren<TMP_Text>(true))
                {
                    // The Localize component would put the original label back.
                    foreach (Component component in text.GetComponents<Component>())
                    {
                        if (component != null && component.GetType().Name == "Localize")
                            UnityEngine.Object.Destroy(component);
                    }
                    text.text = Label;
                }
                foreach (UITooltip tooltip in copy.GetComponentsInChildren<UITooltip>(true))
                    tooltip.Set(Label, "Pins on the highest peaks, for Top of the World (build at 432 m)");

                PeakFinderPlugin.ShowPeaks.SettingChanged += (_, __) =>
                {
                    if (toggle != null && toggle.isOn != PeakFinderPlugin.ShowPeaks.Value)
                        toggle.isOn = PeakFinderPlugin.ShowPeaks.Value;
                    s_nextCheck = 0f;
                };
            }
            catch (Exception e)
            {
                PeakFinderPlugin.Log.LogWarning($"Couldn't add the map toggle (use ShowPeaks in the config): {e.Message}");
            }
        }

        // ---- pins ----

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        private static void UpdatePins(Minimap __instance)
        {
            if (Player.m_localPlayer == null || Time.time < s_nextCheck)
                return;
            s_nextCheck = Time.time + 2f;

            List<Minimap.PinData> pins = PinsRef(__instance);
            bool cleared = s_pins.Exists(p => !pins.Contains(p));
            List<Peak> chosen = new List<Peak>();
            if (PeakFinderPlugin.ShowPeaks.Value)
            {
                Peaks.EnsureScan(__instance);
                foreach (Peak peak in Peaks.All)
                {
                    if (chosen.Count >= PeakFinderPlugin.PeakCount.Value)
                        break;
                    if (!PeakFinderPlugin.OnlyExplored.Value || IsExplored(__instance, peak.Position))
                        chosen.Add(peak);
                }
            }

            string key = string.Join(";", chosen.ConvertAll(p => p.Position.ToString()));
            if (key == s_shown && !cleared)
                return;

            foreach (Minimap.PinData pin in s_pins)
                __instance.RemovePin(pin);
            s_pins.Clear();
            foreach (Peak peak in chosen)
                s_pins.Add(__instance.AddPin(peak.Position, Minimap.PinType.Icon3, PinName(peak), false, false));
            s_shown = key;
        }

        // "Peak 401 m (+31 m)": the extra is what you still need to build on top for Top of the World.
        private static string PinName(Peak peak)
        {
            float missing = PeakFinderPlugin.TopOfTheWorld - peak.Height;
            return missing > 0f ? $"Peak {peak.Height:0} m (+{Mathf.Ceil(missing):0} m)" : $"Peak {peak.Height:0} m";
        }
    }
}
