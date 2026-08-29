using Blackzone.Core;
using Blackzone.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace Blackzone.UI
{
    /// <summary>
    /// Builds the shared HUD: health/armor bars, ammo, weapon name, crosshair,
    /// hitmarker, reload indicator, interact prompt, enemy counter and FPS.
    /// All UI is created in code (uGUI), so no scene/prefab assets are needed.
    /// </summary>
    public sealed class HudController : MonoBehaviour
    {
        private Image healthBar;
        private Image armorBar;
        private Text ammoText;
        private Text weaponNameText;
        private Text reloadText;
        private Text enemyText;
        private Text fpsText;
        private Image crosshair;
        private Image hitmarker;
        private GameObject interactPrompt;

        private float hitmarkerTimer;
        private float fpsTimer;
        private int frameCount;
        private float fps;

        public static HudController Build(Transform canvasRoot)
        {
            var go = new GameObject("Hud");
            go.transform.SetParent(canvasRoot, false);
            var hud = go.AddComponent<HudController>();
            hud.BuildWidgets(go.transform);
            return hud;
        }

        private void BuildWidgets(Transform root)
        {
            var rect = root.gameObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // --- Bottom-left: health + armor ---
            healthBar = MakeBar(root, "HealthBar", new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(230f, 18f), new Vector2(20f, 24f), new Color(0.82f, 0.30f, 0.26f));
            armorBar = MakeBar(root, "ArmorBar", new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(230f, 12f), new Vector2(20f, 46f), new Color(0.35f, 0.62f, 0.85f));

            // --- Bottom-right: ammo ---
            ammoText = MakeText(root, "Ammo", 40, TextAnchor.MiddleRight,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(220f, 50f), new Vector2(-20f, 22f));
            weaponNameText = MakeText(root, "WeaponName", 18, TextAnchor.MiddleRight,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(220f, 22f), new Vector2(-20f, 62f));

            // --- Reload indicator (center-bottom) ---
            reloadText = MakeText(root, "Reload", 24, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(300f, 40f), new Vector2(0f, 90f));
            reloadText.text = "";
            reloadText.color = new Color(1f, 0.8f, 0.4f);

            // --- Enemy counter (top-left) ---
            enemyText = MakeText(root, "Enemies", 20, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(240f, 30f), new Vector2(16f, -12f));
            enemyText.text = "";

            // --- FPS (below enemy counter, dev builds) ---
            fpsText = MakeText(root, "FPS", 16, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(120f, 24f), new Vector2(16f, -40f));
            fpsText.text = "";

            // --- Crosshair (center) ---
            crosshair = MakeImage(root, "Crosshair", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(10f, 10f), new Vector2(0f, 0f), new Color(1f, 1f, 1f, 0.9f));

            // --- Hitmarker (center, flashes on hit) ---
            hitmarker = MakeImage(root, "Hitmarker", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(26f, 26f), new Vector2(0f, 0f), new Color(1f, 1f, 1f, 0f));
            hitmarker.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            // --- Interact prompt (bottom-center) ---
            interactPrompt = MakeText(root, "InteractPrompt", 20, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(320f, 30f), new Vector2(0f, 140f)).gameObject;
            interactPrompt.GetComponent<Text>().text = "[E] INTERACT";
            interactPrompt.SetActive(false);

            // --- Subscribe ---
            GameEvents.PlayerHealthChanged += OnHealth;
            GameEvents.PlayerArmorChanged += OnArmor;
            GameEvents.AmmoChanged += OnAmmo;
            GameEvents.WeaponSwitched += OnWeaponSwitched;
            GameEvents.ReloadStarted += OnReloadStarted;
            GameEvents.ReloadFinished += OnReloadFinished;
            GameEvents.HitConfirmed += OnHitConfirmed;
            GameEvents.EnemyKilled += OnEnemyKilled;
            GameEvents.EnemiesRemaining += OnEnemiesRemaining;
            GameEvents.ShowInteractPrompt += OnInteractPrompt;
        }

        private void OnDestroy()
        {
            GameEvents.PlayerHealthChanged -= OnHealth;
            GameEvents.PlayerArmorChanged -= OnArmor;
            GameEvents.AmmoChanged -= OnAmmo;
            GameEvents.WeaponSwitched -= OnWeaponSwitched;
            GameEvents.ReloadStarted -= OnReloadStarted;
            GameEvents.ReloadFinished -= OnReloadFinished;
            GameEvents.HitConfirmed -= OnHitConfirmed;
            GameEvents.EnemyKilled -= OnEnemyKilled;
            GameEvents.EnemiesRemaining -= OnEnemiesRemaining;
            GameEvents.ShowInteractPrompt -= OnInteractPrompt;
        }

        // ---------------------------------------------------------------
        // Event handlers
        // ---------------------------------------------------------------

        private void OnHealth(float current, float max)
        {
            if (healthBar != null)
                healthBar.fillAmount = Mathf.Clamp01(current / max);
        }

        private void OnArmor(float current, float max)
        {
            if (armorBar != null)
                armorBar.fillAmount = Mathf.Clamp01(current / max);
        }

        private void OnAmmo(int mag, int reserve)
        {
            if (ammoText != null) ammoText.text = $"{mag} / {reserve}";
        }

        private void OnWeaponSwitched(int slot)
        {
            // Weapon name is refreshed by the UI factory after binding; this
            // handler keeps the HUD in sync when switching happens at runtime.
            var gm = GameManager.Instance;
            if (gm == null) return;
            var arsenal = gm.PlayerRoot != null ? gm.PlayerRoot.GetComponentInChildren<WeaponArsenal>() : null;
            if (arsenal != null && arsenal.Active != null && weaponNameText != null)
                weaponNameText.text = arsenal.Active.Def.displayName;
        }

        private void OnReloadStarted()
        {
            if (reloadText != null) reloadText.text = "RELOADING...";
        }

        private void OnReloadFinished()
        {
            if (reloadText != null) reloadText.text = "";
        }

        private void OnHitConfirmed()
        {
            if (hitmarker == null) return;
            hitmarker.color = new Color(1f, 1f, 1f, 1f);
            hitmarkerTimer = 0.09f;
        }

        private void OnEnemyKilled()
        {
            if (hitmarker == null) return;
            hitmarker.color = new Color(1f, 0.35f, 0.3f, 1f);
            hitmarkerTimer = 0.16f;
        }

        private void OnEnemiesRemaining(int alive, int total)
        {
            if (enemyText != null) enemyText.text = $"HOSTILES: {alive}/{total}";
        }

        private void OnInteractPrompt(bool show)
        {
            if (interactPrompt != null) interactPrompt.SetActive(show);
        }

        public void SetWeaponName(string name)
        {
            if (weaponNameText != null) weaponNameText.text = name;
        }

        // ---------------------------------------------------------------
        // Per-frame bits (hitmarker fade, FPS counter)
        // ---------------------------------------------------------------

        private void Update()
        {
            if (hitmarkerTimer > 0f)
            {
                hitmarkerTimer -= Time.unscaledDeltaTime;
                if (hitmarkerTimer <= 0f)
                    hitmarker.color = new Color(1f, 1f, 1f, 0f);
            }

            fpsTimer += Time.unscaledDeltaTime;
            frameCount++;
            if (fpsTimer >= 0.5f)
            {
                fps = frameCount / fpsTimer;
                frameCount = 0;
                fpsTimer = 0f;
                if (fpsText != null)
                    fpsText.text = Debug.isDebugBuild ? $"FPS {fps:0}" : "";
            }
        }

        // ---------------------------------------------------------------
        // uGUI helpers
        // ---------------------------------------------------------------

        private static Image MakeBar(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 size, Vector2 pos, Color color)
        {
            var go = MakeImage(parent, name, anchorMin, anchorMax, size, pos, color);
            go.type = Image.Type.Filled;
            go.fillMethod = Image.FillMethod.Horizontal;
            go.fillAmount = 1f;
            return go;
        }

        private static Image MakeImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 size, Vector2 pos, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static Text MakeText(Transform parent, string name, int fontSize, TextAnchor anchor,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }
    }
}
