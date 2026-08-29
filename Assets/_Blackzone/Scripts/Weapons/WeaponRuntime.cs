using Blackzone.Audio;
using Blackzone.Combat;
using Blackzone.Core;
using Blackzone.Player;
using UnityEngine;

namespace Blackzone.Weapons
{
    /// <summary>
    /// Runtime state of a single weapon instance: ammo, reload, ADS blend,
    /// recoil kick and per-shot effects. One instance per loadout slot; the
    /// arsenal drives Tick() each frame. Uses the shared ballistics service.
    /// </summary>
    public sealed class WeaponRuntime
    {
        public readonly WeaponDefinition Def;

        private readonly Transform visualRoot;
        private readonly Transform muzzle;
        private readonly Camera cam;
        private readonly FpsLook look;

        private Vector3 hipPos;
        private Vector3 adsPos;
        private Quaternion hipRot;
        private Quaternion adsRot;

        private float fireCooldown;
        private float reloadTimer;
        private bool reloading;
        private float adsAmount;
        private bool adsWanted;
        private float kickZ;
        private float kickRotX;
        private bool dryClicked;

        public int MagazineAmmo { get; private set; }
        public int ReserveAmmo { get; private set; }
        public bool IsReloading => reloading;
        public float ReloadProgress { get; private set; }
        public float AdsAmount => adsAmount;
        public bool AdsWanted => adsWanted;

        public WeaponRuntime(WeaponDefinition def, Transform viewRoot, Camera camera, FpsLook lookSystem)
        {
            Def = def;
            cam = camera;
            look = lookSystem;

            visualRoot = new GameObject(def.displayName + "_Visual").transform;
            visualRoot.SetParent(viewRoot, false);
            muzzle = WeaponVisualFactory.Build(def, visualRoot);

            SetupStancePositions();

            MagazineAmmo = def.magazineSize;
            ReserveAmmo = def.reserveAmmo;
            visualRoot.localPosition = hipPos;
        }

        private void SetupStancePositions()
        {
            switch (Def.weaponClass)
            {
                case WeaponClass.SMG:
                    hipPos = new Vector3(0.20f, -0.19f, 0.52f);
                    adsPos = new Vector3(0f, -0.105f, 0.10f);
                    break;
                case WeaponClass.Shotgun:
                    hipPos = new Vector3(0.27f, -0.21f, 0.50f);
                    adsPos = new Vector3(-0.055f, -0.115f, 0.08f);
                    break;
                case WeaponClass.MarksmanRifle:
                    hipPos = new Vector3(0.24f, -0.20f, 0.50f);
                    adsPos = new Vector3(0f, -0.112f, 0.06f);
                    break;
                default:
                    hipPos = new Vector3(0.22f, -0.20f, 0.52f);
                    adsPos = new Vector3(0f, -0.11f, 0.08f);
                    break;
            }
            hipRot = Quaternion.identity;
            adsRot = Quaternion.identity;
        }

        public bool TryStartReload()
        {
            if (reloading || MagazineAmmo >= Def.magazineSize || ReserveAmmo <= 0) return false;
            reloading = true;
            ReloadProgress = 0f;
            reloadTimer = Def.reloadTime;
            adsWanted = false;
            GameEvents.EmitReloadStarted();
            AudioManager.Instance.Play(AudioId.Reload, 1f);
            return true;
        }

        public void SetAdsWanted(bool wanted)
        {
            adsWanted = wanted;
        }

