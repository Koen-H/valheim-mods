// The PeakFinder toggles on the big map: "Show high points" and "Show whole map".
// Each is a copy of the map's Cartography Table panel (Minimap.m_sharedMapHint), stacked above it. The original
// toggle is wired in the game's UI prefab to Minimap.OnToggleSharedMapData; the copy's handler is switched off
// and replaced. The Cartography Table panel only shows when you have map data from a table, so the stack moves
// down into its place while it is hidden.
// "Show whole map" swaps the big map's fog texture for an empty one; what you explored is not changed.

using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PeakFinder
{
    [HarmonyPatch(typeof(Minimap))]
    internal static class MapToggles
    {
        private const float Spacing = 50f;

        private static readonly AccessTools.FieldRef<Minimap, Material> LargeMapMaterialRef =
            AccessTools.FieldRefAccess<Minimap, Material>("m_mapLargeShader");
        private static readonly AccessTools.FieldRef<Minimap, Texture2D> FogTextureRef =
            AccessTools.FieldRefAccess<Minimap, Texture2D>("m_fogTexture");

        private sealed class MapToggle
        {
            public string Label;
            public string Tooltip;
            public ConfigEntry<bool> Setting;
            public RectTransform Panel;
            public Toggle Toggle;
        }

        private static readonly List<MapToggle> s_toggles = new List<MapToggle>();
        private static bool s_subscribed;
        // The map whose fog texture is swapped right now, if any.
        private static Minimap s_clearedMap;

        private static void Define()
        {
            if (s_toggles.Count > 0)
                return;
            s_toggles.Add(new MapToggle
            {
                Label = "Show high points",
                Tooltip = "Pins on the highest peaks, for Top of the World (build at 432 m)",
                Setting = PeakFinderPlugin.ShowPeaks
            });
            s_toggles.Add(new MapToggle
            {
                Label = "Show whole map",
                Tooltip = "Lift the fog from the whole map while this is on. What you explored doesn't change.",
                Setting = PeakFinderPlugin.ShowWholeMap
            });
        }

        [HarmonyPostfix]
        [HarmonyPatch("Start")]
        private static void CreateToggles(Minimap __instance)
        {
            Define();
            s_clearedMap = null;
            GameObject original = __instance.m_sharedMapHint;
            foreach (MapToggle mapToggle in s_toggles)
            {
                mapToggle.Panel = null;
                mapToggle.Toggle = null;
                if (original == null)
                    continue;
                try
                {
                    CreateToggle(original, mapToggle);
                }
                catch (Exception e)
                {
                    PeakFinderPlugin.Log.LogWarning($"Couldn't add the '{mapToggle.Label}' map toggle (use the config): {e.Message}");
                }
            }

            if (!s_subscribed)
            {
                s_subscribed = true;
                foreach (MapToggle mapToggle in s_toggles)
                {
                    MapToggle current = mapToggle;
                    current.Setting.SettingChanged += (_, __) =>
                    {
                        if (current.Toggle != null && current.Toggle.isOn != current.Setting.Value)
                            current.Toggle.isOn = current.Setting.Value;
                    };
                }
            }
        }

        private static void CreateToggle(GameObject original, MapToggle mapToggle)
        {
            GameObject copy = UnityEngine.Object.Instantiate(original, original.transform.parent);
            copy.name = "PeakFinder_" + mapToggle.Setting.Definition.Key;
            copy.SetActive(true);

            // Gamepad hotkey and its hint belong to the Cartography Table toggle.
            foreach (UIGamePad gamePad in copy.GetComponentsInChildren<UIGamePad>(true))
            {
                if (gamePad.m_hint != null)
                    gamePad.m_hint.SetActive(false);
                UnityEngine.Object.Destroy(gamePad);
            }

            Toggle toggle = copy.GetComponentInChildren<Toggle>(true);
            for (int i = 0; i < toggle.onValueChanged.GetPersistentEventCount(); i++)
                toggle.onValueChanged.SetPersistentListenerState(i, UnityEventCallState.Off);
            toggle.isOn = mapToggle.Setting.Value;
            toggle.onValueChanged.AddListener(on => mapToggle.Setting.Value = on);

            Transform labelTransform = toggle.transform.Find("Label");
            TMP_Text label = labelTransform != null ? labelTransform.GetComponent<TMP_Text>() : copy.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                // The Localize component would put the original label back.
                foreach (Component component in label.GetComponents<Component>())
                {
                    if (component != null && component.GetType().Name == "Localize")
                        UnityEngine.Object.Destroy(component);
                }
                label.text = mapToggle.Label;
            }
            foreach (UITooltip tooltip in copy.GetComponentsInChildren<UITooltip>(true))
                tooltip.Set(mapToggle.Label, mapToggle.Tooltip);

            mapToggle.Panel = (RectTransform)copy.transform;
            mapToggle.Toggle = toggle;
        }

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        private static void UpdateToggles(Minimap __instance)
        {
            GameObject original = __instance.m_sharedMapHint;
            if (original != null)
            {
                var originalRt = (RectTransform)original.transform;
                Vector2 position = originalRt.anchoredPosition;
                if (original.activeSelf)
                    position.y += Spacing;
                foreach (MapToggle mapToggle in s_toggles)
                {
                    if (mapToggle.Panel == null)
                        continue;
                    mapToggle.Panel.anchoredPosition = position;
                    position.y += Spacing;
                }
            }

            ShowWholeMap(__instance, PeakFinderPlugin.ShowWholeMap.Value);
        }

        private static void ShowWholeMap(Minimap map, bool show)
        {
            if (show == (s_clearedMap == map))
                return;
            Material material = LargeMapMaterialRef(map);
            if (material == null)
                return;
            material.SetTexture("_FogTex", show ? Texture2D.blackTexture : (Texture)FogTextureRef(map));
            s_clearedMap = show ? map : null;
        }
    }
}
