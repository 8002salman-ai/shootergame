using Blackzone.Input;
using UnityEngine;

namespace Blackzone.Player
{
    /// <summary>
    /// FPS locomotion: walk / sprint / crouch / jump with grounded checks,
    /// coyote time, jump buffering, acceleration and deceleration curves.
    /// Camera pitch happens on FpsLook; this component only moves the capsule
    /// and keeps the camera pivot at the correct eye height.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class FpsMovement : MonoBehaviour
    {
        [Header("Speeds (m/s)")]
        [SerializeField] private float walkSpeed = 4.2f;
        [SerializeField] private float sprintSpeed = 6.4f;
        [SerializeField] private float crouchSpeed = 2.1f;

        [Header("Feel")]
        [SerializeField] private float acceleration = 42f;
        [SerializeField] private float deceleration = 30f;
        [SerializeField] private float airAcceleration = 14f;
        [SerializeField] private float gravity = -24f;
        [SerializeField] private float jumpHeight = 1.15f;
        [SerializeField] private float coyoteTime = 0.15f;
        [SerializeField] private float jumpBufferTime = 0.12f;

        [Header("Stance")]
        [SerializeField] private float standHeight = 1.8f;
        [SerializeField] private float crouchHeight = 1.15f;
        [SerializeField] private float standEyeHeight = 1.62f;
        [SerializeField] private float crouchEyeHeight = 1.05f;
        [SerializeField] private float stanceLerpSpeed = 12f;

        [Header("Head bob")]
        [SerializeField] private float bobFrequency = 9f;
        [SerializeField] private float bobAmount = 0.035f;

        private CharacterController controller;
        private Vector3 velocity;
        private float timeSinceGrounded;
        private float jumpBufferTimer;
        private float bobPhase;

        public Transform CameraPivot { get; private set; }

        public bool IsGrounded => controller != null && controller.isGrounded;
        public bool IsCrouching { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsMoving { get; private set; }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            var pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(transform, false);
            pivot.transform.localPosition = new Vector3(0f, standEyeHeight, 0f);
            CameraPivot = pivot.transform;
        }

        public void UpdateMovement()
        {
            float dt = Time.deltaTime;

            // --- Intent ---
            Vector2 input = GameInput.Move;
            Vector3 wishDir = transform.right * input.x + transform.forward * input.y;
            bool hasInput = wishDir.sqrMagnitude > 0.01f;
            if (hasInput) wishDir.Normalize();

            bool wantsSprint = input.y > 0.5f && !GameInput.AdsHeld && !IsCrouching && controller.isGrounded;
            IsSprinting = wantsSprint && hasInput;
            IsMoving = hasInput && controller.isGrounded;

            float targetSpeed = IsCrouching ? crouchSpeed : (IsSprinting ? sprintSpeed : walkSpeed);
            Vector3 targetVel = wishDir * targetSpeed;

            // --- Accelerate / decelerate ---
            float accel = controller.isGrounded ? (hasInput ? acceleration : deceleration) : airAcceleration;
            velocity.x = Mathf.MoveTowards(velocity.x, targetVel.x, accel * dt);
            velocity.z = Mathf.MoveTowards(velocity.z, targetVel.z, accel * dt);

            // --- Gravity + jumping ---
            if (controller.isGrounded) timeSinceGrounded = 0f;
            else timeSinceGrounded += dt;

            if (GameInput.JumpPressed) jumpBufferTimer = jumpBufferTime;
            else jumpBufferTimer = Mathf.Max(0f, jumpBufferTimer - dt);

            if (jumpBufferTimer > 0f && (controller.isGrounded || timeSinceGrounded < coyoteTime))
            {
                velocity.y = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight);
                jumpBufferTimer = 0f;
                timeSinceGrounded = coyoteTime; // consume coyote
            }

            if (controller.isGrounded && velocity.y < 0f) velocity.y = -2f;
            velocity.y += gravity * dt;

            controller.Move(velocity * dt);

            // --- Crouch toggle ---
            if (GameInput.CrouchPressed)
            {
                IsCrouching = !IsCrouching;
                if (IsCrouching) IsSprinting = false;
            }

            // --- Stance (controller height + eye height) ---
            float targetHeight = IsCrouching ? crouchHeight : standHeight;
            if (!Mathf.Approximately(controller.height, targetHeight))
            {
                controller.height = Mathf.Lerp(controller.height, targetHeight, stanceLerpSpeed * dt);
                controller.center = new Vector3(0f, controller.height * 0.5f, 0f);
            }

            // --- Head bob ---
            float speedFactor = IsCrouching ? 0.5f : 1f;
            if (IsMoving && !IsCrouching) bobPhase += bobFrequency * dt * speedFactor * (IsSprinting ? 1.35f : 1f);
            else bobPhase = Mathf.Lerp(bobPhase, 0f, dt * 6f);

            float bobY = Mathf.Sin(bobPhase) * bobAmount * speedFactor;
            float bobX = Mathf.Cos(bobPhase * 0.5f) * bobAmount * 0.5f * speedFactor;
            float speedScale = Mathf.Clamp01(velocity.magnitude / walkSpeed);
            bobX *= speedScale;
            bobY *= speedScale;

            float targetEye = IsCrouching ? crouchEyeHeight : standEyeHeight;
            float eyeY = Mathf.Lerp(CameraPivot.localPosition.y, targetEye, stanceLerpSpeed * dt);

            CameraPivot.localPosition = new Vector3(bobX, eyeY + bobY, 0f);
        }

        public void ResetState()
        {
            velocity = Vector3.zero;
            IsCrouching = false;
            IsSprinting = false;
            jumpBufferTimer = 0f;
            bobPhase = 0f;
            controller.height = standHeight;
            controller.center = new Vector3(0f, standHeight * 0.5f, 0f);
            if (CameraPivot != null)
                CameraPivot.localPosition = new Vector3(0f, standEyeHeight, 0f);
        }
    }
}
