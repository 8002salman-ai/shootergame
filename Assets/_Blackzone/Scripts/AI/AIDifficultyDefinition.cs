using UnityEngine;

namespace Blackzone.AI
{
    /// <summary>
    /// Data-driven AI difficulty profile. ROOKIE and SOLDIER assets ship in
    /// Resources/AI; the architecture supports adding ELITE/BOSS profiles later
    /// without code changes.
    /// </summary>
    [CreateAssetMenu(fileName = "AIDifficulty", menuName = "Blackzone/AI Difficulty")]
    public sealed class AIDifficultyDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string difficultyId = "rookie";

        [Header("Combat stats")]
        public float health = 80f;
        public float moveSpeed = 3.2f;
        public float damagePerShot = 9f;

        [Header("Detection")]
        public float detectionRange = 40f;
        public float viewAngle = 100f;       // degrees (centered on facing)
        public float reactionTime = 0.5f;    // seconds before engaging

        [Header("Firing behavior")]
        public float accuracyDegrees = 2.5f; // spread of each AI shot
        public float fireRange = 35f;
        public int burstSize = 4;
        public float burstInterval = 0.11f;  // seconds between shots in a burst
        public float burstCooldown = 1.6f;   // seconds between bursts

        [Header("Repositioning")]
        public float repositionInterval = 3.5f;
        public float repositionRadius = 8f;

        [Header("Tracking")]
        public float loseTargetTime = 4f;    // time without LOS before giving up
        public float searchTime = 5f;

        [Header("Patrol")]
        public float patrolWaitMin = 1f;
        public float patrolWaitMax = 3f;
    }
}
