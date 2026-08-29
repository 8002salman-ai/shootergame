# BLACKZONE runtime data (ScriptableObjects)

Designer-editable data lives here so it can be loaded at runtime with
`Resources.LoadAll` — no scene serialization needed:

- `Resources/Weapons/*.asset` — weapon definitions (4 prototype weapons)
- `Resources/AI/*.asset` — AI difficulty profiles (ROOKIE, SOLDIER)

Created via the editor menu **Blackzone → 03 - Create Weapon + AI Data Assets**
(they are generated from the same code catalogs the game falls back to, so the
game is playable even before you run the menu).

Editing values in the Inspector is the supported tuning workflow.
