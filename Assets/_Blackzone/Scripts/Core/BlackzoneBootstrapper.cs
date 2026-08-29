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

            // 8. Game manager wiring
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

        /// <summary>Drives the input snapshot after all gameplay updates.</summary>
        private void LateUpdate()
        {
            GameInput.UpdateFrame();
        }
    }
}
