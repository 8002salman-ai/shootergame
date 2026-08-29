using UnityEngine;

namespace Blackzone.Input
{
    /// <summary>
    /// Static facade over the active IInputProvider. Gameplay code reads this
    /// class only. Also exposes mobile-facing setters used by the touch UI.
    /// </summary>
    public static class GameInput
    {
        private static IInputProvider provider;
        private static bool mobile;

        public static bool IsMobile => mobile;
        public static bool Enabled { get; set; } = true;

        public static Vector2 Move => Enabled ? provider.Move : Vector2.zero;
        public static Vector2 LookDelta => Enabled ? provider.LookDelta : Vector2.zero;
        public static bool FireHeld => Enabled && provider.FireHeld;
        public static bool AdsHeld => Enabled && provider.AdsHeld;

        public static bool FirePressed { get; private set; }
        public static bool ReloadPressed { get; private set; }
        public static bool JumpPressed { get; private set; }
        public static bool CrouchPressed { get; private set; }
        public static bool PausePressed { get; private set; }
        public static int WeaponSlotRequested { get; private set; } = -1;
        public static int PrevNextRequest { get; private set; }

        public static void Initialize(bool isMobile)
        {
            mobile = isMobile;
            provider = isMobile ? (IInputProvider)new MobileInputProvider() : new DesktopInputProvider();
        }

        /// <summary>Refreshes the snapshot. Driven once per frame in LateUpdate.</summary>
        public static void UpdateFrame()
        {
            if (provider == null) return;

            provider.Sample();
            FirePressed = provider.ConsumeFirePressed();
            ReloadPressed = provider.ConsumeReloadPressed();
            JumpPressed = provider.ConsumeJumpPressed();
            CrouchPressed = provider.ConsumeCrouchPressed();
            PausePressed = provider.ConsumePausePressed();
            WeaponSlotRequested = provider.ConsumeWeaponSlotRequested();
            PrevNextRequest = provider.ConsumePrevNextRequest();
        }

        // --- Mobile setters (called by the touch control panel) ---
        public static void SetMobileMove(Vector2 move) => (provider as MobileInputProvider)?.SetMove(move);
        public static void SetMobileLook(Vector2 delta) => (provider as MobileInputProvider)?.SetLook(delta);
        public static void SetMobileButton(MobileButton button, bool down) =>
            (provider as MobileInputProvider)?.SetButton(button, down);
        public static void SetMobilePause() => (provider as MobileInputProvider)?.SetPauseFlag();
    }

    public enum MobileButton
    {
        Fire,
        Ads,
        Reload,
        Jump,
        Crouch,
        PrevWeapon,
        NextWeapon
        ,None
    }
}
