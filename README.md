# BLACKZONE — Phase 1 Vertical Slice (V0.01)

**BLACKZONE** is an original near-future **tactical FPS** for **Android** (iOS later),
built with **Unity 6 + URP**. Phase 1 delivers a playable single-encounter vertical
slice: movement, shooting, four prototype weapons, enemy AI and mobile touch
controls — the technical foundation of a serious tactical mobile shooter.

> ⚠️ **Work in progress.** This is V0.01. Not a complete game, no multiplayer,
> no store, no backend. See `Documentation/NEXT_PHASE.md` for the roadmap.

---

## Quick facts

| Item | Value |
| ---- | ----- |
| Engine | Unity **6000.0.82f1** (6000.0 LTS) |
| Render pipeline | URP **17.0.3** |
| Platforms | Android (primary, ARM64) · Editor/Windows for development · iOS later |
| Target build | `Blackzone_Phase1.unity` (auto-added to build settings) |
| Namespace root | `Blackzone` |
| Code location | `Assets/_Blackzone/Scripts/` |

## How to open and run

1. Install **Unity Hub** and **Unity 6000.0.82f1 LTS** with modules:
   **Android Build Support (SDK + NDK + OpenJDK)**.
2. In Unity Hub → *Add project from disk* → select the cloned repository folder
   (the repo **is** the project root).
3. On first open, Unity imports the project and regenerates
   `ProjectSettings.asset`, `InputManager.asset`, etc. from defaults — this is
   **expected and safe** (they are intentionally not committed; see
   `Documentation/TECH_ARCHITECTURE.md`).
4. Wait for compilation to finish. Then run menu **Blackzone ▸ 01 - Create URP
   Asset + Quality Levels** and **Blackzone ▸ 03 - Create Weapon + AI Data
   Assets** (menu 03 is optional — the game runs without it).
5. Open `Assets/_Blackzone/Scenes/Blackzone_Phase1.unity` and press **Play**.

### Editor controls (Windows)

| Action | Key |
| ------ | --- |
| Move | WASD |
| Look | Mouse |
| Shoot | Left mouse (hold = auto) |
| ADS | Right mouse (hold) |
| Sprint | Shift (hold, forward only) |
| Crouch | Ctrl or C (toggle) |
| Jump | Space |
| Reload | R |
| Weapons 1–4 | 1 / 2 / 3 / 4 |
| Previous / next weapon | Q / E |
| Pause | Esc or P |

### Mobile controls (Android)

- **Left half of screen:** dynamic movement joystick.
- **Right half of screen:** drag to look.
- **Buttons (right side):** FIRE (big), ADS, reload, jump, crouch, prev/next
  weapon, pause (top-right).

## Android build

See `Documentation/ANDROID_BUILD_GUIDE.md` for the full walkthrough
(modules, SDK/NDK/JDK, Player Settings, build, USB deploy). Short version:

1. Run editor menu **Blackzone ▸ 02 - Configure Android Player Settings**
   (ARM64 / IL2CPP / landscape / package `com.blackzone.tactical`).
2. `File ▸ Build Settings ▸ Android ▸ Build`.
3. Install the APK: `adb install -r Builds/Blackzone.apk`.

## Repository layout

```
Assets/_Blackzone/
  Art/                 (reserved — all Phase 1 art is runtime-generated)
  Audio/               (reserved — sounds are synthesized in code)
  Prefabs/             (reserved — the game builds objects in code)
  Resources/Weapons/   (WeaponDefinition assets — created via editor menu)
  Resources/AI/        (AIDifficultyDefinition assets — created via editor menu)
  Scenes/Blackzone_Phase1.unity   (one object: BlackzoneBootstrapper)
  Scripts/             (all gameplay code, namespaced by system)
  Scripts/Core/        GameEvents, GameManager, BlackzoneBootstrapper
  Scripts/Player/      FpsMovement, FpsLook, PlayerFactory
  Scripts/Input/       GameInput + desktop/mobile providers
  Scripts/Weapons/     WeaponDefinition, catalog, runtime, arsenal, FX, visuals
  Scripts/Combat/      Health, Armor, HitRegion, Ballistics
  Scripts/AI/          EnemySoldier, spawner, difficulty data
  Scripts/World/       MapBuilder (Training Outpost)
  Scripts/UI/          HUD, touch controls, pause/settings/death screens
  Scripts/Settings/    GameSettings, QualityApplier (LOW/MED/HIGH)
  Scripts/Audio/       AudioManager (procedural SFX)
  Scripts/Editor/      BlackzoneProjectSetup (menu items)
Tools/                 meta generator + static validator (Python, no Unity needed)
Documentation/         design, architecture, checklists, build guide, known issues
```

## What is implemented (V0.01)

- First-person player: walk / sprint / crouch / jump with coyote time & jump
  buffering, head bob, smooth camera, pitch limits, ADS FOV blend.
- Input architecture with two providers (keyboard/mouse + touch) feeding one
  common `GameInput` facade — UI buttons never contain gameplay logic.
- Weapon framework: one reusable `WeaponRuntime` driven by `WeaponDefinition`
  data. **Four original weapons: KESTREL K-17 (AR), VIPER V-9 (SMG),
  ANVIL A-12 (shotgun), LONGBOW LB-7 (marksman).**
- Shooting: fire-rate, ammo, reload, ADS, hip fire, hit-scan ballistics with
  headshot multipliers, recoil + recovery, spread, pooled tracer/muzzle/impact FX.
- Player 100 HP + 50 armor (absorbs 50% of damage), death screen, auto-restart
  and manual restart.
- Enemy AI: patrol → detect (LOS + FOV, 0.25s throttle) → engage with bursts →
  reposition → lose target → search → return. Two difficulties: **ROOKIE** and
  **SOLDIER**.
- Map: **BLACKZONE TRAINING OUTPOST** (~140×90 m): container yard, two
  warehouses, barriers, watchtower, long sightline, NavMesh baked at runtime.
- HUD: health/armor, ammo, weapon name, crosshair, hitmarker, enemy counter,
  reload indicator, FPS (dev builds), interact prompt placeholder.
- Settings: graphics LOW 30 / MEDIUM 45 / HIGH 60 (render scale, shadows, MSAA,
  FPS cap), camera sensitivity, ADS sensitivity, master/effects volume.

## What is intentionally NOT in Phase 1

Multiplayer, extraction persistence, backend/accounts, battle royale, store,
payments, ads, battle pass, ranked, clans, chat/voice, anti-cheat, huge map.
See `Documentation/NEXT_PHASE.md`.

## Current limitations (honest list)

- **No Unity Editor in the authoring environment** — all C# was statically
  validated (tree-sitter syntax + cross-file contract checks), but **no
  compilation or in-Editor playtest has happened yet**. First open in Unity
  may surface API-level issues; see `Documentation/KNOWN_ISSUES.md`.
- `ProjectSettings.asset`, `QualitySettings.asset`, `InputManager.asset` are
  intentionally **not committed** (Unity regenerates them). Run menu
  **Blackzone ▸ 01** once to configure URP + quality levels.
- All art/audio is procedural placeholder, replaced later without code changes.
- No APK has been produced yet (no Android toolchain in this environment).

## Validation tooling

No-Unity static checks (run from the repo root):

```bash
python3 Tools/generate_meta.py --check   # metas present & valid
python3 Tools/validate_project.py        # C# syntax + contracts + GUIDs + layers
```

## License / originality

All names, art direction and systems are original BLACKZONE work. No assets,
names or UI from existing commercial shooters are used. Genre inspiration only.
