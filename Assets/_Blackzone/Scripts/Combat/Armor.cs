using UnityEngine;

namespace Blackzone.Combat
{
    /// <summary>
    /// Phase 1 armor: a simple durability pool that absorbs a configurable
    /// percentage of incoming damage while it has capacity left.
    /// </summary>
    public class Armor : MonoBehaviour
    {
        [SerializeField] private float maxCapacity = 50f;
        [SerializeField, Range(0f, 0.9f)] private float absorbPercent = 0.5f;

        private float current;

        public float Current => current;
        public float Max => maxCapacity;

        public void Initialize(float capacity, float absorb)
        {
            maxCapacity = capacity;
            absorbPercent = absorb;
            current = capacity;
        }

        public void RestoreFull()
        {
            current = maxCapacity;
        }

        /// <summary>Returns damage that passes through after absorption.</summary>
        public float Absorb(float incoming)
        {
            if (current <= 0f || incoming <= 0f) return incoming;

            float absorbed = Mathf.Min(current, incoming * absorbPercent);
            current -= absorbed;
            return incoming - absorbed;
        }
    }
}
