using UnityEngine;

namespace Blackzone.Weapons
{
    /// <summary>
    /// Source of truth for weapon definitions. Prefers designer ScriptableObject
    /// assets in Resources/Weapons; falls back to the built-in prototype loadout
    /// so a fresh clone of the repo is always playable without setup steps.
    /// </summary>
    public static class WeaponCatalog
    {
        public static WeaponDefinition[] GetWeaponDefinitions()
        {
            var assets = Resources.LoadAll<WeaponDefinition>("Weapons");
            if (assets != null && assets.Length > 0)
            {
                System.Array.Sort(assets, (a, b) => a.slotIndex.CompareTo(b.slotIndex));
                return assets;
            }
            return CreateDefaultLoadout();
        }

        /// <summary>Original fictional prototype weapons (no real-world brands).</summary>
        private static WeaponDefinition[] CreateDefaultLoadout()
        {
            return new[]
            {
                CreateAssaultRifle(),
                CreateSmg(),
                CreateShotgun(),
                CreateMarksman()
            };
        }

        private static WeaponDefinition CreateAssaultRifle()
        {
            var def = ScriptableObject.CreateInstance<WeaponDefinition>();
            def.weaponId = "kestrel_k17";
            def.displayName = "KESTREL K-17";
            def.weaponClass = WeaponClass.AssaultRifle;
            def.fireMode = FireMode.FullAuto;
            def.damage = 24f;
            def.headshotMultiplier = 1.8f;
            def.roundsPerMinute = 620f;
            def.magazineSize = 30;
            def.reserveAmmo = 90;
            def.reloadTime = 2.1f;
            def.adsSpeed = 9f;
            def.adsFov = 55f;
            def.hipSpreadDegrees = 2.2f;
            def.adsSpreadDegrees = 0.4f;
            def.range = 140f;
            def.recoilVertical = 1.15f;
            def.recoilHorizontalMin = -0.35f;
            def.recoilHorizontalMax = 0.35f;
            def.recoilRecovery = 7f;
            def.kickAmount = 0.028f;
            def.audioPitch = 1f;
            def.accentColor = new Color(0.85f, 0.62f, 0.25f);
            def.slotIndex = 0;
            return def;
        }

        private static WeaponDefinition CreateSmg()
        {
            var def = ScriptableObject.CreateInstance<WeaponDefinition>();
            def.weaponId = "viper_v9";
            def.displayName = "VIPER V-9";
            def.weaponClass = WeaponClass.SMG;
            def.fireMode = FireMode.FullAuto;
            def.damage = 16f;
            def.headshotMultiplier = 1.7f;
            def.roundsPerMinute = 850f;
            def.magazineSize = 32;
            def.reserveAmmo = 128;
            def.reloadTime = 1.8f;
            def.adsSpeed = 11f;
            def.adsFov = 60f;
            def.hipSpreadDegrees = 3.4f;
            def.adsSpreadDegrees = 0.9f;
            def.range = 80f;
            def.recoilVertical = 0.75f;
            def.recoilHorizontalMin = -0.5f;
            def.recoilHorizontalMax = 0.5f;
            def.recoilRecovery = 8f;
            def.kickAmount = 0.022f;
            def.audioPitch = 1.25f;
            def.accentColor = new Color(0.35f, 0.75f, 0.55f);
            def.slotIndex = 1;
            return def;
        }

        private static WeaponDefinition CreateShotgun()
        {
            var def = ScriptableObject.CreateInstance<WeaponDefinition>();
            def.weaponId = "anvil_a12";
            def.displayName = "ANVIL A-12";
            def.weaponClass = WeaponClass.Shotgun;
            def.fireMode = FireMode.SemiAuto;
            def.damage = 11f;
            def.headshotMultiplier = 1.5f;
            def.roundsPerMinute = 80f;
            def.magazineSize = 6;
            def.reserveAmmo = 24;
            def.reloadTime = 2.6f;
            def.adsSpeed = 7f;
            def.adsFov = 52f;
            def.hipSpreadDegrees = 5.5f;
            def.adsSpreadDegrees = 2.6f;
            def.range = 35f;
            def.pelletsPerShot = 8;
            def.pelletSpreadDegrees = 1.2f;
            def.recoilVertical = 2.6f;
            def.recoilHorizontalMin = -0.6f;
            def.recoilHorizontalMax = 0.6f;
            def.recoilRecovery = 5f;
            def.kickAmount = 0.05f;
            def.audioPitch = 0.7f;
            def.accentColor = new Color(0.75f, 0.35f, 0.28f);
            def.slotIndex = 2;
            return def;
        }

        private static WeaponDefinition CreateMarksman()
        {
            var def = ScriptableObject.CreateInstance<WeaponDefinition>();
            def.weaponId = "longbow_lb7";
            def.displayName = "LONGBOW LB-7";
            def.weaponClass = WeaponClass.MarksmanRifle;
            def.fireMode = FireMode.SemiAuto;
            def.damage = 58f;
            def.headshotMultiplier = 2.2f;
            def.roundsPerMinute = 150f;
            def.magazineSize = 10;
            def.reserveAmmo = 40;
            def.reloadTime = 2.4f;
            def.adsSpeed = 6f;
            def.adsFov = 42f;
            def.hipSpreadDegrees = 4.5f;
            def.adsSpreadDegrees = 0.15f;
            def.range = 260f;
            def.recoilVertical = 1.9f;
            def.recoilHorizontalMin = -0.25f;
            def.recoilHorizontalMax = 0.25f;
            def.recoilRecovery = 5f;
            def.kickAmount = 0.04f;
            def.audioPitch = 0.85f;
            def.accentColor = new Color(0.5f, 0.6f, 0.85f);
            def.slotIndex = 3;
            return def;
        }
    }
}
