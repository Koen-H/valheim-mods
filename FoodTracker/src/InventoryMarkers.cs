// A small diamond on the left of every inventory or chest slot that holds food you haven't eaten yet.
// InventoryGrid.UpdateGui runs every frame while the inventory is open; the marker is created once per slot.

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

        private static void Postfix(InventoryGrid __instance)
        {
            Inventory inventory = __instance.GetInventory();
            bool show = FoodTrackerPlugin.InventoryMarkers.Value && inventory != null && Game.instance != null;

            foreach (InventoryElement element in ElementsRef(__instance))
            {
                ItemDrop.ItemData item = show && element.m_used ? inventory.GetItemAt(element.Position.x, element.Position.y) : null;
                bool hungry = item != null && FoodStatus.NotEatenYet(item.m_shared.m_name);

                Transform marker = element.transform.Find(MarkerName);
                if (marker == null)
                {
                    if (!hungry)
                        continue;
                    marker = CreateMarker(element.transform);
                }
                if (marker.gameObject.activeSelf != hungry)
                    marker.gameObject.SetActive(hungry);
            }
        }

        private static Transform CreateMarker(Transform slot)
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
            return rt;
        }
    }
}
