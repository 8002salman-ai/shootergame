using Blackzone.Input;
using Blackzone.Settings;
using Blackzone.Utilities;
using UnityEngine;

namespace Blackzone.Player
{
    /// <summary>
    /// Camera look with weapon sway: yaw on parent rig, pitch on camera,
    /// sensitivity + ADS scaling, recoil with recovery, FOV blending,
    /// and new: movement-based weapon sway, look-sway response, and
    /// sprint bob for tactical FPS feel.
    /// </summary>
    public sealed class FpsLook : MonoBehaviour
    {
        [SerializeField, Range(1f, 89f)] private float pitchLimit = 88f;

        private Transform rig;
        private float yaw;
        private float pitch;
        private float basePitch;
        private float recoilPitch;
        private float recoilYaw;
        private float recoilRecovery = 7f;

        private float adsAmount;
        private float adsFov = 55f;
        private bool sprinting;

        private Camera cam;

        // Weapon sway state
        private float swayX;
        private float swayY;
        private float swayYaw;
        private float swayPitch;
        private Vector3 lastMouseDelta;

        // Sway parameters
        private const float SwayIntensity = 0.004f;
        private const float SwaySmoothing = 8f;
        private const float LookSwayIntensity = 0.003f;
        private const float SprintSwayAmount = 0.008f;
        private const float SprintSwaySpeed = 6f;
        private float sprintSwayPhase;

        // Head bob
        private float bobPhase;
        private const float BobFrequency = 9f;
        private const float BobAmountHorizontal = 0.003f;
        private const float BobAmountVertical = 0.005f;

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

            // Recoil recovery
            float recovery = recoilRecovery * Time.deltaTime;
            recoilPitch = Mathf.MoveTowards(recoilPitch, 0f, recovery);
            recoilYaw = Mathf.MoveTowards(recoilYaw, 0f, recovery);

            pitch = Mathf.Clamp(basePitch + recoilPitch, -pitchLimit, pitchLimit);

            rig.rotation = Quaternion.Euler(0f, yaw, 0f);
            transform.localRotation = Quaternion.Euler(pitch, recoilYaw, 0f);

            // FOV
            float baseFov = GameConstants.BaseFov;
            float targetFov = Mathf.Lerp(baseFov, adsFov, adsAmount);
            if (sprinting && adsAmount < 0.1f) targetFov += 6f;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, 12f * Time.deltaTime);

            // Weapon sway (applied from WeaponRuntime)
            UpdateSway(delta);
        }

        /// <summary>Called by WeaponRuntime each frame to apply sway offset.</summary>
        public Vector3 GetCurrentSway()
        {
            return new Vector3(swayX, swayY, 0f);
        }

        /// <summary>Called by WeaponRuntime each frame to apply sway rotation.</summary>
        public Quaternion GetCurrentSwayRotation()
        {
            return Quaternion.Euler(swayPitch, swayYaw, 0f);
        }

        private void UpdateSway(Vector2 mouseDelta)
        {
            float dt = Time.deltaTime;
            float adsInverse = 1f - adsAmount; // sway reduces during ADS

            // Mouse-look sway: weapon lags behind camera rotation
            float targetSwayYaw = -mouseDelta.x * LookSwayIntensity * adsInverse;
            float targetSwayPitch = mouseDelta.y * LookSwayIntensity * adsInverse;
            swayYaw = Mathf.Lerp(swayYaw, targetSwayYaw, SwaySmoothing * dt);
            swayPitch = Mathf.Lerp(swayPitch, targetSwayPitch, SwaySmoothing * dt);

            // Movement sway: based on input
            Vector2 input = GameInput.Move;
            float targetSwayX = -input.x * SwayIntensity * adsInverse;
            float targetSwayY = input.y * SwayIntensity * 0.5f * adsInverse;
            swayX = Mathf.Lerp(swayX, targetSwayX, SwaySmoothing * dt);
            swayY = Mathf.Lerp(swayY, targetSwayY, SwaySmoothing * dt);

            // Sprint sway: exaggerated bob
            if (sprinting && input.y > 0.5f)
            {
                sprintSwayPhase += SprintSwaySpeed * dt;
                float sprintBobX = Mathf.Sin(sprintSwayPhase) * SprintSwayAmount * adsInverse;
                float sprintBobY = Mathf.Abs(Mathf.Sin(sprintSwayPhase * 0.5f)) * SprintSwayAmount * 0.5f * adsInverse;
                swayX += sprintBobX;
                swayY += sprintBobY;
            }
            else
            {
                sprintSwayPhase = 0f;
            }

            // Head bob (subtle, only when moving on ground)
            bool isMoving = input.sqrMagnitude > 0.01f;
            if (isMoving && !sprinting)
            {
                bobPhase += BobFrequency * dt;
                swayX += Mathf.Cos(bobPhase) * BobAmountHorizontal * adsInverse;
                swayY += Mathf.Sin(bobPhase * 2f) * BobAmountVertical * adsInverse;
            }
        }

        public void SetWeaponViewState(float ads, float fov, bool isSprinting)
        {
            adsAmount = ads;
            adsFov = fov;
            sprinting = isSprinting;
        }

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
            swayX = 0f;
            swayY = 0f;
            swayYaw = 0f;
            swayPitch = 0f;
            sprintSwayPhase = 0f;
            bobPhase = 0f;
        }
    }
}
