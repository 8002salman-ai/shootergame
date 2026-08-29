# BLACKZONE — Game Design (Phase 1)

> Version: V0.01 · Status: playable vertical slice · Scope: single-player tech demo

## 1. Vision

BLACKZONE is an **original near-future tactical FPS** for mobile (Android first).
The full-game vision is a tactical extraction shooter: squad combat, AI
soldiers, PvP, looting, missions, extraction zones, persistent inventory and a
5v5 quick mode. **Phase 1 deliberately builds none of that** — it proves that
movement, shooting, AI and touch controls feel right.

## 2. Setting & atmosphere

- **Desert + industrial military zone.** A remote outpost at the edge of
  contested territory: shipping containers, warehouses, concrete barriers,
  sand, dust, watchtowers.
- **Tone:** clean, modern, tactical. Dark charcoal UI, restrained accents,
  readable at arm's length on a phone.
- **Originality rule:** everything is original BLACKZONE IP. No assets, names,
  UI or skins from existing shooters. Genre inspiration only.

## 3. Phase 1 gameplay loop

```
Spawn at the south gate of the Training Outpost
  → fight through container yard / warehouses / center lanes
  → kill 8 AI soldiers (ROOKIE + SOLDIER)
  → die or survive → restart the encounter (8s auto or manual)
```

The encounter has **no win condition beyond combat mastery** — it is a sandbox
for testing guns, movement and AI. Kill count is tracked in the HUD.

## 4. Player

| Stat | Value | Notes |
| ---- | ----- | ----- |
| Health | 100 | configurable, `GameConstants` |
| Armor | 50, absorbs 50% | durability depletes as it absorbs |
| Move speed | 4.2 m/s walk · 6.4 sprint · 2.1 crouch | sprint = forward only |
| Jump | 1.15 m | coyote 0.15 s, buffer 0.12 s |
| Camera | FOV 72° hip, ~42–60° ADS per weapon | pitch clamped ±88° |

Death: combat input locks, death screen, restart. Restart resets position,
health, armor, ammo and all enemies.

## 5. Weapons (original prototypes)

All four use one shared framework (`WeaponRuntime` + `WeaponDefinition` data).

| # | Name | Class | Damage | Headshot | RPM | Mag/Reserve | Reload | Role |
| - | ---- | ----- | ------ | -------- | --- | ----------- | ------ | ---- |
| 1 | **KESTREL K-17** | Assault Rifle | 24 | ×1.8 | 620 | 30/90 | 2.1 s | all-round |
| 2 | **VIPER V-9** | SMG | 16 | ×1.7 | 850 | 32/128 | 1.8 s | close range |
| 3 | **ANVIL A-12** | Shotgun | 11 × 8 pellets | ×1.5 | 80 | 6/24 | 2.6 s | CQC |
| 4 | **LONGBOW LB-7** | Marksman | 58 | ×2.2 | 150 | 10/40 | 2.4 s | long range |

- **Recoil:** vertical kick + random horizontal, smooth recovery; distinct per
  weapon; ADS reduces recoil and spread.
- **Ammo:** magazine + reserve; empty mag auto-reloads if reserve exists;
  manual reload anytime (R / button).
- **FX:** pooled tracers, muzzle flashes, impact sparks; all SFX synthesized.

## 6. Enemy AI

Prototype soldiers with a compact state machine:

```
IDLE → PATROL → (detect) → ENGAGE → (lose target) → SEARCH → RETURN → PATROL
                       ↑  └─(reposition, bursts)                     
               (damaged) └──────────────┘
```

- **Detection:** range + FOV + **line-of-sight raycast** (can't see through
  walls), checked every 0.25 s (mobile CPU).
- **Engage:** reaction delay (per difficulty), burst fire with inaccuracy,
  periodic repositioning.
- **Losing the player:** search the last known position, then return to patrol.
- **Difficulties (data-driven):**

| | ROOKIE | SOLDIER |
| - | ------ | ------- |
| Health | 80 | 120 |
| Detection | 34 m / 95° | 46 m / 110° |
| Reaction | 0.7 s | 0.35 s |
| Accuracy | 3.6° | 2.2° |
| Burst | 3 × 0.13 s | 5 × 0.09 s |
| Damage/shot | 8 | 11 |

## 7. Map — BLACKZONE TRAINING OUTPOST

~140 × 90 m desert yard, walled, two gates (north/south):

- **Container yard (east):** stacked containers, 2 m lanes, rotated containers —
  close-quarters combat.
- **Two warehouses (west):** wall openings, interior crates — mid-range fights.
- **Center barriers:** jersey-barrier rows with gaps — cover lanes.
- **Watchtower (north-east):** ramp-accessible elevated position.
- **Kill lane:** one long open sightline north → south.
- **8 enemy spawns, 10 patrol waypoints.**

## 8. UI (Phase 1)

- **HUD:** health bar, armor bar, ammo `mag / reserve`, weapon name,
  crosshair, hitmarker (white = hit, red = kill), reload indicator,
  `HOSTILES: alive/total`, FPS counter (dev builds), interact prompt
  (placeholder, hidden).
- **Touch controls:** dynamic left joystick, right-side look surface, buttons:
  FIRE / ADS / reload / jump / crouch / weapon prev-next / pause.
- **Overlays:** pause (resume/restart/settings/quit), settings (sensitivity,
  ADS sensitivity, master/effects volume, LOW/MED/HIGH graphics), death screen
  (K.I.A., restart now, auto-restart countdown).

## 9. Settings & performance targets

| Preset | FPS target | Render scale | Shadows | MSAA |
| ------ | ---------- | ------------ | ------- | ---- |
| LOW | 30 | 0.7 | off (dist 12 m) | 0 |
| MEDIUM | 45 | 0.85 | soft (25 m) | 2 |
| HIGH | 60 | 1.0 | soft (45 m) | 4 |

## 10. Explicitly out of scope (Phase 1)

Multiplayer, extraction persistence, looting/inventory, missions, store,
payments, ads, battle pass, clans, ranking, accounts, chat, voice, backend,
anti-cheat, large open world, animations, real art.
