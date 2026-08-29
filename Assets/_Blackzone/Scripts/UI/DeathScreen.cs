using Blackzone.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Blackzone.UI
{
    /// <summary>
    /// Death overlay: shown when the player dies; offers an immediate restart.
    /// The game manager auto-restarts after a delay if the player does nothing.
    /// </summary>
    public sealed class DeathScreen : MonoBehaviour
    {
        private GameObject panel;
        private Text countdownText;

        public static DeathScreen Build(Transform canvasRoot)
        {
            var go = new GameObject("DeathScreen");
            go.transform.SetParent(canvasRoot, false);
            var screen = go.AddComponent<DeathScreen>();
            screen.BuildWidgets(go.transform);
            return screen;
        }

        private void BuildWidgets(Transform root)
        {
            panel = new GameObject("DeathOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(root, false);
            var rt = (RectTransform)panel.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.1f, 0.02f, 0.02f, 0.65f);

            var title = MakeText(panel.transform, "Title", 72, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(600f, 100f), new Vector2(0f, 60f));
            title.text = "K.I.A.";
            title.color = new Color(0.85f, 0.2f, 0.18f, 1f);

            countdownText = MakeText(panel.transform, "Countdown", 22, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(600f, 40f), new Vector2(0f, -20f));
            countdownText.text = "";

            MakeButton(panel.transform, "Restart", "RESTART NOW", new Vector2(0f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(280f, 62f), new Vector2(0f, -110f), () => GameManager.Instance?.RestartEncounter());

            panel.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.PlayerDied += Show;
            GameEvents.EncounterRestarted += Hide;
        }

        private void OnDisable()
        {
            GameEvents.PlayerDied -= Show;
            GameEvents.EncounterRestarted -= Hide;
        }

        private void Show()
        {
            panel.SetActive(true);
        }

        private void Hide()
        {
            panel.SetActive(false);
        }

        private void Update()
        {
            if (!panel.activeSelf) return;
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Dead) return;
            // Count down using unscaled time so it also works while paused.
            int seconds = Mathf.CeilToInt(gm.AutoRestartDelay - gm.DeathElapsed);
            countdownText.text = "AUTO RESTART IN " + Mathf.Max(0, seconds);
        }

        // ---------------------------------------------------------------
        // Widget helpers
        // ---------------------------------------------------------------

        private static Text MakeText(Transform parent, string name, int fontSize, TextAnchor anchor,
            Vector2 aMin, Vector2 aMax, Vector2 size, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var t = go.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = fontSize;
            t.alignment = anchor;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        private static void MakeButton(Transform parent, string name, string label,
            Vector2 aMin, Vector2 aMax, Vector2 size, Vector2 pos, System.Action onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var img = go.GetComponent<Image>();
            img.color = new Color(0.22f, 0.24f, 0.28f, 1f);
            img.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick());

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var lrt = (RectTransform)labelGo.transform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            var t = labelGo.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 20;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(0.92f, 0.92f, 0.92f, 1f);
            t.text = label;
            t.raycastTarget = false;
        }
    }
}
