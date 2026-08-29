using Blackzone.Core;
using Blackzone.Input;
using UnityEngine;
using UnityEngine.UI;

namespace Blackzone.UI
{
    /// <summary>
    /// Pause overlay (Esc/P on desktop, pause button on mobile).
    /// Pausing stops gameplay time; the overlay itself uses unscaled time.
    /// </summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        private GameObject panel;
        private SettingsScreen settings;

        public static PauseMenu Build(Transform canvasRoot)
        {
            var go = new GameObject("PauseMenu");
            go.transform.SetParent(canvasRoot, false);
            var menu = go.AddComponent<PauseMenu>();
            menu.BuildWidgets(go.transform);
            return menu;
        }

        public void SetSettings(SettingsScreen screen) => settings = screen;

        private void BuildWidgets(Transform root)
        {
            panel = MakeOverlay(root, "PausePanel", new Color(0.08f, 0.08f, 0.1f, 0.82f));

            var title = MakeText(panel.transform, "Title", 40, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(400f, 60f), new Vector2(0f, -50f));
            title.text = "PAUSED";
            title.color = new Color(0.85f, 0.62f, 0.25f);

            MakeButton(panel.transform, "Resume", "RESUME", new Vector2(0f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(280f, 62f), new Vector2(0f, 90f), () =>
                {
                    GameManager.Instance?.SetPaused(false);
                    panel.SetActive(false);
                });
            MakeButton(panel.transform, "Restart", "RESTART", new Vector2(0f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(280f, 62f), new Vector2(0f, 10f), () =>
                {
                    panel.SetActive(false);
                    GameManager.Instance?.RestartEncounter();
                });
            MakeButton(panel.transform, "Settings", "SETTINGS", new Vector2(0f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(280f, 62f), new Vector2(0f, -70f), () =>
                {
                    panel.SetActive(false);
                    if (settings != null) settings.Show();
                });
            MakeButton(panel.transform, "Quit", "QUIT", new Vector2(0f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(280f, 62f), new Vector2(0f, -150f), () =>
                {
                    panel.SetActive(false);
                    Application.Quit();
                });

            panel.SetActive(false);
        }

        private void Update()
        {
            if (GameInput.PausePressed)
            {
                if (panel.activeSelf) Resume();
                else Pause();
            }
        }

        public void Pause()
        {
            if (GameManager.Instance == null || GameManager.Instance.State == GameState.Dead) return;
            GameManager.Instance.SetPaused(true);
            panel.SetActive(true);
        }

        public void Resume()
        {
            GameManager.Instance?.SetPaused(false);
            panel.SetActive(false);
        }

        public void Hide()
        {
            panel.SetActive(false);
        }

        // ---------------------------------------------------------------
        // Widget helpers
        // ---------------------------------------------------------------

        private static GameObject MakeOverlay(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = color;
            go.GetComponent<Image>().raycastTarget = true;
            return go;
        }

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
