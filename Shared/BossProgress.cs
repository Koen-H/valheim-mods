// Shared by several mods (linked into each csproj): has the player defeated a boss?
//
// A boss counts as defeated when this character has killed it on any difficulty (the profile's kill stats,
// which carry over between worlds), or when the key the game sets on its death is present: the current
// world's global key (a friend got the kill) or the character's own unique key.

using System.Collections.Generic;

namespace Koendertjh.Shared
{
    internal enum Boss
    {
        None,
        Eikthyr,
        Elder,
        Bonemass,
        Moder,
        Yagluth,
        Queen,
        Fader,
        Kall
    }

    internal static class BossProgress
    {
        private static readonly string[] EnemyNames =
        {
            null,
            "$enemy_eikthyr",
            "$enemy_gdking",
            "$enemy_bonemass",
            "$enemy_dragon",
            "$enemy_goblinking",
            "$enemy_seekerqueen",
            "$enemy_fader",
            "$enemy_frozenking_p3"
        };

        // Boss enemy name -> the key its death sets (Character.m_defeatSetGlobalKey), read from the prefabs.
        private static Dictionary<string, string> s_defeatKeys;
        private static ZNetScene s_keysScene;

        internal static string EnemyName(Boss boss) => EnemyNames[(int)boss];

        internal static bool IsDefeated(Boss boss)
        {
            if (boss == Boss.None)
                return true;

            string enemy = EnemyName(boss);
            if (HasKilled(enemy))
                return true;

            string key = DefeatKey(enemy);
            if (string.IsNullOrEmpty(key))
                return false;
            return (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(key)) ||
                   (Player.m_localPlayer != null && Player.m_localPlayer.HaveUniqueKey(key));
        }

        /// <summary>This character killed a creature with this name, on any difficulty.</summary>
        internal static bool HasKilled(string enemyName)
        {
            PlayerProfile profile = Game.instance?.GetPlayerProfile();
            if (profile == null)
                return false;

            foreach (PlayerProfile.PlayerStats stats in profile.m_playerStats)
            {
                if (stats?.m_enemyStats == null)
                    continue;
                foreach (Dictionary<string, float> kills in stats.m_enemyStats)
                {
                    if (kills != null && kills.TryGetValue(enemyName, out float count) && count > 0f)
                        return true;
                }
            }
            return false;
        }

        private static string DefeatKey(string enemyName)
        {
            if (ZNetScene.instance == null)
                return null;

            if (s_defeatKeys == null || s_keysScene != ZNetScene.instance)
            {
                s_defeatKeys = new Dictionary<string, string>();
                s_keysScene = ZNetScene.instance;
                foreach (UnityEngine.GameObject prefab in ZNetScene.instance.m_prefabs)
                {
                    Character character = prefab != null ? prefab.GetComponent<Character>() : null;
                    if (character != null && !string.IsNullOrEmpty(character.m_defeatSetGlobalKey))
                        s_defeatKeys[character.m_name] = character.m_defeatSetGlobalKey;
                }
            }

            s_defeatKeys.TryGetValue(enemyName, out string key);
            return key;
        }
    }
}
