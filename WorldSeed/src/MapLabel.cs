// A "Seed: ..." line on the big map, right below the biome name. It is a copy of the biome name text, so it
// has the game's font and style, and it shows and hides together with the big map.

using HarmonyLib;
using TMPro;
using UnityEngine;

namespace WorldSeed
{
    [HarmonyPatch(typeof(Minimap))]
    internal static class MapLabel
    {
        private static TMP_Text s_label;
        private static float s_nextRefresh;

        [HarmonyPostfix]
        [HarmonyPatch("Start")]
        private static void Create(Minimap __instance)
        {
            TMP_Text biome = __instance.m_biomeNameLarge;
            if (biome == null)
                return;

            GameObject copy = Object.Instantiate(biome.gameObject, biome.transform.parent);
            copy.name = "WorldSeedLabel";
            // The biome name pulses through its animator; the copy keeps still.
            Animator animator = copy.GetComponent<Animator>();
            if (animator != null)
                Object.Destroy(animator);

            var rt = (RectTransform)copy.transform;
            var biomeRt = (RectTransform)biome.transform;
            rt.anchoredPosition = biomeRt.anchoredPosition - new Vector2(0f, biomeRt.rect.height * 0.6f + 6f);

            s_label = copy.GetComponent<TMP_Text>();
            s_label.fontSize = biome.fontSize * 0.55f;
            s_label.text = "";
        }

        // Minimap.Update runs every frame; the text only needs a look now and then, and only while it can be seen.
        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        private static void Refresh()
        {
            if (s_label == null)
                return;
            if (!s_label.isActiveAndEnabled)
            {
                // Map closed: refresh right away the next time it opens.
                s_nextRefresh = 0f;
                return;
            }
            if (Time.time < s_nextRefresh)
                return;
            s_nextRefresh = Time.time + 1f;

            string text = "";
            if (WorldSeedPlugin.ShowOnMap.Value && WorldSeedPlugin.Seed != null)
                text = WorldSeedPlugin.Unlocked ? $"Seed: {WorldSeedPlugin.Seed}" : "Seed: ???";
            if (s_label.text != text)
                s_label.text = text;
        }
    }
}
