using System;

namespace Blackzone.Core
{
    /// <summary>
    /// Lightweight static event bus. Systems communicate through these events
    /// so UI/gameplay never reference each other directly.
    /// All handlers are optional; invoke is null-safe.
    /// </summary>
    public static class GameEvents
    {
        // --- Player ---
        public static event Action<float, float> PlayerHealthChanged;      // current, max
        public static event Action<float, float> PlayerArmorChanged;      // current, max
        public static event Action PlayerDied;

        // --- Weapons / combat ---
        public static event Action<int> WeaponSwitched;                   // slot index
        public static event Action<int, int> AmmoChanged;                 // magazine, reserve
        public static event Action ReloadStarted;
        public static event Action ReloadFinished;
        public static event Action<bool> AdsChanged;                      // true = aiming
        public static event Action HitConfirmed;                          // hitmarker
        public static event Action EnemyKilled;

        // --- Enemies / encounter ---
        public static event Action<int, int> EnemiesRemaining;            // alive, total
        public static event Action EncounterRestarted;

        // --- World / UI ---
        public static event Action<bool> ShowInteractPrompt;              // placeholder
        public static event Action<string> Toast;                         // short center message

        public static void EmitPlayerHealthChanged(float current, float max) =>
            PlayerHealthChanged?.Invoke(current, max);
        public static void EmitPlayerArmorChanged(float current, float max) =>
            PlayerArmorChanged?.Invoke(current, max);
        public static void EmitPlayerDied() => PlayerDied?.Invoke();
        public static void EmitWeaponSwitched(int slot) => WeaponSwitched?.Invoke(slot);
        public static void EmitAmmoChanged(int magazine, int reserve) => AmmoChanged?.Invoke(magazine, reserve);
        public static void EmitReloadStarted() => ReloadStarted?.Invoke();
        public static void EmitReloadFinished() => ReloadFinished?.Invoke();
        public static void EmitAdsChanged(bool aiming) => AdsChanged?.Invoke(aiming);
        public static void EmitHitConfirmed() => HitConfirmed?.Invoke();
        public static void EmitEnemyKilled() => EnemyKilled?.Invoke();
        public static void EmitEnemiesRemaining(int alive, int total) => EnemiesRemaining?.Invoke(alive, total);
        public static void EmitEncounterRestarted() => EncounterRestarted?.Invoke();
        public static void EmitShowInteractPrompt(bool show) => ShowInteractPrompt?.Invoke(show);
        public static void EmitToast(string message) => Toast?.Invoke(message);
    }
}
