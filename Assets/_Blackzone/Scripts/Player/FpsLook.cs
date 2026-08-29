using Blackzone.Input;
using Blackzone.Settings;
using Blackzone.Utilities;
using UnityEngine;

namespace Blackzone.Player
{
    /// <summary>
    /// Camera look: yaw on the parent rig, pitch on this camera, vertical
    /// pitch limits, sensitivity + ADS sensitivity scaling, recoil punch with
    /// smooth recovery, and FOV blending for ADS / sprint.
    /// </summary>
    public sealed class FpsLook : MonoBehaviour
    {
        [SerializeField, Range(1f, 89f)] private float pitchLimit = 88f;

        private Transform rig;          // yaw
        private float yaw;
        private float pitch;            // current pitch (base + recoil)
        private float basePitch;
        private float recoilPitch;
        private float recoilYaw;
        private float recoilRecovery = 7f;

        private float adsAmount;
        private float adsFov = 55f;
        private bool sprinting;

        private Camera cam;

        private void Awake()
        {
            rig = transform.parent;
            cam = GetComponent<Camera>();
            yaw = rig.eulerAngles.y;
            pitch = 0f;
        }

        public void UpdateLook()
        {
            Vector2 delta = GameInput.LookDelta;

            float lookScale = GameInput.IsMobile ? 0.22f : 0.10f;
            float sens = GameSettings.Sensitivity;
            float adsSens = GameSettings.AdsSensitivity;

            yaw += delta.x * lookScale * sens;
            basePitch -= delta.y * lookScale * sens * (adsAmount > 0.5f ? adsSens : 1f);

            // --- Recoil recovery (view returns toward the aim point) ---
            float recovery = recoilRecovery * Time.deltaTime;
            recoilPitch = Mathf.MoveTowards(recoilPitch, 0f, recovery);
            recoilYaw = Mathf.MoveTowards(recoilYaw, 0f, recovery);

            pitch = Mathf.Clamp(basePitch + recoilPitch, -pitchLimit, pitchLimit);

            rig.rotation = Quaternion.Euler(0f, yaw, 0f);
            transform.localRotation = Quaternion.Euler(pitch, recoilYaw, 0f);

            // --- FOV ---
            float baseFov = GameConstants.BaseFov;
            float targetFov = Mathf.Lerp(baseFov, adsFov, adsAmount);
            if (sprinting && adsAmount < 0.1f) targetFov += 6f;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, 12f * Time.deltaTime);
        }

        /// <summary>Called by the weapon arsenal each frame.</summary>
        public void SetWeaponViewState(float ads, float fov, bool isSprinting)
        {
            adsAmount = ads;
            adsFov = fov;
            sprinting = isSprinting;
        }

        /// <summary>Recoil punch in degrees (positive pitch = up).</summary>
        public void ApplyRecoil(float verticalDegrees, float horizontalDegrees)
        {
            recoilPitch = Mathf.Clamp(recoilPitch + verticalDegrees, -12f, 12f);
            recoilYaw = Mathf.Clamp(recoilYaw + horizontalDegrees, -8f, 8f);
        }

        public void SetRecoveryRate(float degreesPerSecond)
        {
            recoilRecovery = degreesPerSecond;
        }

        public void ResetView()
        {
            basePitch = 0f;
            recoilPitch = 0f;
            recoilYaw = 0f;
            pitch = 0f;
            transform.localRotation = Quaternion.identity;
            rig.rotation = Quaternion.Euler(0f, yaw, 0f);
            adsAmount = 0f;
        }
    }
}
