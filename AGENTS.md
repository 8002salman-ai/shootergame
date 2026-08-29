# AGENTS.md — Guidance for AI agents working on BLACKZONE

This file exists so another coding agent (e.g. FREEBUFF) can clone this
repository and continue development **without rebuilding anything from scratch**.

---

## 1. Project purpose

BLACKZONE: an original near-future **tactical FPS** for Android. Phase 1 (this
repo state) is a **playable single-encounter vertical slice** proving
movement, shooting, enemy AI and mobile controls. The long-term vision is a
tactical extraction shooter (squad AI, looting, extraction zones, PvP), but
**Phase 1 must stay a vertical slice** — see section 12.

## 2. Unity version

- **Unity 6000.0.82f1 LTS** (`ProjectSettings/ProjectVersion.txt`).
- **URP 17.0.3** + **com.unity.ai.navigation 2.0.9** + **com.unity.ugui 2.0.0**
  pinned in `Packages/manifest.json`.
- Everything below uses the legacy uGUI (Text/Image/Button) — no UI Toolkit.

## 3. Current architecture (the 30-second version)

**One scene, one object (`BlackzoneBootstrapper`) → builds everything at
runtime from code.** There are no scene-authored gameplay objects, no prefab
assets, no serialized references. This makes the repo tiny and reproducible,
and makes `ProjectSettings.asset` corruption impossible to reintroduce
(that file is intentionally not committed).

Flow: `BlackzoneBootstrapper.Awake()` →
`GameSettings.Load` → `QualityApplier.Apply` → `AudioManager.EnsureInstance`
→ `MapBuilder.Build` (world + NavMesh) → `GameInput.Initialize` →
`PlayerFactory.Build` → `EnemySpawner.Build` → `UiFactory.Build` →
`GameManager` bind + start.

Systems communicate through **`GameEvents`** (static event bus in
`Scripts/Core/GameEvents.cs`). Gameplay never references UI classes directly,
and UI never contains gameplay logic (buttons call `GameInput` setters).

Input: two `IInputProvider` implementations (desktop keyboard/mouse,
mobile touch) behind the static `GameInput` facade. `BlackzoneBootstrapper`
drives `GameInput.UpdateFrame()` in `LateUpdate`.

## 4. Folder structure

```
Assets/_Blackzone/
  Scenes/                 Blackzone_Phase1.unity (one object: bootstrapper)
  Scripts/
    Core/                 GameEvents, GameManager, BlackzoneBootstrapper
    Player/               FpsMovement, FpsLook, PlayerFactory
    Input/                IInputProvider, GameInput, Desktop/Mobile providers
    Weapons/              WeaponDefinition, WeaponCatalog, WeaponRuntime,
                          WeaponArsenal, WeaponFx, WeaponVisualFactory
    Combat/               Health, Armor, HitRegion, DamageInfo, Ballistics
    AI/                   AIDifficultyDefinition, AIDifficultyCatalog,
                          EnemySoldier, EnemySpawner
    World/                MapBuilder (Training Outpost + MapLayout)
    UI/                   UiFactory, HudController, MobileControlPanel,
                          PauseMenu, SettingsScreen, DeathScreen
    Settings/             GameSettings, QualityApplier
    Audio/                AudioManager (procedural SFX)
    Utilities/            GameConstants, ObjectPool
    Editor/               BlackzoneProjectSetup (editor menus)
  Resources/
    Weapons/              *.asset (created by editor menu; optional)
    AI/                   *.asset (created by editor menu; optional)
  Art/  Audio/  Prefabs/  (reserved folders for later phases)
Tools/                    generate_meta.py, validate_project.py (Python)
Documentation/            design + tech docs
```

Assemblies: `Blackzone.Runtime` (game code, asmdef at
`Scripts/Blackzone.Runtime.asmdef`), `Blackzone.Editor` (Editor-only, at
`Scripts/Editor/`). Keep all new code inside these folders.

## 5. Systems implemented (Phase 1)

