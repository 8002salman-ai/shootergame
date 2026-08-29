using UnityEngine;

namespace Blackzone.Input
{
    /// <summary>
    /// Android touch input. The touch UI (joystick, look area, buttons) pushes
    /// raw values into this provider; Sample() converts them into the unified
    /// held/edge-triggered snapshot consumed by GameInput.
    /// </summary>
    public sealed class MobileInputProvider : IInputProvider
    {
        private Vector2 move;
        private Vector2 lookAccum;
        private bool fireHeld;
        private bool adsHeld;

        private bool firePressed;
        private bool reloadPressed;
        private bool jumpPressed;
        private bool crouchPressed;
        private bool pausePressed;
        private int slotRequested = -1;
        private int prevNext;

        private readonly bool[] held = new bool[7];      // indexed by MobileButton
        private readonly bool[] prevHeld = new bool[7];

        public Vector2 Move => move;
        public Vector2 LookDelta => lookAccum;
        public bool FireHeld => fireHeld;
        public bool AdsHeld => adsHeld;

        // --- UI -> provider ---
        public void SetMove(Vector2 value) => move = value;
        public void SetLook(Vector2 delta) => lookAccum += delta;
        public void SetButton(MobileButton button, bool down) => held[(int)button] = down;
        public void SetPauseFlag() => pausePressed = true;

        public void Sample()
        {
            fireHeld = held[(int)MobileButton.Fire];
            adsHeld = held[(int)MobileButton.Ads];

            firePressed = fireHeld && !prevHeld[(int)MobileButton.Fire];
            reloadPressed = held[(int)MobileButton.Reload] && !prevHeld[(int)MobileButton.Reload];
            jumpPressed = held[(int)MobileButton.Jump] && !prevHeld[(int)MobileButton.Jump];
            crouchPressed = held[(int)MobileButton.Crouch] && !prevHeld[(int)MobileButton.Crouch];
            prevNext = 0;
            if (held[(int)MobileButton.PrevWeapon] && !prevHeld[(int)MobileButton.PrevWeapon]) prevNext = -1;
            else if (held[(int)MobileButton.NextWeapon] && !prevHeld[(int)MobileButton.NextWeapon]) prevNext = 1;

            for (int i = 0; i < held.Length; i++) prevHeld[i] = held[i];

            // look delta is only meaningful while a look touch is active; it is
            // cleared by the control panel when the finger lifts.
            if (!HasLookTouch) lookAccum = Vector2.zero;
        }

        public void ClearLook()
        {
            lookAccum = Vector2.zero;
            HasLookTouch = false;
        }

        public bool HasLookTouch { get; private set; }

        public void BeginLook()
        {
            HasLookTouch = true;
            lookAccum = Vector2.zero;
        }

        public bool ConsumeFirePressed() { bool v = firePressed; firePressed = false; return v; }
        public bool ConsumeReloadPressed() { bool v = reloadPressed; reloadPressed = false; return v; }
        public bool ConsumeJumpPressed() { bool v = jumpPressed; jumpPressed = false; return v; }
        public bool ConsumeCrouchPressed() { bool v = crouchPressed; crouchPressed = false; return v; }
        public bool ConsumePausePressed() { bool v = pausePressed; pausePressed = false; return v; }
        public int ConsumeWeaponSlotRequested() { int v = slotRequested; slotRequested = -1; return v; }
        public int ConsumePrevNextRequest() { int v = prevNext; prevNext = 0; return v; }
    }
}
