using Blackzone.Core;
using Blackzone.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace Blackzone.UI
{
    /// <summary>
    /// Polished tactical HUD: spread-aware crosshair with gap, animated
    /// hitmarker with scale pulse, reload progress bar, improved health/armor
    /// bars, weapon name, ammo count, enemy counter, and FPS display.
    /// All uGUI, code-built — no scene assets needed.
    /// </summary>
    public sealed class HudController : MonoBehaviour
    {
        // Widgets
        private Image healthBar;
        private Image healthFill;
        private Image armorBar;
        private Image armorFill;
        private Text ammoText;
        private Text weaponNameText;
        private Text reloadText;
        private Image reloadBar;
        private Text enemyText;
        private Text fpsText;

        // Crosshair (4 lines with gap)
        private Image crosshairTop;
        private Image crosshairBot;
        private Image crosshairLeft;
        private Image crosshairRight;
        private Image crosshairDot;

        // Hitmarker
        private Image hitmarker;
        private Image hitmarkerInner;

        // Animation state
        private float hitmarkerTimer;
        private float hitmarkerScale = 1f;
        private float hitmarkerTargetScale = 1f;
        private float fpsTimer;
        private int frameCount;
        private float fps;
        private float currentSpreadDeg;
        private bool isReloading;
        private float reloadProgress;

        // Crosshair config
        private const float CrosshairBaseGap = 6f;
        private const float CrosshairLineLen = 10f;
        private const float CrosshairLineThick = 2f;
        private const float SpreadScale = 8f; // pixels per degree of spread

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

            // === Health bar (bottom-left) ===
            healthBar = MakeBarBg(root, "HealthBarBg", new Vector2(240f, 22f), new Vector2(20f, 24f));
            healthFill = MakeBarFill(root, "HealthFill", new Vector2(236f, 18f), new Vector2(22f, 26f),
                new Color(0.85f, 0.28f, 0.24f));

            // === Armor bar (below health) ===
            armorBar = MakeBarBg(root, "ArmorBarBg", new Vector2(240f, 16f), new Vector2(20f, 50f));
            armorFill = MakeBarFill(root, "ArmorFill", new Vector2(236f, 12f), new Vector2(22f, 52f),
                new Color(0.30f, 0.60f, 0.88f));

            // === Ammo (bottom-right) ===
            ammoText = MakeText(root, "Ammo", 42, TextAnchor.MiddleRight,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(240f, 55f), new Vector2(-20f, 20f));
            ammoText.color = new Color(1f, 1f, 1f, 0.95f);

            weaponNameText = MakeText(root, "WeaponName", 16, TextAnchor.MiddleRight,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(240f, 20f), new Vector2(-20f, 68f));
            weaponNameText.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);

            // === Reload progress bar (center-bottom) ===
            reloadText = MakeText(root, "Reload", 20, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(200f, 28f), new Vector2(0f, 100f));
            reloadText.text = "";
            reloadText.color = new Color(1f, 0.85f, 0.45f);

            reloadBar = MakeBarFill(root, "ReloadBar", new Vector2(160f, 4f), new Vector2(0f, 90f),
                new Color(1f, 0.85f, 0.45f, 0.7f));
            reloadBar.fillAmount = 0f;

            // === Enemy counter (top-left) ===
            enemyText = MakeText(root, "Enemies", 18, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(260f, 28f), new Vector2(16f, -10f));
            enemyText.color = new Color(0.9f, 0.35f, 0.3f, 0.9f);

            // === FPS (dev) ===
            fpsText = MakeText(root, "FPS", 14, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(120f, 22f), new Vector2(16f, -36f));
            fpsText.color = new Color(0.6f, 0.6f, 0.6f, 0.6f);

            // === Crosshair (4 lines + center dot) ===
            Color chColor = new Color(1f, 1f, 1f, 0.85f);
            crosshairTop = MakeCrosshairLine(root, "CH_Top", chColor, true);
            crosshairBot = MakeCrosshairLine(root, "CH_Bot", chColor, true);
            crosshairLeft = MakeCrosshairLine(root, "CH_Left", chColor, false);
            crosshairRight = MakeCrosshairLine(root, "CH_Right", chColor, false);
            crosshairDot = MakeImage(root, "CH_Dot", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(3f, 3f), Vector2.zero, chColor);

            // === Hitmarker (4 angled lines, center) ===
            hitmarker = MakeImage(root, "Hitmarker", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(30f, 30f), Vector2.zero, new Color(1f, 1f, 1f, 0f));
            hitmarker.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            hitmarkerInner = MakeImage(root, "HitmarkerInner", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(14f, 14f), Vector2.zero, new Color(1f, 1f, 1f, 0f));
            hitmarkerInner.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            // === Subscribe to events ===
            GameEvents.PlayerHealthChanged += OnHealth;
            GameEvents.PlayerArmorChanged += OnArmor;
            GameEvents.AmmoChanged += OnAmmo;
            GameEvents.WeaponSwitched += OnWeaponSwitched;
            GameEvents.ReloadStarted += OnReloadStarted;
            GameEvents.ReloadFinished += OnReloadFinished;
            GameEvents.HitConfirmed += OnHitConfirmed;
            GameEvents.EnemyKilled += OnEnemyKilled;
            GameEvents.EnemiesRemaining += OnEnemiesRemaining;
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
        }

        // ---------------------------------------------------------------
        // Event handlers
        // ---------------------------------------------------------------

        private void OnHealth(float current, float max)
        {
            if (healthFill != null)
                healthFill.fillAmount = Mathf.Clamp01(current / max);
        }

        private void OnArmor(float current, float max)
        {
            if (armorFill != null)
                armorFill.fillAmount = Mathf.Clamp01(current / max);
        }

        private void OnAmmo(int mag, int reserve)
        {
            if (ammoText != null) ammoText.text = $"{mag} / {reserve}";
        }

        private void OnWeaponSwitched(int slot)
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            var arsenal = gm.PlayerRoot != null ? gm.PlayerRoot.GetComponentInChildren<WeaponArsenal>() : null;
            if (arsenal != null && arsenal.Active != null && weaponNameText != null)
                weaponNameText.text = arsenal.Active.Def.displayName;
        }

        private void OnReloadStarted()
        {
            isReloading = true;
            if (reloadText != null) reloadText.text = "RELOADING";
        }

        private void OnReloadFinished()
        {
            isReloading = false;
            reloadProgress = 0f;
            if (reloadText != null) reloadText.text = "";
            if (reloadBar != null) reloadBar.fillAmount = 0f;
        }

        private void OnHitConfirmed()
        {
            if (hitmarker == null) return;
            hitmarker.color = new Color(1f, 1f, 1f, 1f);
            hitmarkerInner.color = new Color(1f, 1f, 1f, 0.8f);
            hitmarkerTimer = 0.12f;
            hitmarkerTargetScale = 1.3f;
            hitmarkerScale = 0.8f;
        }

        private void OnEnemyKilled()
        {
            if (hitmarker == null) return;
            hitmarker.color = new Color(1f, 0.30f, 0.25f, 1f);
            hitmarkerInner.color = new Color(1f, 0.45f, 0.35f, 0.9f);
            hitmarkerTimer = 0.20f;
            hitmarkerTargetScale = 1.5f;
            hitmarkerScale = 0.7f;
        }

        private void OnEnemiesRemaining(int alive, int total)
        {
            if (enemyText != null) enemyText.text = $"HOSTILES: {alive}/{total}";
        }

        public void SetWeaponName(string name)
        {
            if (weaponNameText != null) weaponNameText.text = name;
        }

        /// <summary>Called by WeaponArsenal to feed spread for crosshair gap.</summary>
        public void SetSpread(float hipDegrees, float adsDegrees, float adsAmount)
        {
            currentSpreadDeg = Mathf.Lerp(hipDegrees, adsDegrees, adsAmount);
        }

        // ---------------------------------------------------------------
        // Per-frame
        // ---------------------------------------------------------------

        private void Update()
        {
            UpdateHitmarker();
            UpdateCrosshair();
            UpdateReloadBar();
            UpdateFps();
        }

        private void UpdateHitmarker()
        {
            if (hitmarkerTimer > 0f)
            {
                hitmarkerTimer -= Time.unscaledDeltaTime;
                hitmarkerScale = Mathf.Lerp(hitmarkerScale, hitmarkerTargetScale, 20f * Time.unscaledDeltaTime);
                hitmarkerTargetScale = Mathf.Lerp(hitmarkerTargetScale, 1f, 10f * Time.unscaledDeltaTime);

                float alpha = hitmarkerTimer > 0f ? Mathf.Clamp01(hitmarkerTimer / 0.15f) : 0f;
                hitmarker.color = new Color(hitmarker.color.r, hitmarker.color.g, hitmarker.color.b, alpha);
                hitmarkerInner.color = new Color(hitmarkerInner.color.r, hitmarkerInner.color.g, hitmarkerInner.color.b, alpha * 0.8f);
                hitmarker.transform.localScale = Vector3.one * hitmarkerScale;
                hitmarkerInner.transform.localScale = Vector3.one * hitmarkerScale * 0.85f;
            }
        }

        private void UpdateCrosshair()
        {
            float gap = CrosshairBaseGap + currentSpreadDeg * SpreadScale;
            float halfGap = gap * 0.5f;

            // Top line
            SetAnchored(crosshairTop.rectTransform, new Vector2(0f, halfGap));
            crosshairTop.rectTransform.sizeDelta = new Vector2(CrosshairLineThick, CrosshairLineLen);
            // Bottom line
            SetAnchored(crosshairBot.rectTransform, new Vector2(0f, -halfGap));
            crosshairBot.rectTransform.sizeDelta = new Vector2(CrosshairLineThick, CrosshairLineLen);
            // Left line
            SetAnchored(crosshairLeft.rectTransform, new Vector2(-halfGap, 0f));
            crosshairLeft.rectTransform.sizeDelta = new Vector2(CrosshairLineLen, CrosshairLineThick);
            // Right line
            SetAnchored(crosshairRight.rectTransform, new Vector2(halfGap, 0f));
            crosshairRight.rectTransform.sizeDelta = new Vector2(CrosshairLineLen, CrosshairLineThick);
        }

        private void UpdateReloadBar()
        {
            if (!isReloading) return;

            // Get progress from active weapon
            var gm = GameManager.Instance;
            if (gm == null || gm.PlayerRoot == null) return;
            var arsenal = gm.PlayerRoot.GetComponentInChildren<WeaponArsenal>();
            if (arsenal != null && arsenal.Active != null)
            {
                reloadProgress = arsenal.Active.ReloadProgress;
                if (reloadBar != null) reloadBar.fillAmount = reloadProgress;
            }
        }

        private void UpdateFps()
        {
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
        // Widget factories
        // ---------------------------------------------------------------

        private static Image MakeBarBg(Transform parent, string name, Vector2 size, Vector2 pos)
        {
            var img = MakeImage(parent, name, new Vector2(0f, 0f), new Vector2(0f, 0f), size, pos,
                new Color(0.15f, 0.15f, 0.15f, 0.6f));
            return img;
        }

        private static Image MakeBarFill(Transform parent, string name, Vector2 size, Vector2 pos, Color color)
        {
            var img = MakeImage(parent, name, new Vector2(0f, 0f), new Vector2(0f, 0f), size, pos, color);
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillAmount = 1f;
            return img;
        }

        private static Image MakeCrosshairLine(Transform parent, string name, Color color, bool vertical)
        {
            var size = vertical
                ? new Vector2(CrosshairLineThick, CrosshairLineLen)
                : new Vector2(CrosshairLineLen, CrosshairLineThick);
            return MakeImage(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                size, Vector2.zero, color);
        }

        private static void SetAnchored(RectTransform rt, Vector2 offset)
        {
            rt.anchoredPosition = offset;
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