- Player: walk/sprint/crouch/jump, coyote time, jump buffer, head bob, gravity,
  stance lerp (`FpsMovement`); camera look with pitch clamp, sensitivity +
  ADS sensitivity scaling, recoil punch + recovery, FOV blending (`FpsLook`).
- Weapons: one data-driven runtime for all guns. Four original weapons
  (KESTREL K-17, VIPER V-9, ANVIL A-12, LONGBOW LB-7) defined in
  `WeaponCatalog`; ammo/reload/ADS/auto-reload-on-empty; hit-scan ballistics
  with headshot multipliers; pooled tracer/muzzle/impact FX; recoil distinct
  per weapon. Switch with 0.25 s holster delay (keys 1–4 / Q / E / touch).
- Combat: `Health` (100) + `Armor` (50, absorbs 50%); `HitRegion` marks head
  colliders; death → `GameManager` state machine → death screen →
  auto-restart (8 s) or manual restart.
- AI: `EnemySoldier` FSM (Idle/Patrol/Investigate/Engage/Search/Return/Dead)
  with LOS-validated detection at 0.25 s intervals, burst fire with reaction
  delay, periodic repositioning, search-and-return after losing the player.
  Difficulties are data: **ROOKIE** and **SOLDIER** (`AIDifficultyCatalog`).
- Map: `MapBuilder` builds the Training Outpost (~140×90 m) from primitives
  and bakes a NavMeshSurface at runtime; returns `MapLayout`
  (PlayerSpawn, EnemySpawns, Waypoints).
- UI: code-built uGUI. HUD (health/armor bars, ammo, crosshair, hitmarker,
  enemy counter, reload text, FPS), touch joystick + look surface + action
  buttons, pause menu, settings screen (sensitivity/volumes/quality),
  death screen.
- Settings: `GameSettings` (PlayerPrefs) + `QualityApplier` — LOW 30 fps /
  MEDIUM 45 fps / HIGH 60 fps with render scale, shadows, MSAA, FPS cap.
- Audio: `AudioManager` synthesizes all SFX at runtime (no audio files).
- Editor menus (`Blackzone ▸ 01/02/03`): URP asset + quality levels,
  Android Player Settings, Weapon/AI data assets.

## 6. Systems intentionally NOT implemented

- Multiplayer / networking / matchmaking / backend / accounts / anti-cheat.
- Extraction persistence, looting, inventory, missions, progression, battle
  pass, store, payments, ads, clans, chat, voice, ranking.
- Prone / vault / slide / lean / mantling (movement is architected to add them).
- Elite/Boss AI profiles, armor tiers, healing items (Health/armor are
  architected to support them).
- Real art/audio assets, animations, cinematics, main menu.
- Day/night, weather, dynamic destruction, large open world.

Do **not** add any of these in Phase 1 without explicit instruction.

## 7. Coding conventions

- C# (Unity 6 compatible), namespaces under `Blackzone.*`, assembly
  `Blackzone.Runtime` / `Blackzone.Editor`.
- One class per file, file named after the class.
- Static factories for composition (`*Factory.Build(...)`); avoid
  MonoBehaviour bloat; no god classes.
- All gameplay constants in `GameConstants`; all tunables on data objects
  (ScriptableObjects or serialized fields with sane defaults).
- Events via `GameEvents`; never reference UI from gameplay, never put
  gameplay in UI callbacks (buttons → `GameInput` setters only).
- No LINQ in hot paths (per-frame code), no allocations per shot where
  possible; use `ObjectPool` for repeated FX.
- `Time.deltaTime` for gameplay; `Time.unscaledDeltaTime` for UI overlays.
- Strings in code are fine for Phase 1 (no localization layer yet).

## 8. How to validate changes (no Unity Editor needed)

```bash
python3 Tools/generate_meta.py --check   # every asset has a valid .meta
python3 Tools/validate_project.py        # C# syntax (tree-sitter), contracts,
                                         # GUID resolution, layer consistency
```

