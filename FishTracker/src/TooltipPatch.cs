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

            string name = item.m_shared.m_name;
            var text = new StringBuilder(__result);

            FishInfo fish = FishData.ByItemName(name);
            if (fish != null)
            {
                if (!FishData.IsCaught(fish))
                    text.Append($"\n\n<color={HintColor}>You haven't caught this fish yet.</color>");
                if (FishData.Fisherman != null)
                    text.Append($"\n<color={ProgressColor}>{Localization.instance.Localize(FishData.Fisherman.m_name)}: " +
                                $"{FishData.CaughtCount()} / {FishData.TotalCount()}</color>");
                __result = text.ToString();
                return;
            }

            string bait = Chum.BaitOf(name) ?? name;
            List<FishInfo> biters = FishData.All.FindAll(f => f.Baits.ContainsKey(bait));
            if (biters.Count == 0)
                return;

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
