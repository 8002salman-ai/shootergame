using UnityEngine;

namespace Blackzone.Input
{
    /// <summary>
    /// Platform-agnostic input contract. Both the keyboard/mouse provider and
    /// the touch provider implement it. GameInput calls Sample() once per frame
    /// (in LateUpdate) and exposes a unified snapshot to gameplay systems.
    /// Edge-triggered values are consumed once per frame by GameInput.
    /// </summary>
    public interface IInputProvider
    {
        void Sample();

        Vector2 Move { get; }          // -1..1, y is forward
        Vector2 LookDelta { get; }     // mouse delta or touch delta
        bool FireHeld { get; }
        bool AdsHeld { get; }

        bool ConsumeFirePressed();
        bool ConsumeReloadPressed();
        bool ConsumeJumpPressed();
        bool ConsumeCrouchPressed();
        bool ConsumePausePressed();
        int ConsumeWeaponSlotRequested(); // absolute slot index, -1 when none
        int ConsumePrevNextRequest();     // -1 = previous, +1 = next, 0 = none
    }
}
