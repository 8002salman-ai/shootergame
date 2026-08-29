using Blackzone.Combat;
using Blackzone.Input;
using UnityEngine;

namespace Blackzone.Core
{
    public enum GameState { Boot, Playing, Dead, Paused }

    /// <summary>
    /// Owns the encounter lifecycle: start, player death, restart, pause.
    /// Single instance created by the bootstrapper; systems reach it via Instance.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private float autoRestartDelay = 8f;

        public GameState State { get; private set; } = GameState.Boot;
        public Transform PlayerRoot { get; private set; }
        public float AutoRestartDelay => autoRestartDelay;
        public float DeathElapsed => Mathf.Max(0f, autoRestartDelay - deathTimer);

        private Health playerHealth;
        private Armor playerArmor;
        private System.Action restartAction;
        private float deathTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (State == GameState.Dead)
            {
                deathTimer -= Time.deltaTime;
                if (deathTimer <= 0f) RestartEncounter();
            }
        }

        public void BindPlayer(Transform playerRoot, Health health, Armor armor)
        {
            PlayerRoot = playerRoot;
            playerHealth = health;
            playerArmor = armor;
            playerHealth.Died += OnPlayerDied;
        }

        public void SetRestartAction(System.Action action)
        {
            restartAction = action;
        }

        public void StartEncounter()
        {
            State = GameState.Playing;
        }

        public void SetPaused(bool paused)
        {
            if (State == GameState.Dead) return;
            State = paused ? GameState.Paused : GameState.Playing;
            Time.timeScale = paused ? 0f : 1f;
        }

        public void RestartEncounter()
        {
            Time.timeScale = 1f;
            GameInput.Enabled = true;
            restartAction?.Invoke();
            playerHealth?.Revive();
            playerArmor?.RestoreFull();
            GameEvents.EmitEncounterRestarted();
            State = GameState.Playing;
        }

        private void OnPlayerDied(Health health)
        {
            State = GameState.Dead;
            deathTimer = autoRestartDelay;
            GameInput.Enabled = false;
            GameEvents.EmitPlayerDied();
        }
    }
}
