using UnityEngine;

namespace Blackzone.Utilities
{
    /// <summary>Central gameplay constants, tuned once and reused everywhere.</summary>
    public static class GameConstants
    {
        // Player combat
        public const float PlayerMaxHealth = 100f;
        public const float PlayerArmorCapacity = 50f;
        public const float PlayerArmorAbsorb = 0.5f;

        // Player movement feel
        public const float WalkSpeed = 4.2f;
        public const float SprintSpeed = 6.4f;
        public const float CrouchSpeed = 2.1f;
        public const float JumpHeight = 1.15f;

        // Camera
        public const float BaseFov = 72f;
        public const float AdsFov = 55f;

        // Layers (must match ProjectSettings/TagManager.asset)
        public static readonly int LayerPlayer = 3;
        public static readonly int LayerUI = 5;
        public static readonly int LayerWorld = 8;
        public static readonly int LayerEnemy = 9;
        public static readonly int LayerInteractable = 10;

        public static readonly LayerMask WorldMask = 1 << LayerWorld;
        public static readonly LayerMask PlayerFireMask = (1 << LayerWorld) | (1 << LayerEnemy) | (1 << LayerInteractable);
        public static readonly LayerMask EnemyFireMask = (1 << LayerWorld) | (1 << LayerPlayer);
        public static readonly LayerMask EnemyVisionMask = (1 << LayerWorld) | (1 << LayerEnemy);
    }
}
