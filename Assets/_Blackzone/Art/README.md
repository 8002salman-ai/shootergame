# BLACKZONE placeholder art

All Phase 1 art is **procedural and runtime-generated** — no binary art assets exist
in the repo, which keeps the repository small and fully reproducible:

| Folder | Content |
| ------ | ------- |
| `Art/Materials` | (reserved) authored materials for later phases |
| `Art/Models` | (reserved) real weapon/enemy/environment meshes |
| `Art/Textures` | (reserved) albedo/normal/roughness textures |
| `Art/VFX` | (reserved) particle systems / VFX graphs |

Runtime builders (single source of truth for visuals):

- `Scripts/Weapons/WeaponVisualFactory.cs` — primitive gun viewmodels per class
- `Scripts/World/MapBuilder.cs` — full map geometry from primitives
- `Scripts/AI/EnemySoldier.cs` — enemy capsule/head/gun visuals
- `Scripts/Weapons/WeaponFx.cs` — pooled tracers, muzzle flashes, impacts

Replacing any of these with real art later requires **zero gameplay changes** —
each builder is self-contained.
