// Fishing indicator: a line under the crosshair that says when to reel in (block) and when to wait.
//
// - Bait in the water, nothing biting: "Wait for a bite". Reeling now only pulls the bait closer.
// - A fish bites on the right bait: "Bite! Reel in to hook it" for the half second in which that works.
// - Fish on the hook: "Reel in" while it is calm, "Fish is fighting, wait" while it thrashes. Reeling a
//   fighting fish costs twice the stamina for half the line, and running out of stamina loses the fish.
//   A hooked fish fights for 0.5-3 s (longer with stars), then rests for 0.75-4 s.

using HarmonyLib;
using TMPro;
using UnityEngine;

namespace FishTracker
{
    [HarmonyPatch]
    internal static class FishingIndicator
    {
        // FishingFloat.TryToHook hooks the fish this long after a nibble.
        private const float HookWindow = 0.5f;

        private static readonly Color WaitColor = new Color(0.85f, 0.85f, 0.85f, 0.8f);
        private static readonly Color PullColor = new Color(0.4f, 1f, 0.4f, 1f);
        private static readonly Color HoldColor = new Color(1f, 0.35f, 0.3f, 1f);

        private static readonly AccessTools.FieldRef<FishingFloat, Fish> s_nibbler =
            AccessTools.FieldRefAccess<FishingFloat, Fish>("m_nibbler");
        private static readonly AccessTools.FieldRef<FishingFloat, float> s_nibbleTime =
            AccessTools.FieldRefAccess<FishingFloat, float>("m_nibbleTime");
        private static readonly AccessTools.FieldRef<FishingFloat, ZNetView> s_floatView =
            AccessTools.FieldRefAccess<FishingFloat, ZNetView>("m_nview");

        private static TMP_Text s_label;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Hud), "Awake")]
        private static void HudAwake(Hud __instance)
        {
            RectTransform crosshair = __instance.m_crosshair.rectTransform;
            var labelObject = new GameObject("FishTracker_FishingIndicator", typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = (RectTransform)labelObject.transform;
            rect.SetParent(crosshair.parent, false);
            rect.anchorMin = crosshair.anchorMin;
            rect.anchorMax = crosshair.anchorMax;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(400f, 40f);
            rect.anchoredPosition = crosshair.anchoredPosition + new Vector2(0f, -40f);

            s_label = labelObject.GetComponent<TextMeshProUGUI>();
            s_label.font = __instance.m_hoverName.font;
            s_label.fontSharedMaterial = __instance.m_hoverName.fontSharedMaterial;
            s_label.fontSize = 26f;
            s_label.fontStyle = FontStyles.Bold;
            s_label.alignment = TextAlignmentOptions.Top;
            s_label.raycastTarget = false;
            labelObject.SetActive(false);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Hud), "Update")]
        private static void HudUpdate()
        {
            if (s_label == null)
                return;

            FishingFloat fishingFloat = FishTrackerPlugin.FishingIndicator.Value ? FindLocalFloat() : null;
            if (fishingFloat == null || !fishingFloat.IsInWater())
            {
                s_label.gameObject.SetActive(false);
                return;
            }

            Fish fish = fishingFloat.GetCatch();
            if (fish != null)
            {
                // The player who hooks a fish takes ownership of it, so its fight timer runs here.
                if (fish.IsEscaping())
                    Show("Fish is fighting, wait", HoldColor);
                else
                    Show("Reel in", PullColor);
            }
            else if (s_nibbler(fishingFloat) != null && Time.time - s_nibbleTime(fishingFloat) < HookWindow)
                Show("Bite! Reel in to hook it", PullColor);
            else
                Show("Wait for a bite", WaitColor);
        }

        private static void Show(string text, Color color)
        {
            s_label.text = text;
            s_label.color = color;
            s_label.gameObject.SetActive(true);
        }

        private static FishingFloat FindLocalFloat()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
                return null;

            long userId = player.GetZDOID().UserID;
            foreach (FishingFloat fishingFloat in FishingFloat.GetAllInstances())
            {
                ZNetView view = s_floatView(fishingFloat);
                if (view != null && view.IsValid() && view.GetZDO().GetLong(ZDOVars.s_rodOwner) == userId)
                    return fishingFloat;
            }
            return null;
        }
    }
}