When adding a file: run `python3 Tools/generate_meta.py` (deterministic GUIDs
via uuid5 — stable across machines, so don't hand-edit metas).

When adding a script that other code calls: extend the "contracts" section of
`Tools/validate_project.py` so cross-file APIs are checked automatically.

After that, the definitive validation is opening the project in Unity 6000.0.82f1
and pressing Play (see `Documentation/TESTING_CHECKLIST.md`). **Never claim
Unity compilation or APK results that were not actually produced.**

## 9. Known limitations

- `ProjectSettings.asset` / `QualitySettings.asset` / `InputManager.asset` are
  **not committed** (Unity regenerates defaults). After first open, run
  **Blackzone ▸ 01** to assign the URP asset and quality levels. Android
  settings come from **Blackzone ▸ 02**.
- The hand-written scene (`Blackzone_Phase1.unity`) contains only the
  bootstrapper; Unity upgrades its serialization on first open (normal).
- Layer indices (3/5/8/9/10) must stay in sync between `TagManager.asset` and
  `GameConstants` — the validator checks this.
- `EnemySoldier` uses `NavMeshAgent` + manual facing; a `NavMeshSurface` is
  baked at runtime on `[World]`. If the map changes, the bake happens
  automatically (surface is a child of `[World]`).
- Legacy uGUI only. Fonts use `Resources.GetBuiltinResource<Font>
  ("LegacyRuntime.ttf")`.
- Full known issues: `Documentation/KNOWN_ISSUES.md`.

## 10. Mobile performance rules (Phase 1 baseline)

- Target 30–60 fps on mid-range Android; test with the in-game FPS counter.
- Keep per-frame allocations out of `Update()`; reuse via `ObjectPool`.
- AI detection is throttled (coroutine every 0.25 s); keep new AI work
  off-frame or coroutine-gated.
- One real-time light (sun) + one fill light; shadow distance/quality per
  preset (`QualityApplier`); keep additional real-time lights rare.
- Prefer hit-scan over projectiles; particles stay pooled and short-lived.
- Watch draw calls: mark map geometry static (baked into static batching);
  avoid per-object materials where a shared cache works
  (`WeaponVisualFactory`/`MapBuilder` cache materials).
- `Application.targetFrameRate` is set by the quality preset — do not override
  it elsewhere.

## 11. Git rules

- Repo: `https://github.com/8002salman-ai/shootergame` — the repo **is** the
  Unity project root. Default branch `main`; feature work on `dev`.
- Never force push; never rewrite history; never delete remote history.
- Never commit: `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`, builds,
  keystores, credentials, `.env`, `google-services.json` (all covered by
  `.gitignore`).
- Committed: `Assets/`, `Packages/`, `ProjectSettings/ProjectVersion.txt`,
  `ProjectSettings/TagManager.asset`, `ProjectSettings/EditorBuildSettings.asset`,
  `Tools/`, `Documentation/`, `.gitignore`, README/AGENTS.
- Logical commits with conventional prefixes (`feat:`, `fix:`, `docs:`,
  `chore:`). Check `git status` before every push; verify the push with
  `git ls-remote` or `git log origin/main..HEAD`.

## 12. What Phase 2 should tackle (recommended order)

1. Open in Unity 6000.0.82f1, fix any compile/runtime issues, playtest.
2. Run **Blackzone ▸ 01/02/03** menus; produce the first APK.
3. Gameplay feel pass: recoil curves, hit feedback (blood/decal), enemy
   reaction tuning, weapon switch cancel-on-reload, sprint-ADS transitions.
4. Main menu + scene flow (menu scene → gameplay scene).
5. Interaction placeholder → real interactions (doors, pickups).
6. Vault/slide/lean movement layer on `FpsMovement`.
7. ELITE/BOSS difficulty profiles (data only).
8. Map polish pass with real art; weapon viewmodel meshes.
9. Then discuss: extraction prototype, squad AI, PvP — a separate phase with
   its own scope (backend, accounts, anti-cheat are far beyond Phase 2).

## 13. Reporting style

When reporting work: state exactly what was changed, what was validated and
how, and what remains unverified. Never pad with invented test results.
