using Blackzone.Utilities;
using UnityEngine;

namespace Blackzone.Combat
{
    /// <summary>
    /// Static hit-scan service. Both the player's weapons and enemy AI route
    /// their shots through here so hit feedback and damage rules stay in one
    /// place. Hitscan is used for Phase 1 responsiveness (no projectiles).
    /// </summary>
    public static class Ballistics
    {
        /// <summary>
        /// Fires a player shot. Applies damage to the first Health target hit.
        /// </summary>
        /// <returns>True if anything was hit; out params describe the result.</returns>
        public static bool FirePlayerRay(Vector3 origin, Vector3 direction, float range,
            Weapons.WeaponDefinition def, out RaycastHit hit, out bool killedEnemy, out bool headshot)
        {
            killedEnemy = false;
            headshot = false;
            hit = default;

            if (!Physics.Raycast(origin, direction, out hit, range, GameConstants.PlayerFireMask,
                    QueryTriggerInteraction.Ignore))
                return false;

            var target = hit.collider.GetComponentInParent<Health>();
            if (target == null) return true; // world geometry hit, no damage

            var region = hit.collider.GetComponent<HitRegion>();
            headshot = region != null && region.IsHead;

            float damage = def.damage * (headshot ? def.headshotMultiplier : 1f);
            target.ApplyDamage(new DamageInfo(damage, DamageSource.Player, headshot, def.displayName));
            killedEnemy = target.IsDead;
            return true;
        }

        /// <summary>Enemy AI shot. Damage comes from the difficulty profile.</summary>
        public static bool FireEnemyRay(Vector3 origin, Vector3 direction, float range,
            float damage, out RaycastHit hit, out bool killedPlayer, out bool headshot)
        {
            killedPlayer = false;
            headshot = false;
            hit = default;

            if (!Physics.Raycast(origin, direction, out hit, range, GameConstants.EnemyFireMask,
                    QueryTriggerInteraction.Ignore))
                return false;

            var target = hit.collider.GetComponentInParent<Health>();
            if (target == null) return true;

            var region = hit.collider.GetComponent<HitRegion>();
            headshot = region != null && region.IsHead;
            float final = headshot ? damage * 1.5f : damage;

            target.ApplyDamage(new DamageInfo(final, DamageSource.Enemy, headshot, "AI"));
            killedPlayer = target.IsDead;
            return true;
        }
    }
}
