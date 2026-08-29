using UnityEngine;

namespace Blackzone.Weapons
{
    public enum WeaponClass { AssaultRifle, SMG, Shotgun, MarksmanRifle }
    public enum FireMode { SemiAuto, FullAuto }

    /// <summary>
    /// Data-driven weapon definition. One ScriptableObject per weapon.
    /// Created at runtime from the code catalog (WeaponCatalog) when no
    /// designer asset exists; designer assets in Resources/Weapons override.
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponDefinition", menuName = "Blackzone/Weapon Definition")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string weaponId = "weapon";
        public string displayName = "Weapon";
        public WeaponClass weaponClass = WeaponClass.AssaultRifle;

        [Header("Damage")]
        public float damage = 24f;
        public float headshotMultiplier = 1.8f;

        [Header("Firing")]
        public FireMode fireMode = FireMode.FullAuto;
        public float roundsPerMinute = 620f;
        public int magazineSize = 30;
        public int reserveAmmo = 90;
        public float reloadTime = 2.1f;

        [Header("Aiming")]
        public float adsSpeed = 8f;       // ads blend speed
        public float adsFov = 55f;

        [Header("Accuracy (degrees of spread)")]
        public float hipSpreadDegrees = 2.4f;
        public float adsSpreadDegrees = 0.4f;
        public float range = 120f;
        public int pelletsPerShot = 1;    // shotgun: 8
        public float pelletSpreadDegrees = 0f;

        [Header("Recoil (degrees)")]
        public float recoilVertical = 1.1f;
        public float recoilHorizontalMin = -0.4f;
        public float recoilHorizontalMax = 0.4f;
        public float recoilRecovery = 7f; // degrees/sec back to aim point

        [Header("Viewmodel")]
        public float kickAmount = 0.03f;  // local z kick on fire
        public float audioPitch = 1f;
        public Color accentColor = Color.gray;
        public int slotIndex = 0;
    }
}
