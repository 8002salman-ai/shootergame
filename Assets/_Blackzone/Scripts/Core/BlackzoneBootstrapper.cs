using Blackzone.AI;
using Blackzone.Audio;
using Blackzone.Combat;
using Blackzone.Input;
using Blackzone.Player;
using Blackzone.Settings;
using Blackzone.UI;
using Blackzone.Utilities;
using Blackzone.World;
using UnityEngine;

namespace Blackzone.Core
{
    /// <summary>
    /// Entry point of BLACKZONE V0.01. Lives on the only object in the scene and
    /// builds the whole playable encounter at runtime from code (no scene assets
    /// required, fully reproducible from a fresh clone).
    ///
    /// Composition order:
    ///  settings -> quality -> audio -> world -> input -> player -> enemies -> UI -> game
    /// </summary>
    public class BlackzoneBootstrapper : MonoBehaviour
    {
        private void Awake()
        {
            var systemsRoot = new GameObject("[Systems]").transform;

            // 0. Runtime safety: verify URP pipeline has a renderer
            VerifyRenderPipeline();

            // 1. Settings + quality (before anything renders)
            GameSettings.Load();
            QualityApplier.Apply(GameSettings.Quality);

            // 2. Audio
            AudioManager.EnsureInstance(systemsRoot);

            // 3. World (map + navmesh)
            var map = MapBuilder.Build(systemsRoot);

            // 4. Input
            GameInput.Initialize(Application.isMobilePlatform);

            // 5. Player
            var player = PlayerFactory.Build(systemsRoot, map.PlayerSpawn);

            // 6. Enemies
            var spawner = EnemySpawner.Build(systemsRoot, map, player.Root);

            // 7. UI (HUD, mobile controls, pause, death, settings)
            UiFactory.Build(player, spawner);

            // 8. Post-processing volume (premium FPS atmosphere)
            PostProcessingSetup.Setup(player.Camera);
            PostProcessingSetup.SetQuality(GameSettings.Quality);

            // 9. Game manager wiring
            var gm = systemsRoot.gameObject.AddComponent<GameManager>();
            gm.BindPlayer(player.Root, player.Health, player.Armor);
            gm.SetRestartAction(() =>
            {
                player.Movement.ResetState();
                player.Look.ResetView();
                player.Health.Initialize(GameConstants.PlayerMaxHealth);
                player.Armor.Initialize(GameConstants.PlayerArmorCapacity, GameConstants.PlayerArmorAbsorb);
                player.Arsenal.RestockAll();
                spawner.ResetAll();
            });
            gm.StartEncounter();
        }

        /// <summary>
        /// Runtime safety: verify the URP pipeline asset has a valid renderer.
        /// If not, log a clear error directing the user to run Blackzone > 01.
        /// </summary>
        private static void VerifyRenderPipeline()
        {
            var pipeline = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;
            if (pipeline == null)
            {
                Debug.LogError("[BLACKZONE] No render pipeline assigned! " +
                    "Run menu Blackzone > 01 - Create URP Asset + Quality Levels.");
                return;
            }

            // Check if the pipeline asset has a renderer assigned
            var urpAsset = pipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
            if (urpAsset != null)
            {
                try
                {
                    var renderer = urpAsset.GetRenderer(0);
                    if (renderer == null)
                    {
                        Debug.LogError("[BLACKZONE] URP asset has NO renderer assigned! " +
                            "This causes 'Default Renderer is missing' error.\n" +
                            "Fix: Run menu Blackzone > 01 - Create URP Asset + Quality Levels\n" +
                            "Or manually assign a Forward Renderer in the URP asset inspector.");
                    }
                }
                catch
                {
                    // Older URP API — check via reflection
                    Debug.LogWarning("[BLACKZONE] Could not verify renderer assignment. " +
                        "If you see renderer errors, re-run Blackzone > 01.");
                }
            }
        }

        /// <summary>Drives the input snapshot after all gameplay updates.</summary>
        private void LateUpdate()
        {
            GameInput.UpdateFrame();
        }
    }
}
