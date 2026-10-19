// The peak pins on the big map, switched with the "Show high points" toggle (see MapToggles).
// Pins are not saved; they are re-added whenever the map was cleared (loading a world) or the selection changed.

using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace PeakFinder
{
    [HarmonyPatch(typeof(Minimap))]
    internal static class MapPeaks
    {
        private static readonly AccessTools.FieldRef<Minimap, List<Minimap.PinData>> PinsRef =
            AccessTools.FieldRefAccess<Minimap, List<Minimap.PinData>>("m_pins");
        private static readonly Func<Minimap, Vector3, bool> IsExplored =
            AccessTools.MethodDelegate<Func<Minimap, Vector3, bool>>(AccessTools.Method(typeof(Minimap), "IsExplored"));

        private static readonly List<Minimap.PinData> s_pins = new List<Minimap.PinData>();
        private static string s_shown = "";
        private static float s_nextCheck;

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
                Peaks.EnsureScan();
                foreach (Peak peak in Peaks.All)
                {
                    if (chosen.Count >= PeakFinderPlugin.PeakCount.Value)
                        break;
                    if (!PeakFinderPlugin.OnlyExplored.Value || PeakFinderPlugin.ShowWholeMap.Value ||
                        IsExplored(__instance, peak.Position))
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
