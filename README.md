# Unity Strategy Prototype

A compact Unity prototype combining RTS-style unit selection and commands with close-quarters melee and ranged combat. The repository contains modular C# systems, scene and editor assets, and animation logic so the project can be inspected and extended within the Unity Editor.

## Key systems and features
- Unit selection and command routing via `SelectionManager` and `UnitCommandReceiver`.
- Unit AI and command execution (`UnitBrain`, `IUnitCommand`, `MoveCommand`, `AttackCommand`, `FollowCommand`).
- Combat systems: melee hitboxes, ranged bows with ballistic arrows, and blocking (`WeaponHitbox`, `BowWeapon`, `BlockController`).
- Per-unit stats and equipment with UI binding via `GameManager` and `Unit`.
- Animation integration through Animator controllers (`UnitAnimator`) and animation-triggered events.
- Environment/visual helpers: see-through window masking and interior triggers (`SeeThroughWindow`, `BuildingInteriorTrigger`).
- Hitbox / hurtbox targeting (`Hurtbox`).

## Art, assets and editor work
- Character and object models were created in Blender and imported into Unity.
- Unity Animator controller state machines and animation logic are included.
- Custom ProBuilder geometry and scene layout assets are present.
- UX logic, TextMeshPro shaders, and general Unity Editor project settings are part of the repository.

## Getting started
1. Open the project in the Unity Editor (use a Unity version compatible with the project's packages).
2. Ensure the new Input System and TextMeshPro packages are installed (the code uses Input System actions and TMP UI).
3. Open the main scene and press Play.

## Controls (default Input Action names)
- Left click: select unit(s) (supports additive select).
- Right click: issue move or attack/follow orders.
- `Player/ToggleRun`: toggle running for selected units.
- `Player/ToggleBlock`: toggle blocking for selected units.
- `Player/EquipSword`: equip sword on selected units.
- `Player/EquipBow`: equip bow on selected units.
- `UI/Cancel`: pause.

## Notes and tips
- If key bindings do not match your environment, open the Input Actions asset in the Editor and verify bindings for `Player/*` and `UI/Cancel`.
- Review `Project Settings` and package versions if you encounter missing-package errors.
- The codebase is intentionally modular to make AI, weapons, and animation hooks straightforward to extend.
- TextMeshPro resources and custom shaders are included for UI rendering; inspect `Assets/TextMesh Pro` for examples.