# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

CrossFire is a 2D space shooter built in **Unity 6 (6000.0.28f1)** using **Unity DOTS/ECS** (Data-Oriented Technology Stack). It is a learning project exploring ECS architecture and AI-assisted development.

## Building & Running

There are no CLI build scripts. The project is built and run through the **Unity Editor** or **Visual Studio**. Open `CrossFire/CrossFire.sln` in Visual Studio for code editing with full IntelliSense.

Tests are split into two Unity test suites: `Tests.EditMode` and `Tests.PlayMode`, run via Unity Test Runner (Window → General → Test Runner).

Key scenes in `Assets/_Game/Scenes/`:
- `Gameplay.unity` — main gameplay scene
- `HexMap.unity` — hex map demo

## Code Style

- **Explicit types** everywhere (no `var` unless the type is obvious from the right-hand side)
- **Explicit, descriptive variable names** — no abbreviations
- **Always use braces**, even for single-line `if`/`for` bodies
- Preserve existing comments; add comments where logic is not self-evident
- Naming:
  - Public fields/properties: `PascalCase`
  - Private fields: `_lowerCamelCaseWithLeadingUnderscore`
  - Constants: `SCREAMING_SNAKE_CASE`

## Architecture

### Assembly Structure

Scripts live in `CrossFire/Assets/_Game/Scripts/` and are split into assemblies with explicit dependency direction:

```
CrossFire.App          ← orchestrates everything
CrossFire.Combat       ← weapons, bullets, damage
CrossFire.Ships        ← spawning, ship prefab registry
CrossFire.Targeting    ← player input, ship selection, AI intent
CrossFire.Core         ← shared components (Health, TeamId, ControlIntent)
CrossFire.Physics      ← standalone custom 2D physics engine
CrossFire.HexMap       ← hexagonal map grid
CrossFire.Utilities    ← helpers
```

### ECS Patterns

All gameplay systems use **unmanaged `ISystem` structs** with `[BurstCompile]` and `[DisableAutoCreation]`. Systems are never auto-created by Unity; they must be manually registered.

### Simulation Pipeline

`AppSimulationPipeline` (`Bootstrap/AppSimulationPipeline.cs`) is a single `ComponentSystemGroup` inside `SimulationSystemGroup` with `EnableSystemSorting = false`. All systems are added in explicit order:

1. **Bootstrap** — `CollisionEventBufferBootstrapSystem`, command buffer systems, `LookupBootstrapSystem`
2. **Snapshot** — `SnapshotSystem` (saves previous frame pose)
3. **Spawn** — `ShipsSpawnSystem`
4. **Intent/Decision** — input, selection, `PlayerIntentSystem`, AI movement intent
5. **Movement** — `ShipMovementSystem`, `WeaponCooldownSystem`, `WeaponFireSystem`, physics integration (`LinearDampingSystem` → `AngularIntegrationSystem` → `PositionIntegrationSystem` → `MaxVelocityClampSystem`)
6. **Physics** — `CollisionDetectionSystem` (broadphase grid + narrowphase triangle tests)
7. **Combat reactions** — bullet update/damage/destroy, `DeathSystem`, `PostPhysicsSystem`
8. **Cleanup/Presentation** — `CollisionEventCleanupSystem`, `ColorPresentationSystem`, `CollisionDebugSystem`

### Custom Physics

The project uses a **fully custom 2D physics engine** (no Box2D / Unity Physics):
- Components: `WorldPose`, `PrevWorldPose`, `Velocity`, `AngularVelocity`, `Collider2D`, `CollisionLayer`, `CollisionMask`
- Broadphase: grid-based cell partitioning (`CollisionGridSettings`)
- Narrowphase: triangle-based intersection tests
- Integration: velocity Verlet with angular integration and `LinearDamping`
- Collision results written to a `DynamicBuffer<CollisionEvent>` on a singleton entity tagged with `CollisionEventBufferTag`

Authoring components (MonoBehaviours that bake into ECS): `BasicBodyAuthoring`, `DynamicBodyAuthoring`, `MaxVelocityAuthoring`, `LinearDampingAuthoring`, `ColliderAuthoring`, `CollisionGridSettingsAuthoring`.

### Command / Event Pattern

Deferred operations use ECS command buffers:
- `SpawnShipsCommand` / `ShipsSpawnCommandBufferSystem` — spawning ships
- `ShipControlIntentCommand` / `ShipControlIntentCommandBufferSystem` — applying control intent
- `SelectionRequestCommand` / `ClickPickRequestBufferSystem` — ship selection from player clicks

### Lookup System

A `LookupBootstrapSystem` + `LookupSnapshotSystem` pair maintains fast entity lookup tables (used by targeting and AI) that are snapshotted each frame before intent systems run.

## Important Constraints

- **Never modify `.meta` files, scene files (`.unity`), or binary asset files** — these are managed by Unity and should not be edited as text.
- Do not add systems to `[UpdateInGroup]` attributes; all ordering is controlled manually in `AppSimulationPipeline`.
- New gameplay systems must be added to `AppSimulationPipeline` in the correct phase slot and marked `[DisableAutoCreation]`.
