# BLACKZONE — Technical Architecture (Phase 1)

## 1. Principles

1. **One scene, code-built world.** `Blackzone_Phase1.unity` contains a single
   `BlackzoneBootstrapper` object. Everything (map, player, enemies, UI, audio)
   is composed at runtime by static factories. A fresh clone is always
   playable without authoring assets.
2. **No god classes.** Each system owns one concern; factories compose them.
3. **Data-driven where it matters.** Weapons and AI difficulties are
   `ScriptableObject`s with code fallbacks (`Resources/LoadAll` overrides).
4. **Decoupled systems.** Communication through `GameEvents`; input through the
   `GameInput` facade; UI never contains gameplay logic.
5. **Mobile-conscious from frame one.** Throttled AI, pooled FX, no per-frame
   allocations in hot paths, static-batched map geometry, quality presets.

## 2. Startup flow

```
BlackzoneBootstrapper.Awake()
 ├─ GameSettings.Load()                     PlayerPrefs
 ├─ QualityApplier.Apply(Quality)           URP renderScale/MSAA/shadows/fps
 ├─ AudioManager.EnsureInstance()           procedural clips + 8-source pool
 ├─ MapBuilder.Build()                      world geometry + NavMeshSurface
 ├─ GameInput.Initialize(isMobile)          provider selection
 ├─ PlayerFactory.Build()                   rig: CC + movement + health/armor
 │                                          + camera/look + viewmodel + arsenal
 ├─ EnemySpawner.Build()                    N soldiers from MapLayout spawns
 ├─ UiFactory.Build()                       canvas + HUD + touch + overlays
 └─ GameManager (bind, StartEncounter)
LateUpdate → GameInput.UpdateFrame()        snapshot for next frame
```

## 3. Assembly layout

| Assembly | Location | References |
| -------- | -------- | ---------- |
| `Blackzone.Runtime` | `Scripts/Blackzone.Runtime.asmdef` | engine, `Unity.AI.Navigation`, `UnityEngine.UI`, `Unity.RenderPipelines.Universal.Runtime` |
| `Blackzone.Editor` | `Scripts/Editor/Blackzone.Editor.asmdef` | `Blackzone.Runtime` + above (Editor-only) |

## 4. System details

### 4.1 Input (`Scripts/Input/`)

```
IInputProvider (interface)
 ├─ DesktopInputProvider   WASD, mouse, Shift/Ctrl/C/Space/R/1-4/Esc/Q/E
 └─ MobileInputProvider    joystick + look surface + buttons (UI pushes in)
GameInput (static facade)  gameplay reads Move/LookDelta/FireHeld/FirePressed/...
```

- Edge-triggered flags (`FirePressed`, `ReloadPressed`, slot requests) are
  consumed once per frame by `GameInput.UpdateFrame()` in `LateUpdate`.
- `GameInput.Enabled` is set false on player death, true on restart.
- Mobile buttons push `MobileButton` enums — **no gameplay logic in UI**.

### 4.2 Player (`Scripts/Player/`)

- `FpsMovement`: `CharacterController`; ground/air acceleration, deceleration,
  gravity, coyote time, jump buffer, crouch toggle with stance lerp
  (capsule height + eye height), head bob. Reset for restart.
- `FpsLook`: yaw on rig, pitch on camera, clamp ±88°; sensitivity ×
  `GameSettings.Sensitivity`; ADS sensitivity multiplier; recoil punch with
  `recoilRecovery` deg/s recovery; FOV blend (hip → weapon `adsFov`, +6 while
  sprinting).
- `PlayerFactory.PlayerRig`: capsule (radius 0.35, height 1.8) + `Health` +
  `Armor` + `FpsMovement` → `CameraPivot` → `Camera` + `FpsLook` →
  `ViewmodelRoot` → weapon visuals.

### 4.3 Weapons (`Scripts/Weapons/`)

- `WeaponDefinition` (SO): identity, damage, headshot multiplier, RPM, fire
  mode, magazine/reserve, reload, ADS speed/FOV, hip/ADS spread, range,
  pellets, recoil (vertical, horizontal range, recovery), kick, accent color.
- `WeaponCatalog`: `Resources/LoadAll<WeaponDefinition>("Weapons")` or
  built-in 4-weapon fallback (KESTREL K-17 / VIPER V-9 / ANVIL A-12 /
  LONGBOW LB-7).
- `WeaponRuntime`: per-instance state (ammo, reload timer, ADS blend, kick).
  `TryFire()` gates on RPM cooldown, handles dry-fire, pellet spread, ballistics,
  FX, audio, recoil; auto-reload on empty.
- `WeaponArsenal`: loadout array, switch with 0.25 s holster, fire-input
  arbitration (auto vs semi), per-frame `Tick`, feeds ADS/FOV/sprint state to
  `FpsLook`.
- `WeaponVisualFactory`: primitive viewmodels per class (receiver/barrel/grip/
  mag/sight); `WeaponFx`: pooled tracer/muzzle/impact (unlit URP materials).

### 4.4 Combat (`Scripts/Combat/`)

- `Health`: pool + events (`Damaged`, `Died`), `Armor`: durability pool
  absorbing a percentage. `DamageInfo` struct carries source/headshot/weapon.
- `HitRegion` on head colliders → headshot multiplier.
- `Ballistics`: static hit-scan for player and AI shots; layer-masked
  (`PlayerFireMask = World|Enemy|Interactable`, `EnemyFireMask =
  World|Player`); impact FX + hit confirmation events on hits.

