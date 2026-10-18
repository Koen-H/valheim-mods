// A small diamond on the left of every inventory or chest slot that holds food you haven't eaten yet.
// InventoryGrid.UpdateGui runs every frame while the inventory is open, so this keeps the work per frame small:
// the items are put in a slot table once per call, and each slot's marker is remembered after it is created.

using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace FoodTracker
{
    [HarmonyPatch(typeof(InventoryGrid), "UpdateGui")]
    internal static class InventoryMarkers
    {
        private const string MarkerName = "FoodTrackerMarker";

        private static readonly AccessTools.FieldRef<InventoryGrid, List<InventoryElement>> ElementsRef =
            AccessTools.FieldRefAccess<InventoryGrid, List<InventoryElement>>("m_elements");

        // Slot element -> its marker. Elements are destroyed when a grid changes size; their entries are dropped
        // once the table grows.
        private static readonly Dictionary<InventoryElement, GameObject> s_markers = new Dictionary<InventoryElement, GameObject>();
        private static ItemDrop.ItemData[] s_slots = new ItemDrop.ItemData[0];

        private static void Postfix(InventoryGrid __instance)
        {
            Inventory inventory = __instance.GetInventory();
            bool show = FoodTrackerPlugin.InventoryMarkers.Value && inventory != null && Game.instance != null;
            if (!show && s_markers.Count == 0)
                return;

            int width = 0;
            if (show)
            {
                width = inventory.GetWidth();
                int size = width * inventory.GetHeight();
                if (s_slots.Length < size)
                    s_slots = new ItemDrop.ItemData[size];
                System.Array.Clear(s_slots, 0, s_slots.Length);
                foreach (ItemDrop.ItemData item in inventory.GetAllItems())
                {
                    int index = item.m_gridPos.y * width + item.m_gridPos.x;
                    if (index >= 0 && index < size)
                        s_slots[index] = item;
                }
            }

            foreach (InventoryElement element in ElementsRef(__instance))
            {
                ItemDrop.ItemData item = null;
                if (show && element.m_used)
                {
                    int index = element.Position.y * width + element.Position.x;
                    if (index >= 0 && index < s_slots.Length)
                        item = s_slots[index];
                }
                bool hungry = item != null && FoodStatus.NotEatenYet(item.m_shared.m_name);

                if (!s_markers.TryGetValue(element, out GameObject marker) || marker == null)
                {
                    if (!hungry)
                        continue;
                    marker = CreateMarker(element.transform);
                    s_markers[element] = marker;
                    if (s_markers.Count > 256)
                        DropDestroyed();
                }
                if (marker.activeSelf != hungry)
                    marker.SetActive(hungry);
            }
        }

        private static void DropDestroyed()
        {
            var gone = new List<InventoryElement>();
            foreach (KeyValuePair<InventoryElement, GameObject> pair in s_markers)
            {
                if (pair.Key == null || pair.Value == null)
                    gone.Add(pair.Key);
            }
            foreach (InventoryElement element in gone)
                s_markers.Remove(element);
        }

        private static GameObject CreateMarker(Transform slot)
        {
            var go = new GameObject(MarkerName, typeof(RectTransform), typeof(Image), typeof(Outline));
            var rt = (RectTransform)go.transform;
            rt.SetParent(slot, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(8f, 0f);
            rt.sizeDelta = new Vector2(8f, 8f);
            rt.localRotation = Quaternion.Euler(0f, 0f, 45f);
            rt.SetAsLastSibling();

            var image = go.GetComponent<Image>();
            image.color = FoodTrackerPlugin.MarkerColor.Value;
            image.raycastTarget = false;

            var outline = go.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            go.SetActive(false);
            return go;
        }
    }
}
