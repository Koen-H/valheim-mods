// StoreGui.FillList rebuilds the shop list: one element per ware the trader sells right now, in the order of
// Trader.GetAvailableItems. The markers are added to those elements, and the locked-wares row is an extra copy
// of the list element below them (not in m_itemList, so selecting and buying work as before).

using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MerchantTracker
{
    [HarmonyPatch(typeof(StoreGui), "FillList")]
    internal static class StorePatch
    {
        private static GameObject s_lockedRow;

        private static void Prefix()
        {
            if (s_lockedRow != null)
                Object.Destroy(s_lockedRow);
            s_lockedRow = null;
        }

        private static void Postfix(StoreGui __instance, Trader ___m_trader, List<GameObject> ___m_itemList)
        {
            Player player = Player.m_localPlayer;
            if (___m_trader == null || player == null)
                return;

            List<Trader.TradeItem> available = ___m_trader.GetAvailableItems();
            if (MerchantTrackerPlugin.NewMarker.Value)
            {
                for (int i = 0; i < available.Count && i < ___m_itemList.Count; i++)
                {
                    ItemDrop prefab = available[i].m_prefab;
                    if (prefab == null || player.IsMaterialKnown(prefab.m_itemData.m_shared.m_name))
                        continue;
                    TMP_Text name = ___m_itemList[i].transform.Find("name")?.GetComponent<TMP_Text>();
                    if (name != null)
                        name.text += " " + MerchantTrackerPlugin.MarkerText.Value;
                }
            }

            if (MerchantTrackerPlugin.LockedWaresRow.Value)
                AddLockedRow(__instance, ___m_trader, available.Count);
        }

        // Wares that wait for a world key (a boss, Hildir's chests). Wares already bought once are not counted.
        private static int CountLocked(Trader trader)
        {
            int locked = 0;
            foreach (Trader.TradeItem item in trader.m_items)
            {
                if (string.IsNullOrEmpty(item.m_requiredGlobalKey) || ZoneSystem.instance.GetGlobalKey(item.m_requiredGlobalKey))
                    continue;
                if (!string.IsNullOrEmpty(item.m_buyKey) && Player.m_localPlayer.HaveUniqueKey(item.m_buyKey))
                    continue;
                locked++;
            }
            return locked;
        }

        private static void AddLockedRow(StoreGui gui, Trader trader, int rows)
        {
            int locked = CountLocked(trader);
            if (locked == 0)
                return;

            RectTransform root = gui.m_listRoot;
            s_lockedRow = Object.Instantiate(gui.m_listElement, root);
            s_lockedRow.name = "MerchantTrackerLocked";
            s_lockedRow.SetActive(true);

            // Not a ware: nothing to click, no tooltip, no price or icon.
            Object.Destroy(s_lockedRow.GetComponent<Button>());
            Object.Destroy(s_lockedRow.GetComponent<UITooltip>());
            Transform icon = s_lockedRow.transform.Find("icon");
            if (icon != null)
                icon.gameObject.SetActive(false);
            // The price sits in its own little box (with the coin icon) when it isn't directly on the row.
            Transform price = Utils.FindChild(s_lockedRow.transform, "price");
            if (price != null)
                (price.parent != s_lockedRow.transform ? price.parent : price).gameObject.SetActive(false);

            TMP_Text name = s_lockedRow.transform.Find("name")?.GetComponent<TMP_Text>();
            if (name != null)
            {
                string traderName = Localization.instance.Localize(trader.m_name);
                name.text = $"Only Odin knows what else {traderName} will sell ({locked})";
                name.color = Color.grey;
                name.fontStyle |= FontStyles.Italic;
            }

            // Same placement as FillList, one row further down; make the list tall enough to scroll to it.
            var rt = (RectTransform)s_lockedRow.transform;
            float margin = (root.rect.width - rt.rect.width) / 2f;
            rt.anchoredPosition = new Vector2(margin, rows * -gui.m_itemSpacing - margin);
            float height = (rows + 1) * gui.m_itemSpacing;
            if (root.rect.height < height)
                root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }
    }
}
