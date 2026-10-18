// Player.UpdateEnvStatusEffects adds the "Resting" status when you sit or stand by a fire under a roof.
// The moment it appears, a message lists the best pieces to add.

using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace ComfortHelper
{
    [HarmonyPatch(typeof(Player), "UpdateEnvStatusEffects")]
    internal static class RestingMessage
    {
        private static bool s_wasResting;
        private static float s_lastShown = -10000f;

        private static void Postfix(Player __instance)
        {
            if (__instance != Player.m_localPlayer)
                return;

            bool resting = __instance.GetSEMan().HaveStatusEffect(SEMan.s_statusEffectResting);
            bool started = resting && !s_wasResting;
            s_wasResting = resting;
            if (!started || !ComfortHelperPlugin.RestingMessage.Value ||
                Time.time - s_lastShown < ComfortHelperPlugin.MessageCooldown.Value)
                return;

            s_lastShown = Time.time;
            ComfortReport report = Comfort.Build(__instance);
            string text = $"Comfort {report.Current} (best so far {report.MaxReached:0}, Comfort is King needs 20)";
            if (report.Suggestions.Count > 0)
            {
                text += "\nTo raise it: " + string.Join(", ",
                    report.Suggestions.Take(ComfortHelperPlugin.MaxSuggestions.Value).Select(Comfort.Describe));
                if (report.Suggestions.Count > ComfortHelperPlugin.MaxSuggestions.Value)
                    text += ", ... (/comfort lists all)";
            }
            __instance.Message(MessageHud.MessageType.TopLeft, text);
        }
    }
}
