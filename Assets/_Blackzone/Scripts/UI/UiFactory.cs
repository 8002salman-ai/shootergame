using Blackzone.AI;
using Blackzone.Core;
using Blackzone.Input;
using Blackzone.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Blackzone.UI
{
    /// <summary>
    /// Builds the entire UI layer in code: canvas + event system, HUD,
    /// mobile touch controls, pause menu, settings and death screen.
    /// </summary>
    public static class UiFactory
    {
        public static void Build(PlayerFactory.PlayerRig player, EnemySpawner spawner)
        {
            Transform canvasRoot = CreateCanvas();

            var hud = HudController.Build(canvasRoot);
            if (GameInput.IsMobile) MobileControlPanel.Build(canvasRoot);
            var pause = PauseMenu.Build(canvasRoot);
            var settings = SettingsScreen.Build(canvasRoot);
            pause.SetSettings(settings);
            DeathScreen.Build(canvasRoot);

            // Seed the HUD with the current state (events fired before the HUD
            // existed are re-emitted here).
            if (player.Arsenal.Active != null)
            {
                hud.SetWeaponName(player.Arsenal.Active.Def.displayName);
                GameEvents.EmitAmmoChanged(player.Arsenal.Active.MagazineAmmo, player.Arsenal.Active.ReserveAmmo);
            }
            GameEvents.EmitPlayerHealthChanged(player.Health.Current, player.Health.Max);
            GameEvents.EmitPlayerArmorChanged(player.Armor.Current, player.Armor.Max);
            GameEvents.EmitEnemiesRemaining(spawner.AliveCount(), spawner.Total);
        }

        private static Transform CreateCanvas()
        {
            // Event system
            var eventGo = new GameObject("EventSystem",
                typeof(EventSystem), typeof(StandaloneInputModule));
            Object.DontDestroyOnLoad(eventGo);

            // Canvas
            var canvasGo = new GameObject("UICanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            return canvasGo.transform;
        }
    }
}
