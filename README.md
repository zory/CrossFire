# CrossFire
Just random small project to see Unity ECS capabilities and how AI ccan help in development. Two things I always wanted to incorporate.

# CrossFire Physics (DOTS / ECS)

Custom 2D physics pipeline for a space shooter.

# Packages
I have several packages which I use and develop for UI, for basic math, etc. For now it is private as there are some code which might be "borrowed". Idea is to make it public once I will clean it up from all those files or assets.

---

# Authoring Components

## BasicBodyAuthoring

```
PrevWorldPose
WorldPose
LocalTransform (Unity)
```

## DynamicBodyAuthoring

```
Velocity
AngularVelocity
```

## MaxVelocityAuthoring

```
MaxVelocity
```

## LinearDampingAuthoring

```
LinearDamping
```

## CollisionGridSettingsAuthoring

```
CellSize
```

## ColliderAuthoring

```
ColliderType - (Circle, ConcaveTriangles)

OutlineVertices - Only used for ConcaveTriangles

ColliderBoundRadius - Circle around collider used for broadphase

ColliderCircleRadius - Only used for Circle collider
```

---

# Physics Systems Order

## InitializationSystemGroup

```
CollisionEventBufferBootstrapSystem - Creates collision event buffer
```

---

## SimulationSystemGroup

```
SnapshotSystem - Save previous frame pose

LinearDampingSystem - Apply drag

AngularIntegrationSystem - Rotate entities

PositionIntegrationSystem - Move entities

MaxVelocityClampSystem - Clamp velocity

CollisionDetectionSystem - Broadphase grid, Narrowphase triangle tests, Generate collision events

PostPhysicsSystem - Sync physics pose to LocalTransform

CollisionEventCleanupSystem - Clear collision events
```

---

## PresentationSystemGroup

```
CollisionDebugSystem - Draw collision debug overlay
```

---

# Physics Debugger

![Physics Debugger](Images/PhysicsDebugger.png)
