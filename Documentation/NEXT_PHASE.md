# BLACKZONE — Next Phase (recommended, NOT started)

> Phase 1 is complete in code. The next milestone is **Phase 2: First Verified
> Build** — get the project opened, compiled, playable and on a phone before
> adding new features. Do not skip this.

## Phase 2 — First Verified Build (highest priority)

1. **Open in Unity 6000.0.82f1** on Windows; fix any compile/console errors;
   run the full `Documentation/TESTING_CHECKLIST.md`.
2. Run editor menus **Blackzone ▸ 01 / 02 / 03**; commit any regenerated
   settings that should be shared (never `ProjectSettings.asset` wholesale —
   review first).
3. **Produce the first APK** (`Documentation/ANDROID_BUILD_GUIDE.md`) and test
   on a real device: input feel, FPS, heat, crash-on-restart.
4. Record actual FPS numbers per quality preset; tune presets to hit
   30/45/60 targets.

## Phase 2 — Gameplay feel pass (after the build exists)

5. Recoil/spread tuning per weapon (data-only, via `Resources/Weapons/*.asset`).
6. Hit feedback: impact decals, enemy blood/flinch (replace placeholder tint).
7. Enemy difficulty balance (ROOKIE/SOLDIER data pass).
8. Movement tuning: acceleration, jump feel, head bob, sprint→ADS transition.
9. Weapon switch should cancel reload cleanly; add reload progress bar.

## Phase 2 — Structure additions

10. Main menu scene + scene flow (menu → gameplay → restart loop).
11. Real interactables (doors, ammo pickups) replacing the prompt placeholder.
12. Vault / slide / lean layer on top of `FpsMovement` (architected for it).
13. ELITE/BOSS difficulty profiles (data only, one day of work).
14. First real-art pass: weapon viewmodels, enemy soldier model, map props.

## Phase 3+ (only after 1–14, with its own scope document)

- Extraction prototype: loot, extraction zones, persistent player state.
- Squad AI cooperation (fire-and-maneuver between soldiers).
- 5v5 quick mode: networking, matchmaking, accounts, backend, anti-cheat —
  each is a full project on its own; explicitly out of Phase 1/2.

## Guardrails for the next agent (FREEBUFF or similar)

- Read `AGENTS.md` first — it is the operating manual for this repo.
- Keep the bootstrapper architecture: never rebuild the project by hand.
- Never commit `Library/Temp/Obj/Logs/UserSettings`, keystores or secrets.
- Never force-push; branch off `main` for experiments.
- Validate with `Tools/validate_project.py` before every commit.
- Report only verified results (no invented compile/device test claims).
