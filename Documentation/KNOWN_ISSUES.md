# BLACKZONE — Known Issues & Risks

**Updated:** 2026-08-28 · Version: V0.01 (Phase 1)

## A. Environment-level (why some things are unverified)

1. **No Unity Editor compilation has occurred.** All C# was authored and
   statically validated with the tree-sitter C# parser (syntax) plus custom
   cross-file contract checks, but the Unity compiler has never run against
   this code. First open in Unity 6000.0.82f1 may surface API-level issues
   (e.g. URP property names, NavMeshSurface API details, uGUI specifics).
   This is the #1 item to verify.
2. **No APK exists yet** — no Android SDK/NDK/JDK in the authoring
   environment. Nothing has run on a phone.
3. **ProjectSettings.asset / QualitySettings.asset / InputManager.asset are
   intentionally not committed** (they caused the previous corruption issue).
   Unity regenerates defaults; menu **Blackzone ▸ 01** re-applies URP/quality.
   If the default InputManager lacks Mouse X/Y axes, reset it via
   `Edit ▸ Project Settings ▸ Input ▸ Reset`.

## B. Known technical risks (by system)

4. **MapBuilder NavMesh bake** — `NavMeshSurface.BuildNavMesh()` at runtime
   needs all colliders present (they are, synchronous build). Ramps use a
   ~38° slope, under the 45° agent limit; verify enemies actually climb the
   watchtower ramp.
5. **URP asset API** (`QualityApplier`, editor menu 01) uses
   `renderScale`, `msaaSampleCount`, `shadowDistance`, `mainLightCastShadows`
   — stable in URP 17, but verify on first open.
6. **uGUI fonts** rely on `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`
   — correct for Unity 6; if the asset name differs in a future patch,
   HUD text will log warnings.
7. **Enemy collider layering** — enemies are on layer 9 (Enemy); the player's
   bullets hit `World|Enemy|Interactable`; enemy bullets hit `World|Player`.
   Head colliders live on the head child object with `HitRegion`; body capsule
   on the root. Verify no shot passes through the gap between head and body.
8. **ADS while switching** — arsenal forces `SetAdsWanted(false)` on switch;
   edge cases (reload → switch → fire) should be exercised per
   TESTING_CHECKLIST items 3.5/3.6.
9. **Pause** uses `Time.timeScale = 0`; UI overlays use unscaled time. If a
   future system animates in `Update` with unscaled time it will move while
   paused — keep that convention.
10. **MobileInputProvider look accumulation** — look delta accumulates between
    `Sample()` calls and is cleared when the touch ends (`ClearLook`); verify
    no residual drift after long drags (test 7.2).
11. **Auto-restart while paused is impossible** (pause blocked in Dead state)
    — intended, but note it: dying then pausing is not a flow.
12. **`Random`-based spread** uses `UnityEngine.Random` (non-deterministic) —
    fine for gameplay, never for save/replay data.

## C. Content limitations (intentional for Phase 1)

13. All art/audio is procedural placeholder — weapon models are primitive
    boxes; there are no animations (enemies slide, body "falls" by rotating).
14. Interact prompt is a placeholder: nothing is interactive yet.
15. No main menu / scene flow; the game boots directly into the encounter.
16. No save system, no difficulty selection UI (data exists for ROOKIE/SOLDIER
    but the mix is fixed in `EnemySpawner`).
17. FPS counter only shown in dev builds (`Debug.isDebugBuild`).

## D. Known cosmetic issues

18. Head bob may clip the viewmodel at extreme bob phase — tune
    `bobAmount` in `FpsMovement` if noticeable.
19. Crosshair does not hide/change during ADS yet.
20. Toast event (`GameEvents.Toast`) is wired but nothing emits it yet.

## E. How to report

When a failure is found: record in `Documentation/TESTING_CHECKLIST.md` with
PASS/FAIL, include the Unity console stack trace or logcat snippet, and note
device/model + quality preset + measured FPS. Fixes should come with a
regression pass (section 8 of the testing checklist).
