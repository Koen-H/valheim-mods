using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace SummonTweaks
{
    /// <summary>Gives every summon a <see cref="PlayerPassThrough"/>.</summary>
    [HarmonyPatch(typeof(Character), "Awake")]
    internal static class SummonPatches
    {
        [HarmonyPostfix]
        private static void Postfix(Character __instance)
        {
            // Summons are the tamed creatures that disappear when their owner walks away or logs out.
            // Tamed wolves, lox etc. don't, and the Trollstav troll is not tamed at all.
            Tameable tameable = __instance.GetComponent<Tameable>();
            if (tameable == null || (tameable.m_unsummonDistance <= 0f && tameable.m_unsummonOnOwnerLogoutSeconds <= 0f)) return;
            __instance.gameObject.AddComponent<PlayerPassThrough>();
        }
    }

    /// <summary>
    /// Turns off the physical collision between a summon and every player. Only this pair of colliders is
    /// affected: melee hits and projectiles use overlap checks and raycasts, which ignore this setting.
    /// </summary>
    internal class PlayerPassThrough : MonoBehaviour
    {
        // Players join, respawn (a new collider) and deactivate, which resets the ignore state, so re-apply it.
        private const float Interval = 1f;

        private Collider[] _colliders;
        private string _prefabName;
        private float _timer;

        private void Awake()
        {
            _colliders = GetComponentsInChildren<Collider>().Where(c => !c.isTrigger).ToArray();
            _prefabName = gameObject.name.Replace("(Clone)", "").Trim();
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = Interval;

            // Also applied when switched off, so changing the config in game takes effect.
            bool ignore = SummonTweaksPlugin.WalkThroughSummons.Value && !SummonTweaksPlugin.IsSolid(_prefabName);
            foreach (Player player in Player.GetAllPlayers())
            {
                Collider playerCollider = player != null ? player.GetCollider() : null;
                if (playerCollider == null || !playerCollider.enabled) continue;
                foreach (Collider collider in _colliders)
                {
                    if (collider != null && collider.enabled && collider.gameObject.activeInHierarchy)
                        Physics.IgnoreCollision(collider, playerCollider, ignore);
                }
            }
        }
    }
}
