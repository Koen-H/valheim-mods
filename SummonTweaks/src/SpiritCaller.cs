using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace SummonTweaks
{
    /// <summary>
    /// SpawnAbility.Spawn picks its creature with <c>m_spawnPrefab[Random.Range(0, m_spawnPrefab.Length)]</c>.
    /// That Random.Range call is swapped for <see cref="PickIndex"/>, which only rolls creatures the caster
    /// can still add without one of theirs being unsummoned.
    /// </summary>
    [HarmonyPatch]
    internal static class SpiritCallerPatches
    {
        private static readonly AccessTools.FieldRef<SpawnAbility, Character> s_owner =
            AccessTools.FieldRefAccess<SpawnAbility, Character>("m_owner");
        private static readonly AccessTools.FieldRef<SpawnAbility, ItemDrop.ItemData> s_weapon =
            AccessTools.FieldRefAccess<SpawnAbility, ItemDrop.ItemData>("m_weapon");

        private static MethodBase TargetMethod() => AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(SpawnAbility), "Spawn"));

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo spawnPrefab = AccessTools.Field(typeof(SpawnAbility), nameof(SpawnAbility.m_spawnPrefab));
            MethodInfo range = AccessTools.Method(typeof(Random), nameof(Random.Range), new[] { typeof(int), typeof(int) });

            var matcher = new CodeMatcher(instructions)
                .MatchStartForward(
                    new CodeMatch(_ => true),
                    new CodeMatch(OpCodes.Ldfld, spawnPrefab),
                    new CodeMatch(OpCodes.Ldlen),
                    new CodeMatch(OpCodes.Conv_I4),
                    new CodeMatch(OpCodes.Call, range),
                    new CodeMatch(OpCodes.Ldelem_Ref));
            if (matcher.IsInvalid)
            {
                SummonTweaksPlugin.Log.LogWarning("SpawnAbility.Spawn has changed; SmartSpiritCaller is disabled.");
                return matcher.InstructionEnumeration();
            }

            // The first matched instruction loads the SpawnAbility; load it again as PickIndex's third argument.
            CodeInstruction loadAbility = matcher.Instruction;
            return matcher
                .Advance(4)
                .InsertAndAdvance(new CodeInstruction(loadAbility.opcode, loadAbility.operand))
                .SetInstruction(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SpiritCallerPatches), nameof(PickIndex))))
                .InstructionEnumeration();
        }

        private static int PickIndex(int min, int max, SpawnAbility ability)
        {
            // Only player staffs with a choice of creatures; enemies that summon keep their own behaviour.
            if (!SummonTweaksPlugin.SmartSpiritCaller.Value || max - min <= 1 || !(s_owner(ability) is Player player))
                return Random.Range(min, max);

            int maxInstances = GetMaxInstances(ability, player);
            var free = new List<int>();
            for (int i = min; i < max; i++)
            {
                if (CanAdd(ability, ability.m_spawnPrefab[i], player, maxInstances)) free.Add(i);
            }
            // Everything at its limit: let the game roll and replace one as usual.
            return free.Count > 0 ? free[Random.Range(0, free.Count)] : Random.Range(min, max);
        }

        private static bool CanAdd(SpawnAbility ability, GameObject prefab, Player player, int maxInstances)
        {
            if (prefab == null) return false;
            // The game's hard cap on the number of this creature in the area.
            if (ability.m_maxSpawned > 0 && SpawnSystem.GetNrOfInstances(prefab, Vector3.zero, 0f) >= ability.m_maxSpawned)
                return false;
            if (maxInstances <= 0) return true;

            // Tameable.UnsummonMaxInstances: a player keeps at most maxInstances of a creature with the same name.
            Character character = prefab.GetComponent<Character>();
            if (character == null) return true;
            string playerName = player.GetPlayerName();
            int owned = 0;
            foreach (Character other in Character.GetAllCharacters())
            {
                if (other.m_name != character.m_name) continue;
                ZNetView nview = other.GetComponent<ZNetView>();
                ZDO zdo = nview != null && nview.IsValid() ? nview.GetZDO() : null;
                if (zdo != null && zdo.GetString(ZDOVars.s_follow) == playerName) owned++;
            }
            return owned < maxInstances;
        }

        /// <summary>The per-creature limit SpawnAbility.Spawn stores on what it summons (ZDOVars.s_maxInstances).</summary>
        private static int GetMaxInstances(SpawnAbility ability, Player player)
        {
            Skills skills = player.GetSkills();
            if (skills == null || ability.m_levelUpSettings == null) return 0;
            for (int i = ability.m_levelUpSettings.Count - 1; i >= 0; i--)
            {
                SpawnAbility.LevelUpSettings settings = ability.m_levelUpSettings[i];
                if (skills.GetSkillLevel(settings.m_skill) < settings.m_skillLevel) continue;
                ItemDrop.ItemData weapon = s_weapon(ability);
                return ability.m_setMaxInstancesFromWeaponLevel ? (weapon != null ? weapon.m_quality : 0) : settings.m_maxSpawns;
            }
            return 0;
        }
    }
}
