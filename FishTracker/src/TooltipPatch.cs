// Fish tooltips: "You haven't caught this fish yet" and The Fisherman progress.
// Bait and chum tooltips: the fish that bite on it, with the ones you haven't caught yet marked.

using System.Collections.Generic;
using System.Text;
using HarmonyLib;

namespace FishTracker
{
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip),
        typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool))]
    internal static class TooltipPatch
    {
        private const string HintColor = "#ffb347";
        private const string ProgressColor = "#a0a0a0";

        private static void Postfix(ItemDrop.ItemData item, ref string __result)
        {
            if (!FishTrackerPlugin.TooltipHints.Value || item?.m_shared == null || Game.instance == null ||
                ObjectDB.instance == null)
                return;

            // Runs every frame while the inventory is open: find out first whether there is anything to add.
            string name = item.m_shared.m_name;
            FishInfo fish = FishData.ByItemName(name);
            if (fish != null)
            {
                var fishText = new StringBuilder(__result);
                if (!FishData.IsCaught(fish))
                    fishText.Append($"\n\n<color={HintColor}>You haven't caught this fish yet.</color>");
                if (FishData.Fisherman != null)
                    fishText.Append($"\n<color={ProgressColor}>{Localization.instance.Localize(FishData.Fisherman.m_name)}: " +
                                    $"{FishData.CaughtCount()} / {FishData.TotalCount()}</color>");
                __result = fishText.ToString();
                return;
            }

            List<FishInfo> biters = FishData.BitersOf(Chum.BaitOf(name) ?? name);
            if (biters.Count == 0)
                return;

            var text = new StringBuilder(__result);
            text.Append("\n\nFish that bite:");
            Player player = Player.m_localPlayer;
            foreach (FishInfo biter in biters)
            {
                bool caught = FishData.IsCaught(biter);
                bool known = caught || (player != null && player.IsMaterialKnown(biter.ItemName));
                string fishName = known || !FishTrackerPlugin.HideUnknownFish.Value
                    ? Localization.instance.Localize(biter.Fish.m_name)
                    : "???";
                text.Append(caught
                    ? $"\n  {fishName}"
                    : $"\n  <color={HintColor}>{fishName} (not caught yet)</color>");
            }
            __result = text.ToString();
        }
    }
}
