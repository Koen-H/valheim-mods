// Player.PlacePiece records your highest piece (world height) and tallest piece (above the ground below it) with
// PlayerProfile.SetStat, only when it beats the old record. Each new record gets a message.

using HarmonyLib;

namespace PeakFinder
{
    [HarmonyPatch(typeof(PlayerProfile), nameof(PlayerProfile.SetStat))]
    internal static class HeightMessages
    {
        private static void Postfix(PlayerProfile __instance, PlayerStatType stat, float amount)
        {
            if (!PeakFinderPlugin.HeightMessages.Value || Player.m_localPlayer == null ||
                __instance != Game.instance?.GetPlayerProfile())
                return;

            string text;
            if (stat == PlayerStatType.MaxBuildingHeightWorld)
                text = Line("Highest build so far", amount, "", "Top of the World", PeakFinderPlugin.TopOfTheWorld);
            else if (stat == PlayerStatType.MaxBuildingHeight)
                text = Line("Tallest build so far", amount, " above the ground", "Mighty Halls", PeakFinderPlugin.MightyHalls);
            else
                return;
            Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, text);
        }

        private static string Line(string what, float height, string where, string achievement, float needed) =>
            height >= needed
                ? $"{what}: {height:0} m{where}, high enough for {achievement}!"
                : $"{what}: {height:0} m{where} ({achievement} needs {needed:0} m)";
    }
}