### 4.5 AI (`Scripts/AI/`)

- `AIDifficultyDefinition` (SO) + `AIDifficultyCatalog` (Resources/AI or
  ROOKIE/SOLDIER fallbacks).
- `EnemySoldier`: `NavMeshAgent` (updateRotation=false; manual facing),
  `Health`, head child with `HitRegion` + sphere collider, body capsule
  collider, decorative meshes (colliders removed so they don't block shots).
  Detection coroutine (0.25 s): range + FOV + LOS raycast
  (`EnemyVisionMask = World|Enemy` → can't see through walls, doesn't self-hit).
  FSM: Patrol (waypoints) → Engage (reaction → bursts → reposition every
  interval) → Search (last known pos, spin) → Return → Patrol. Damaged while
  not engaging snaps to Engage. Death: colliders off, agent off, body tips
  over, `EnemyKilled` event.
- `EnemySpawner`: builds roster from `MapLayout.EnemySpawns` (alternating
  difficulties), `ResetAll()` for restart, tracks alive count for HUD.

### 4.6 World (`Scripts/World/MapBuilder.cs`)

- Primitives only; materials cached; geometry marked static (static batching).
- `NavMeshSurface` (CollectObjects.Children, World layer, PhysicsColliders)
  baked at runtime → agents path immediately.
- `MapLayout` struct: `PlayerSpawn`, `EnemySpawns[8]`, `Waypoints[10]`.
- Sun registered with `QualityApplier.RegisterSun` so quality presets control
  shadow casting; fog + flat ambient tuned for the desert palette.

### 4.7 UI (`Scripts/UI/`)

- `UiFactory`: canvas (ScreenSpaceOverlay, 1920×1080 reference,
  ScaleWithScreenSize) + EventSystem; re-emits initial HUD state.
- `HudController`: all widgets code-built; subscribes `GameEvents`.
- `MobileControlPanel`: dynamic joystick (drag radius 90 px), look surface,
  `HoldButton`s → `GameInput.SetMobileButton`.
- `PauseMenu` / `SettingsScreen` / `DeathScreen`: overlays; settings sliders
  write `GameSettings` + `AudioManager` volumes; quality buttons call
  `GameSettings.SetQuality` → `QualityApplier.Apply`.

### 4.8 Settings & quality (`Scripts/Settings/`)

- `GameSettings`: PlayerPrefs (sensitivity, ADS sensitivity, master/effects
  volume, quality). Loaded at boot.
- `QualityApplier.Apply(q)`: `Application.targetFrameRate` 30/45/60, URP
  renderScale 0.7/0.85/1.0, MSAA 0/2/4, shadowDistance 12/25/45, sun shadows
  on/off. Safe to re-apply at runtime.

### 4.9 Audio (`Scripts/Audio/AudioManager.cs`)

- All SFX synthesized at boot (noise bursts, tones, sweeps, clicks) — zero
  licensed audio files. 8-source pool, pitch randomization, master → listener
  volume, effects → per-play volume. `AudioId` enum: Fire, EnemyFire, Reload,
  Empty, Hit, Kill, Death, EnemyDeath, Click.

## 5. Event catalog (`GameEvents`)

| Event | Payload | Emitted by | Consumed by |
| ----- | ------- | ---------- | ----------- |
| PlayerHealthChanged | cur, max | Health (player) | HUD |
| PlayerArmorChanged | cur, max | Armor (player) | HUD |
| PlayerDied | — | GameManager | DeathScreen, pause |
| WeaponSwitched | slot | Arsenal | HUD |
| AmmoChanged | mag, reserve | WeaponRuntime | HUD |
| ReloadStarted/Finished | — | WeaponRuntime | HUD |
| AdsChanged | bool | (reserved) | — |
| HitConfirmed | — | Ballistics path | HUD hitmarker |
| EnemyKilled | — | EnemySoldier/Spawner | HUD, counter |
| EnemiesRemaining | alive, total | EnemySpawner | HUD |
| EncounterRestarted | — | GameManager | DeathScreen |
| ShowInteractPrompt | bool | (placeholder) | HUD |
| Toast | string | (reserved) | — |

## 6. Configuration files

| File | Purpose | Committed? |
| ---- | ------- | ---------- |
| `Packages/manifest.json` | pinned packages (URP 17.0.3, ai.navigation 2.0.9, ugui 2.0.0) | yes |
| `ProjectSettings/ProjectVersion.txt` | Unity 6000.0.82f1 | yes |
| `ProjectSettings/TagManager.asset` | layers 3/5/8/9/10 | yes |
| `ProjectSettings/EditorBuildSettings.asset` | scene in build list | yes |
| `ProjectSettings/ProjectSettings.asset` etc. | **not committed** — Unity regenerates; editor menu `Blackzone ▸ 01/02` configures | no |
| `Assets/_Blackzone/Resources/Weapons|AI/*.asset` | tunable data (menu `03`) | yes (after creation) |

## 7. Performance budget notes (Phase 1)

- AI detection: 0.25 s coroutine; agents: 8 max.
- FX: object pool caps 48 per prefab; lifetimes ≤ 0.25 s.
- Lights: 2 directional (sun + fill), shadowed sun only.
- No dynamic real-time shadows on gameplay objects; shadow distance per preset.
- Map: ~300 primitive draw calls worst case, statically batched.
- uGUI: 1 canvas, no per-frame layout rebuilds; FPS text updates 2×/s.
