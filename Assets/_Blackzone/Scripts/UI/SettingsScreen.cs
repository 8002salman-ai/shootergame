using Blackzone.Audio;
using Blackzone.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Blackzone.UI
{
    /// <summary>
    /// Settings overlay: camera sensitivity, ADS sensitivity, master/effects
    /// volume and the three graphics presets. Changes persist via GameSettings.
    /// </summary>
    public sealed class SettingsScreen : MonoBehaviour
    {
        private GameObject panel;
        private readonly Button[] qualityButtons = new Button[3];

        public static SettingsScreen Build(Transform canvasRoot)
        {
            var go = new GameObject("SettingsScreen");
            go.transform.SetParent(canvasRoot, false);
            var screen = go.AddComponent<SettingsScreen>();
            screen.BuildWidgets(go.transform);
            return screen;
        }

        private void BuildWidgets(Transform root)
        {
            panel = MakeOverlay(root, "SettingsPanel", new Color(0.08f, 0.08f, 0.1f, 0.88f));

            var title = MakeText(panel.transform, "Title", 34, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(500f, 50f), new Vector2(0f, -40f));
            title.text = "SETTINGS";

            MakeSlider(panel.transform, "Sensitivity", 0.1f, 3f, GameSettings.Sensitivity,
                new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(420f, 30f), new Vector2(0f, 120f),
                v => GameSettings.SetSensitivity(v));

            MakeSlider(panel.transform, "ADS Sensitivity", 0.1f, 3f, GameSettings.AdsSensitivity,
                new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(420f, 30f), new Vector2(0f, 40f),
                v => GameSettings.SetAdsSensitivity(v));

            MakeSlider(panel.transform, "Master Volume", 0f, 1f, GameSettings.MasterVolume,
                new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(420f, 30f), new Vector2(0f, -40f),
                v =>
                {
                    GameSettings.SetMasterVolume(v);
                    if (AudioManager.Instance != null) AudioManager.Instance.SetMasterVolume(v);
                });

            MakeSlider(panel.transform, "Effects Volume", 0f, 1f, GameSettings.EffectsVolume,
                new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(420f, 30f), new Vector2(0f, -120f),
                v =>
                {
                    GameSettings.SetEffectsVolume(v);
                    if (AudioManager.Instance != null) AudioManager.Instance.SetEffectsVolume(v);
                });

            // Quality presets
            string[] labels = { "LOW 30", "MEDIUM 45", "HIGH 60" };
            for (int i = 0; i < 3; i++)
            {
                int q = i;
                qualityButtons[i] = MakeButton(panel.transform, "Quality" + i, labels[i],
                    new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(150f, 56f),
                    new Vector2(-170f + i * 170f, -220f), () =>
                    {
                        GameSettings.SetQuality(q);
                        RefreshQuality();
                    });
            }

            MakeButton(panel.transform, "Back", "BACK", new Vector2(0f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(200f, 56f), new Vector2(0f, -310f), Hide);

            RefreshQuality();
            panel.SetActive(false);
        }

        private void RefreshQuality()
        {
            for (int i = 0; i < qualityButtons.Length; i++)
            {
                var colors = qualityButtons[i].colors;
                colors.normalColor = i == GameSettings.Quality
                    ? new Color(0.85f, 0.62f, 0.25f, 1f)
                    : new Color(0.22f, 0.24f, 0.28f, 1f);
                qualityButtons[i].colors = colors;
            }
        }

        public void Show()
        {
            RefreshQuality();
            panel.SetActive(true);
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

        private static void MakeSlider(Transform parent, string label, float min, float max, float value,
            Vector2 aMin, Vector2 aMax, Vector2 size, Vector2 pos, System.Action<float> onChanged)
        {
            var labelText = MakeText(parent, label + "_Label", 20, TextAnchor.MiddleLeft,
                aMin, aMax, new Vector2(size.x, 24f), pos + new Vector2(0f, 26f));
            labelText.text = label.ToUpper();

            var go = new GameObject(label + "_Slider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            go.GetComponent<Image>().color = new Color(0.22f, 0.24f, 0.28f, 1f);
            go.GetComponent<Image>().raycastTarget = true;

            var slider = go.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            slider.onValueChanged.AddListener(v => onChanged(v));
        }

        private static Button MakeButton(Transform parent, string name, string label,
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
            t.fontSize = 16;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(0.92f, 0.92f, 0.92f, 1f);
            t.text = label;
            t.raycastTarget = false;
            return btn;
        }
    }
}
