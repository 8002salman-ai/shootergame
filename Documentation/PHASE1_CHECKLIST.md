# BLACKZONE V0.01 — Phase 1 Checklist

Status legend: ✅ done · ⏳ pending (needs Unity Editor) · ➖ not in Phase 1

## A. Project & tooling

- [x] Git repo connected to `https://github.com/8002salman-ai/shootergame`
- [x] Unity 6 (6000.0.82f1) project structure + package manifest (URP 17.0.3)
- [x] Layer definitions (Player 3, UI 5, World 8, Enemy 9, Interactable 10)
- [x] Scene `Blackzone_Phase1.unity` with bootstrapper + build settings entry
- [x] `.gitignore` (Library/Temp/Obj/Logs/UserSettings/builds/keystores/secrets)
- [x] `Tools/generate_meta.py` (deterministic GUIDs) — all metas generated
- [x] `Tools/validate_project.py` — C# syntax (tree-sitter), contracts, GUIDs,
      layers — **all static checks pass**
- [x] README.md + AGENTS.md + Documentation/ set

## B. Input

- [x] Desktop provider: WASD, mouse look, LMB fire, RMB ADS, Shift sprint,
      Ctrl/C crouch, Space jump, R reload, 1–4 switch, Q/E cycle, Esc/P pause
- [x] Mobile provider: joystick, look surface, FIRE/ADS/reload/jump/crouch/
      prev/next/pause buttons
- [x] Unified `GameInput` facade; gameplay never reads raw input

## C. Movement & camera

- [x] Walk / sprint (forward-only, blocked by ADS) / crouch (toggle) / jump
- [x] Gravity, grounded checks, coyote time, jump buffer
- [x] Acceleration / deceleration, head bob
- [x] Pitch clamp ±88°, sensitivity + ADS sensitivity scaling
- [x] Recoil punch + smooth recovery, ADS FOV blend, sprint FOV kick
- ⏳ Feel tuning pass in Editor (speeds/gravity/jump numbers are defaults)

## D. Weapons & shooting

- [x] Data-driven `WeaponDefinition` + `WeaponCatalog` (4 original weapons)
- [x] Fire-rate, semi/auto, magazine/reserve ammo, dry-fire, auto-reload
- [x] Reload (cancel-to-switch behavior via arsenal), weapon switching 0.25 s
- [x] ADS (spread reduction, FOV, viewmodel), hip fire
- [x] Hit-scan with headshot multipliers, hitmarkers, kill feedback
- [x] Recoil (vertical + horizontal + recovery) per weapon
- [x] Pooled tracer / muzzle flash / impact FX
- ⏳ Visual/audio tuning of the 4 guns in Editor

## E. Combat stats

- [x] Health 100 (configurable), damage flow via `DamageInfo`
- [x] Armor 50, absorbs 50%, depletes; shown in HUD
- [x] Player death (input lock, death screen, 8 s auto-restart, manual restart)
- [x] Encounter restart resets position/health/armor/ammo/enemies

## F. Enemy AI

- [x] FSM: Idle/Patrol/Investigate/Engage/Search/Return/Dead
- [x] Patrol waypoints; detection = range + FOV + LOS raycast (throttled)
- [x] Engage: reaction delay, burst fire with spread, repositioning
- [x] Lose target → search → return to patrol
- [x] Receive damage, snap-aggro, headshots, death, respawn on restart
- [x] ROOKIE + SOLDIER data profiles (ELITE/BOSS slots architected)
- ⏳ Editor: NavMesh bake verification, pathfinding sanity on ramps/tower

## G. Map

- [x] Training Outpost: ground, perimeter + gates, container yard (stacked,
      lanes, rotated), 2 warehouses, barriers with gaps, watchtower + ramp,
      crate clusters, long sightline
- [x] NavMeshSurface runtime bake on World layer
- [x] Sun + fill light, fog, flat ambient; sun shadows driven by quality
- ⏳ Editor: visual inspection, spawn/fighting position balance

## H. UI

- [x] HUD: health/armor bars, ammo, weapon name, crosshair, hitmarker,
      reload text, enemy counter, FPS (dev), interact placeholder
- [x] Mobile: dynamic joystick, look surface, action buttons (no logic in UI)
- [x] Pause menu (resume/restart/settings/quit), Esc/P + touch button
- [x] Settings screen: sensitivity, ADS sensitivity, master/effects volume,
      LOW/MEDIUM/HIGH quality
- [x] Death screen with countdown + restart
- ⏳ Editor: layout review at 16:9 and phone aspect ratios

## I. Settings / performance

- [x] GameSettings (PlayerPrefs) + QualityApplier (30/45/60 fps, render scale,
      shadows, MSAA)
- [x] No per-frame allocations in hot paths; pooled FX; throttled AI
- [x] Static-batched map geometry; material caching
- ⏳ Real device FPS measurement (report only actual numbers)

## J. Audio

- [x] Procedural SFX: fire (player/enemy), reload, empty, hit, kill,
      player/enemy death, click; master + effects volumes
- ⏳ Volume/feel balance in Editor

## K. Android

- [x] Editor menu: URP + quality levels; Android Player Settings
      (ARM64/IL2CPP/landscape/package id); weapon+AI data assets
- [x] `ANDROID_BUILD_GUIDE.md` with module/toolchain/USB steps
- ⏳ Install Android Build Support; run menu 02; build APK; on-device test
- ➖ iOS build (later phase)

## L. Do-not-build discipline (must stay NO)

- [x] No multiplayer, backend, accounts, store, ads, battle pass, ranked,
      clans, chat/voice, anti-cheat, extraction persistence, huge map