        public void TryFire()
        {
            if (fireCooldown > 0f) return;

            if (MagazineAmmo <= 0)
            {
                if (!dryClicked)
                {
                    dryClicked = true;
                    AudioManager.Instance.Play(AudioId.Empty, 1f);
                }
                return;
            }
            dryClicked = false;
            fireCooldown = 60f / Def.roundsPerMinute;
            MagazineAmmo--;

            Vector3 camPos = cam.transform.position;
            Vector3 camForward = cam.transform.forward;
            float spread = Mathf.Lerp(Def.hipSpreadDegrees, Def.adsSpreadDegrees, adsAmount);
            float pattern = Def.pelletsPerShot > 1 ? Def.pelletSpreadDegrees : 0f;

            Vector3? lastHit = null;
            for (int i = 0; i < Def.pelletsPerShot; i++)
            {
                Vector3 dir = Spread(camForward, spread, pattern, i);
                if (Ballistics.FirePlayerRay(camPos, dir, Def.range, Def, out RaycastHit hit,
                        out bool killedEnemy, out bool headshot))
                {
                    lastHit = hit.point;
                    if (hit.collider.GetComponentInParent<Health>() != null)
                    {
                        GameEvents.EmitHitConfirmed();
                        if (killedEnemy)
                        {
                            GameEvents.EmitEnemyKilled();
                            AudioManager.Instance.Play(AudioId.Kill, 1f);
                        }
                    }
                    WeaponFx.SpawnImpact(hit.point, hit.normal);
                }
            }

            // Muzzle + tracer
            Vector3 muzzleWorld = muzzle.position;
            WeaponFx.SpawnMuzzleFlash(muzzleWorld, camForward);
            WeaponFx.SpawnTracer(muzzleWorld, lastHit ?? camPos + camForward * 80f);

            // Audio + recoil + kick
            AudioManager.Instance.Play(AudioId.Fire, Def.audioPitch);
            float adsScale = Mathf.Lerp(1f, 0.75f, adsAmount);
            look.ApplyRecoil(
                Def.recoilVertical * adsScale,
                Random.Range(Def.recoilHorizontalMin, Def.recoilHorizontalMax));
            kickZ = Def.kickAmount;
            kickRotX = Def.recoilVertical * 2.2f;

            GameEvents.EmitAmmoChanged(MagazineAmmo, ReserveAmmo);

            if (MagazineAmmo == 0 && ReserveAmmo > 0) TryStartReload();
        }

        public void Tick(float dt)
        {
            fireCooldown = Mathf.Max(0f, fireCooldown - dt);

            if (reloading)
            {
                reloadTimer -= dt;
                ReloadProgress = Mathf.Clamp01(1f - reloadTimer / Def.reloadTime);
                if (reloadTimer <= 0f)
                {
                    reloading = false;
                    ReloadProgress = 0f;
                    int transfer = Mathf.Min(Def.magazineSize - MagazineAmmo, ReserveAmmo);
                    MagazineAmmo += transfer;
                    ReserveAmmo -= transfer;
                    GameEvents.EmitReloadFinished();
                    GameEvents.EmitAmmoChanged(MagazineAmmo, ReserveAmmo);
                }
            }

            adsAmount = Mathf.MoveTowards(adsAmount, adsWanted ? 1f : 0f, Def.adsSpeed * dt);

            kickZ = Mathf.Lerp(kickZ, 0f, 9f * dt);
            kickRotX = Mathf.Lerp(kickRotX, 0f, 9f * dt);

            Vector3 basePos = Vector3.Lerp(hipPos, adsPos, adsAmount);
            visualRoot.localPosition = basePos + new Vector3(0f, 0f, kickZ);
            visualRoot.localRotation = Quaternion.Slerp(hipRot, adsRot, adsAmount) *
                                       Quaternion.Euler(kickRotX, 0f, 0f);
        }

        public void Restock()
        {
            MagazineAmmo = Def.magazineSize;
            ReserveAmmo = Def.reserveAmmo;
            reloading = false;
            ReloadProgress = 0f;
            adsAmount = 0f;
            adsWanted = false;
            fireCooldown = 0f;
        }

        public void SetActive(bool active)
        {
            visualRoot.gameObject.SetActive(active);
        }

        /// <summary>Applies cone spread (plus a deterministic pellet ring).</summary>
        private static Vector3 Spread(Vector3 forward, float spreadDeg, float patternDeg, int pelletIndex)
        {
            if (spreadDeg <= 0f && patternDeg <= 0f) return forward;

            float jitter = Random.Range(-0.5f, 0.5f);
            float ringDeg = patternDeg > 0f && pelletIndex > 0
                ? patternDeg * Mathf.Sqrt(pelletIndex) + jitter
                : 0f;

            float total = spreadDeg + ringDeg;
            float pitch = Random.Range(-total, total);
            float yaw = Random.Range(-total, total);
            return Quaternion.Euler(pitch, yaw, 0f) * forward;
        }
    }
}
