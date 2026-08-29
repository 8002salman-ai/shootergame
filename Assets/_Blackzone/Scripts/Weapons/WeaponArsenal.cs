using Blackzone.Core;
using Blackzone.Input;
using Blackzone.Player;
using UnityEngine;

namespace Blackzone.Weapons
{
    /// <summary>
    /// Owns the player's loadout: weapon switching, fire input arbitration,
    /// ADS state and per-frame weapon ticking. All weapons share one reusable
    /// runtime (WeaponRuntime) driven by data (WeaponDefinition).
    /// </summary>
    public sealed class WeaponArsenal : MonoBehaviour
    {
        private WeaponRuntime[] weapons;
        private int activeIndex;
        private bool switching;
        private float switchTimer = 0.25f;
        private Camera cam;
        private FpsLook look;
        private FpsMovement movement;
        private Transform viewRoot;

        public WeaponRuntime Active => weapons != null && weapons.Length > 0 ? weapons[activeIndex] : null;
        public int ActiveIndex => activeIndex;
        public int Count => weapons != null ? weapons.Length : 0;

        public void Initialize(WeaponDefinition[] defs, Transform viewmodelRoot, Camera camera,
            FpsLook lookSystem, FpsMovement moveSystem)
        {
            cam = camera;
            look = lookSystem;
            movement = moveSystem;
            viewRoot = viewmodelRoot;

            WeaponFx.EnsureInit();

            weapons = new WeaponRuntime[defs.Length];
            for (int i = 0; i < defs.Length; i++)
            {
                weapons[i] = new WeaponRuntime(defs[i], viewRoot, cam, look);
                weapons[i].SetActive(i == 0);
            }
            activeIndex = 0;
            look.SetRecoveryRate(Active.Def.recoilRecovery);
            GameEvents.EmitWeaponSwitched(activeIndex);
            GameEvents.EmitAmmoChanged(Active.MagazineAmmo, Active.ReserveAmmo);
        }

        private void Update()
        {
            if (weapons == null || weapons.Length == 0) return;
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

            float dt = Time.deltaTime;
            var active = Active;

            // Weapon switch progress
            if (switching)
            {
                switchTimer -= dt;
                if (switchTimer <= 0f)
                {
                    switching = false;
                    active.SetActive(true);
                    GameEvents.EmitWeaponSwitched(activeIndex);
                }
            }

            // Reload request
            if (GameInput.ReloadPressed) active.TryStartReload();

            // Switch requests
            int slot = GameInput.WeaponSlotRequested;
            if (slot >= 0 && slot < weapons.Length) RequestSwitch(slot);
            if (GameInput.PrevNextRequest != 0)
                RequestSwitch(activeIndex + (GameInput.PrevNextRequest > 0 ? 1 : -1));

            // ADS
            bool ads = GameInput.AdsHeld && !active.IsReloading && !switching;
            active.SetAdsWanted(ads);

            // Fire
            if (!switching && !active.IsReloading)
            {
                bool wantsFire = active.Def.fireMode == FireMode.FullAuto
                    ? GameInput.FireHeld
                    : GameInput.FirePressed;
                if (wantsFire && active != null) active.TryFire();
            }

            active.Tick(dt);
            look.SetWeaponViewState(active.AdsAmount, active.Def.adsFov,
                movement != null && movement.IsSprinting);
        }

        public void RequestSwitch(int slot)
        {
            if (weapons == null || weapons.Length == 0) return;
            slot = ((slot % weapons.Length) + weapons.Length) % weapons.Length;
            if (slot == activeIndex || switching) return;

            Active.SetAdsWanted(false);
            Active.SetActive(false);
            activeIndex = slot;
            switching = true;
            switchTimer = 0.25f;
            look.SetRecoveryRate(Active.Def.recoilRecovery);
            GameEvents.EmitAmmoChanged(Active.MagazineAmmo, Active.ReserveAmmo);
        }

        public void RestockAll()
        {
            if (weapons == null) return;
            for (int i = 0; i < weapons.Length; i++)
            {
                weapons[i].Restock();
                weapons[i].SetActive(i == activeIndex);
            }
            switching = false;
            GameEvents.EmitAmmoChanged(Active.MagazineAmmo, Active.ReserveAmmo);
            GameEvents.EmitWeaponSwitched(activeIndex);
        }
    }
}
