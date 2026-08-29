using UnityEngine;

namespace Blackzone.Input
{
    /// <summary>
    /// Windows / macOS / Editor input: WASD, mouse look, mouse buttons,
    /// Shift / Ctrl / C / Space / R / 1-4 / Esc.
    /// </summary>
    public sealed class DesktopInputProvider : IInputProvider
    {
        public Vector2 Move { get; private set; }
        public Vector2 LookDelta { get; private set; }
        public bool FireHeld { get; private set; }
        public bool AdsHeld { get; private set; }

        private bool firePressed;
        private bool reloadPressed;
        private bool jumpPressed;
        private bool crouchPressed;
        private bool pausePressed;
        private int slotRequested = -1;
        private int prevNext;

        public void Sample()
        {
            Move = new Vector2(UnityEngine.Input.GetAxisRaw("Horizontal"), UnityEngine.Input.GetAxisRaw("Vertical"));
            LookDelta = new Vector2(UnityEngine.Input.GetAxis("Mouse X"), UnityEngine.Input.GetAxis("Mouse Y"));
            FireHeld = UnityEngine.Input.GetMouseButton(0);
            AdsHeld = UnityEngine.Input.GetMouseButton(1);

            firePressed = UnityEngine.Input.GetMouseButtonDown(0);
            reloadPressed = UnityEngine.Input.GetKeyDown(KeyCode.R);
            jumpPressed = UnityEngine.Input.GetKeyDown(KeyCode.Space);
            crouchPressed = UnityEngine.Input.GetKeyDown(KeyCode.LeftControl) || UnityEngine.Input.GetKeyDown(KeyCode.C);
            pausePressed = UnityEngine.Input.GetKeyDown(KeyCode.Escape) || UnityEngine.Input.GetKeyDown(KeyCode.P);

            slotRequested = -1;
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1)) slotRequested = 0;
            else if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2)) slotRequested = 1;
            else if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3)) slotRequested = 2;
            else if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha4)) slotRequested = 3;

            prevNext = 0;
            if (UnityEngine.Input.GetKeyDown(KeyCode.Q)) prevNext = -1;
            else if (UnityEngine.Input.GetKeyDown(KeyCode.E)) prevNext = 1;
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
