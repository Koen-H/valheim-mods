// The Materials window (see Shared/InventoryWindow.cs): every material, found ones in colour, missing ones greyed
// out with where they come from, and materials of later biomes hidden until the boss before them is defeated.

using System.Collections.Generic;
using HarmonyLib;
using Koendertjh.Shared;

namespace MaterialTracker
{
    [HarmonyPatch(typeof(InventoryGui))]
    internal static class MaterialsPanel
    {
        private const string Title = "Materials";

        private static readonly InventoryWindow Window = new InventoryWindow("MaterialTracker", Title,
            "Every crafting material, and where to find it", BarIcon.Get, Fill);

        [HarmonyPostfix]
        [HarmonyPatch("Awake")]
        private static void Create(InventoryGui __instance) => Window.Create(__instance, MaterialTrackerPlugin.Log);

        [HarmonyPostfix]
        [HarmonyPatch(nameof(InventoryGui.Hide))]
        private static void AfterHide() => Window.Close();

        [HarmonyPostfix]
        [HarmonyPatch(nameof(InventoryGui.OnOpenTrophies))]
        private static void AfterOpenTrophies() => Window.Close();

        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        private static bool BeforeUpdate() => !Window.HandleEscape();

        private static string Fill(List<WindowEntry> entries)
        {
            Player player = Player.m_localPlayer;
            List<MaterialInfo> all = Materials.All;
            int found = 0;

            foreach (MaterialInfo material in all)
            {
                bool have = player.IsMaterialKnown(material.Name);
                if (have)
                    found++;
                if (have && !MaterialTrackerPlugin.ShowFound.Value)
                    continue;

                string hint = MaterialTrackerPlugin.ShowHints.Value ? material.Hint : "";
                bool revealed = have || MaterialTrackerPlugin.ShowAll.Value || Stages.IsRevealed(material.Stage);
                entries.Add(new WindowEntry
                {
                    Icon = material.Item.m_itemData.GetIcon(),
                    Name = Localization.instance.Localize(material.Name),
                    Description = have ? hint : (hint.Length > 0 ? hint + "\n" : "") + "<i>Not found yet</i>",
                    State = have ? EntryState.Done : revealed ? EntryState.Missing : EntryState.Hidden
                });
            }
            return $"{Title} {found} / {all.Count}";
        }
    }
}
