using Blackzone.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Blackzone.UI
{
    /// <summary>
    /// Android touch controls: dynamic left joystick, right-side look surface
    /// and action buttons. Every control feeds GameInput — gameplay logic never
    /// lives in the buttons themselves.
    /// </summary>
    public sealed class MobileControlPanel : MonoBehaviour
    {
        private const float JoystickRadius = 90f;

        private Image joystickBase;
        private Image joystickKnob;
        private int joystickPointerId = -1;
        private Vector2 knobBase;

        private int lookPointerId = -1;
        private Vector2 lastLookPos;

        private RectTransform canvasRect;

        public static MobileControlPanel Build(Transform canvasRoot)
        {
            var go = new GameObject("TouchControls");
            go.transform.SetParent(canvasRoot, false);
            var panel = go.AddComponent<MobileControlPanel>();
            panel.BuildWidgets(go.transform);
            return panel;
        }

        private void BuildWidgets(Transform root)
        {
            canvasRect = root.GetComponent<RectTransform>();

            // --- Left: joystick zone ---
            var joystickZone = new GameObject("JoystickZone", typeof(RectTransform), typeof(Image));
            joystickZone.transform.SetParent(root, false);
            var jz = (RectTransform)joystickZone.transform;
            jz.anchorMin = new Vector2(0f, 0f);
            jz.anchorMax = new Vector2(0.45f, 1f);
            jz.offsetMin = Vector2.zero;
            jz.offsetMax = Vector2.zero;
            joystickZone.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);
            var jzHandler = joystickZone.AddComponent<JoystickZone>();
            jzHandler.panel = this;

            // Joystick visuals (hidden until touched)
            joystickBase = MakeImage(root, "JoystickBase", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(180f, 180f), new Vector2(150f, 140f), new Color(1f, 1f, 1f, 0.12f));
            joystickKnob = MakeImage(root, "JoystickKnob", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(90f, 90f), new Vector2(150f, 140f), new Color(1f, 1f, 1f, 0.22f));
            joystickBase.gameObject.SetActive(false);
            joystickKnob.gameObject.SetActive(false);

            // --- Right: look surface ---
            var lookZone = new GameObject("LookZone", typeof(RectTransform), typeof(Image));
            lookZone.transform.SetParent(root, false);
            var lz = (RectTransform)lookZone.transform;
            lz.anchorMin = new Vector2(0.45f, 0f);
            lz.anchorMax = new Vector2(1f, 1f);
            lz.offsetMin = Vector2.zero;
            lz.offsetMax = Vector2.zero;
            lookZone.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);
            var lzHandler = lookZone.AddComponent<LookZone>();
            lzHandler.panel = this;

            // --- Action buttons (right side) ---
            HoldButton.Build(root, "FireButton", MobileButton.Fire, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(120f, 120f), new Vector2(-70f, 60f), new Color(0.85f, 0.3f, 0.25f, 0.5f), "FIRE");
            HoldButton.Build(root, "AdsButton", MobileButton.Ads, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(84f, 84f), new Vector2(-190f, 110f), new Color(0.3f, 0.55f, 0.85f, 0.5f), "ADS");
            HoldButton.Build(root, "ReloadButton", MobileButton.Reload, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(72f, 72f), new Vector2(-200f, 210f), new Color(0.5f, 0.5f, 0.5f, 0.5f), "R");
            HoldButton.Build(root, "JumpButton", MobileButton.Jump, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(72f, 72f), new Vector2(-120f, 230f), new Color(0.5f, 0.7f, 0.45f, 0.5f), "JUMP");
            HoldButton.Build(root, "CrouchButton", MobileButton.Crouch, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(60f, 60f), new Vector2(-40f, 210f), new Color(0.55f, 0.5f, 0.4f, 0.5f), "CROUCH");
            HoldButton.Build(root, "PrevWeaponButton", MobileButton.PrevWeapon, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(56f, 56f), new Vector2(-190f, 20f), new Color(0.4f, 0.4f, 0.45f, 0.5f), "<");
            HoldButton.Build(root, "NextWeaponButton", MobileButton.NextWeapon, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(56f, 56f), new Vector2(-120f, 20f), new Color(0.4f, 0.4f, 0.45f, 0.5f), ">");
            HoldButton.Build(root, "PauseButton", MobileButton.None, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(56f, 56f), new Vector2(-20f, -20f), new Color(0.4f, 0.4f, 0.45f, 0.5f), "II");
        }

        // ---------------------------------------------------------------
        // Joystick
        // ---------------------------------------------------------------

        public void BeginJoystick(PointerEventData e)
        {
            if (joystickPointerId >= 0) return;
            joystickPointerId = e.pointerId;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, e.position, null, out var local);
            joystickBase.gameObject.SetActive(true);
            joystickKnob.gameObject.SetActive(true);
            joystickBase.rectTransform.anchoredPosition = local;
            joystickKnob.rectTransform.anchoredPosition = local;
            knobBase = local;
        }

        public void DragJoystick(PointerEventData e)
        {
            if (e.pointerId != joystickPointerId) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, e.position, null, out var local);
            Vector2 delta = local - knobBase;
            if (delta.magnitude > JoystickRadius)
                delta = delta.normalized * JoystickRadius;
            joystickKnob.rectTransform.anchoredPosition = knobBase + delta;
            GameInput.SetMobileMove(delta / JoystickRadius);
        }

        public void EndJoystick(PointerEventData e)
        {
            if (e.pointerId != joystickPointerId) return;
            joystickPointerId = -1;
            joystickBase.gameObject.SetActive(false);
            joystickKnob.gameObject.SetActive(false);
            GameInput.SetMobileMove(Vector2.zero);
        }

        // ---------------------------------------------------------------
        // Look surface
        // ---------------------------------------------------------------

        public void BeginLook(PointerEventData e)
        {
            if (lookPointerId >= 0) return;
            lookPointerId = e.pointerId;
            lastLookPos = e.position;
        }

        public void DragLook(PointerEventData e)
        {
            if (e.pointerId != lookPointerId) return;
            Vector2 delta = e.position - lastLookPos;
            lastLookPos = e.position;
            GameInput.SetMobileLook(delta);
        }

        public void EndLook(PointerEventData e)
        {
            if (e.pointerId != lookPointerId) return;
            lookPointerId = -1;
        }

        private static Image MakeImage(Transform parent, string name, Vector2 aMin, Vector2 aMax,
            Vector2 size, Vector2 pos, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }
    }

    /// <summary>Left half: dynamic joystick.</summary>
    public sealed class JoystickZone : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public MobileControlPanel panel;

        public void OnPointerDown(PointerEventData e) => panel.BeginJoystick(e);
        public void OnDrag(PointerEventData e) => panel.DragJoystick(e);
        public void OnPointerUp(PointerEventData e) => panel.EndJoystick(e);
    }

    /// <summary>Right half: camera look surface.</summary>
    public sealed class LookZone : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public MobileControlPanel panel;

        public void OnPointerDown(PointerEventData e) => panel.BeginLook(e);
        public void OnDrag(PointerEventData e) => panel.DragLook(e);
        public void OnPointerUp(PointerEventData e) => panel.EndLook(e);
    }

    /// <summary>Touch button that pushes held/pressed state into GameInput.</summary>
    public sealed class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private MobileButton button;

        public static void Build(Transform parent, string name, MobileButton btn,
            Vector2 aMin, Vector2 aMax, Vector2 size, Vector2 pos, Color color, string text)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = true;

            var holder = go.AddComponent<HoldButton>();
            holder.button = btn;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var lrt = (RectTransform)labelGo.transform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            var t = labelGo.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 14;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.text = text;
            t.raycastTarget = false;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (button != MobileButton.None) GameInput.SetMobileButton(button, true);
            else GameInput.SetMobilePause();
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (button != MobileButton.None) GameInput.SetMobileButton(button, false);
        }
    }
}
