using System;
using UnityEngine;

namespace Blackzone.Combat
{
    /// <summary>
    /// Simple health pool. Attached to the player rig and to every enemy root.
    /// Damage flows through Armor (if present) before reaching health.
    /// </summary>
    public class Health : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;

        private float current;
        private bool isDead;

        public event Action<Health, float> Damaged; // health, damage dealt
        public event Action<Health> Died;

        public float Current => current;
        public float Max => maxHealth;
        public bool IsDead => isDead;

        public void Initialize(float max)
        {
            maxHealth = max;
            current = max;
            isDead = false;
        }

        public void Revive()
        {
            current = maxHealth;
            isDead = false;
        }

        /// <summary>Applies damage after armor absorption. Returns damage actually dealt.</summary>
        public float ApplyDamage(DamageInfo info)
        {
            if (isDead || info.Damage <= 0f) return 0f;

            float dealt = info.Damage;
            var armor = GetComponent<Armor>();
            if (armor != null && armor.Current > 0f)
            {
                dealt = armor.Absorb(dealt);
            }

            current = Mathf.Max(0f, current - dealt);
            Damaged?.Invoke(this, dealt);

            if (current <= 0f && !isDead)
            {
                isDead = true;
                Died?.Invoke(this);
            }
            return dealt;
        }

        public void Heal(float amount)
        {
            if (isDead) return;
            current = Mathf.Min(maxHealth, current + amount);
            Damaged?.Invoke(this, -amount);
        }
    }
}
