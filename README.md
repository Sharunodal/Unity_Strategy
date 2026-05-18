# Unity Strategy Prototype

A Unity strategy prototype that combines RTS-style unit selection and commands with melee/ranged combat, staged progression, recruitable units, and persistent player squads.

Short video showcasing a very early build:  
https://www.youtube.com/watch?v=bBCE_ZFzDKE

## Project status

This project is being developed in Unity `6000.4.7f1` / Unity 6.4. It is still a prototype, but the core loop now supports selecting units, issuing commands, fighting enemies, recruiting allies, clearing stages, carrying surviving player units forward, and retrying/restarting after defeat.

## Key systems

- RTS-style click and drag-box selection through `PlayerController` and `SelectionManager`.
- Command routing for selected units via `CommandSystem`, `UnitCommandReceiver`, and command types such as move, attack, and follow.
- Unit state and combat behavior in `UnitBrain`, including movement, melee, ranged attacks, blocking, following, and conversations.
- Melee and ranged combat through `WeaponHitbox`, `BowWeapon`, `ArrowProjectile`, `Hurtbox`, and `BlockController`.
- Animation integration through `UnitAnimator` and `CombatAnimationEvents`.
- Player UI and game flow through `GameManager`, including pause, game over, restart, and retry stage.
- Faction relationships and campaign state through `GameProgress` and `FactionRelations`.
- Scene-level faction counts, player defeat, and stage-clear checks through `FactionUnitTracker`.
- Stage exits through `StageExitTrigger`, including objective-gated exits and optional requirements for all living player units to enter the trigger.
- Saved player squad spawning through `StageUnitPersistence` and `UnitSpawnPoint`.
- Recruitable allies through `RecruitableUnit`.
- Environment helpers such as `SeeThroughWindow` and `BuildingInteriorTrigger`.

## Stage progression and persistence

Player units are saved when advancing through a `StageExitTrigger`. On the next stage, `StageUnitPersistence` restores saved units by spawning configured prefabs at `UnitSpawnPoint` locations.

To set up a stage that receives saved units:

1. Add one `StageUnitPersistence` component to the scene.
2. Assign the scene's `FactionUnitTracker`.
3. Add one or more `UnitSpawnPoint` objects to the scene.
4. Add entries to `Unit Prefabs`, mapping saved unit `Persistent Id` values to the prefabs that should be spawned.

Each spawn point is used once. If a saved unit has no prefab mapping or there are not enough spawn points, the project logs a warning.

## Stage clear and game over

`FactionUnitTracker` tracks living unit counts per faction. It can:

- trigger game over when the player faction reaches zero units;
- unlock a configured `StageExitTrigger` when enemies are defeated;
- count either a specific enemy faction or all factions hostile to the player.

Units must be assigned to the scene's `FactionUnitTracker` when they should participate in stage clear or game over logic.

## Controls

Default behavior is wired through Unity's Input System actions:

- Left click: select a unit.
- Left mouse drag: box-select owned units.
- Additive select action: keep current selection while adding units.
- Right click: issue move, follow, or attack orders.
- `Player/ToggleRun`: toggle running for selected units.
- `Player/ToggleBlock`: toggle blocking for selected units.
- `Player/ToggleAutoCombo`: toggle auto-combo behavior when available.
- `Player/Stop`: stop selected units.
- `Player/EquipSword`: equip sword.
- `Player/EquipBow`: equip bow.
- `UI/Cancel`: pause.

## Setup notes

- Open the project with Unity `6000.4.7f1` or a compatible Unity 6 editor.
- The project uses Unity's Input System and TextMeshPro.
- Scene references are generally assigned manually in the Inspector.
- Unit `Persistent Id` values are important for saving and restoring carried units.
- Unit prefabs spawned by `StageUnitPersistence` should include the normal unit components, command receiver, selectable setup, AI/brain setup, and visual/animation dependencies expected by that unit type.

## Assets and editor work

- Character and object models were created in Blender and imported into Unity.
- Unity Animator controller state machines and animation logic are included.
- Custom ProBuilder geometry and scene layout assets are present.
- TextMeshPro resources, UI logic, and general Unity project settings are included in the repository.
