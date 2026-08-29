# BLACKZONE V0.01 — Testing Checklist

How to validate each system. Column **S** = status to fill in
(`PASS` / `FAIL` / `N/A`). Static (no-Unity) checks are already green;
Editor/device checks must be performed by whoever opens the project.

## 0. Project health (no Unity needed)

- [x] `python3 Tools/validate_project.py` → all checks pass
- [x] `python3 Tools/generate_meta.py --check` → all assets have metas
- [x] `git status` clean on `main`; no secrets, caches or builds staged

## 1. First open (Unity Editor, Windows)

| # | Test | S |
| - | ---- | - |
| 1.1 | Project opens without console errors in Unity 6000.0.82f1 | |
| 1.2 | Menu **Blackzone ▸ 01** creates URP asset; no errors | |
| 1.3 | Menu **Blackzone ▸ 03** creates 4 weapon + 2 AI assets | |
| 1.4 | `Blackzone_Phase1.unity` opens; Play mode starts the encounter | |

## 2. Movement & camera

| # | Test | S |
| - | ---- | - |
| 2.1 | WASD moves; speed feels walk/sprint distinct | |
| 2.2 | Shift sprints only when moving forward; FOV kicks slightly | |
| 2.3 | Ctrl/C toggles crouch; camera drops; move speed lowers | |
| 2.4 | Space jumps; double-buffer jump near a ledge works | |
| 2.5 | Mouse look is smooth; pitch stops at limits (±88°) | |
| 2.6 | Sensitivity slider changes feel; ADS sensitivity scales | |
| 2.7 | Player cannot walk through containers/walls (capsule collision) | |
| 2.8 | Head bob subtle while walking; gone while ADS | |

## 3. Weapons & shooting

| # | Test | S |
| - | ---- | - |
| 3.1 | LMB fires; hold = auto on KESTREL/VIPER, semi on ANVIL/LONGBOW | |
| 3.2 | Fire rate matches RPM feel per weapon | |
| 3.3 | Ammo HUD decreases; reserve decreases on reload (R) | |
| 3.4 | Empty mag auto-reloads; dry-fire click when no reserve | |
| 3.5 | Reload cancels ADS; switching weapon cancels reload | |
| 3.6 | Keys 1–4 + Q/E switch weapons (0.25 s holster) | |
| 3.7 | RMB ADS: FOV narrows, spread shrinks, viewmodel centers | |
| 3.8 | Recoil differs per weapon; camera returns toward aim point | |
| 3.9 | Tracers/muzzle flash/impact sparks appear (pooled, no GC spikes) | |
| 3.10 | Headshots deal multiplied damage (see enemy HP behavior) | |

## 4. Player stats & death

| # | Test | S |
| - | ---- | - |
| 4.1 | Enemy shots reduce health bar; armor absorbs ~50% then depletes | |
| 4.2 | At 0 HP: input locks, K.I.A. screen, countdown, auto-restart at 0 | |
| 4.3 | RESTART NOW button works immediately | |
| 4.4 | After restart: player at spawn, full HP/armor/ammo, enemies respawned | |
| 4.5 | Pause (Esc/P or touch II) freezes the world; resume works | |
| 4.6 | Settings changes persist across a restart (PlayerPrefs) | |

## 5. Enemy AI

| # | Test | S |
| - | ---- | - |
| 5.1 | Enemies patrol waypoints; pause briefly at each | |
| 5.2 | Visible enemy within range/FOV engages; **not** through walls | |
| 5.3 | Reaction delay visible (ROOKIE slower than SOLDIER) | |
| 5.4 | Burst fire with spread; damage applies to player | |
| 5.5 | Enemies reposition during combat; stop chasing beyond range | |
| 5.6 | Break LOS: enemy searches last position, then returns to patrol | |
| 5.7 | Body shots damage; head shots kill faster (HitRegion works) | |
| 5.8 | Enemy death: tips over, becomes non-blocking, counter decrements | |
| 5.9 | Shooting an enemy from behind makes it aggro (snap engage) | |
| 5.10 | All 8 enemies active; no two spawn inside each other | |

## 6. Map

| # | Test | S |
| - | ---- | - |
| 6.1 | NavMesh baked: enemies navigate lanes, ramps, warehouse openings | |
| 6.2 | No enemy paths through walls/containers | |
| 6.3 | Watchtower ramp walkable; long sightline clear of cover | |
| 6.4 | Player cannot exit the map (walls + gates) | |
| 6.5 | FPS stable during fights (use built-in counter) | |

## 7. Mobile (Android device or touch emulation)

| # | Test | S |
| - | ---- | - |
| 7.1 | Left joystick appears where touched; movement matches direction | |
| 7.2 | Right side drag looks around; buttons respond on press | |
| 7.3 | FIRE hold = auto fire; ADS hold; reload/jump/crouch tap | |
| 7.4 | Weapon prev/next buttons cycle correctly | |
| 7.5 | HUD readable on a phone in landscape | |
| 7.6 | Pause button opens menu; settings sliders usable by touch | |
| 7.7 | 30/45/60 fps caps apply (measure actual FPS, log it) | |
| 7.8 | No crash on death/restart cycle (repeat 3×) | |
| 7.9 | App survives home-screen backgrounding (onPause) | |

## 8. Regression (after any change)

| # | Test | S |
| - | ---- | - |
| 8.1 | `validate_project.py` green | |
| 8.2 | Fresh clone → first open → menu 01 → Play works (no data assets needed) | |
| 8.3 | All of section 2–6 spot-checked | |

## Recording results

Fill the **S** column with PASS/FAIL and copy the filled file into the repo as
`Documentation/TEST_RESULTS_<date>.md` after a device session. Report measured
FPS as numbers only.
