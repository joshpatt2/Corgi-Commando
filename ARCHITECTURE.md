# Corgi Commando — Architecture

## System Relationship Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        GAME LOOP                                │
│                                                                 │
│  ┌──────────────┐    ┌────────────────────┐                     │
│  │ Unity Input   │───▶│  1. InputBuffer    │                     │
│  │ System        │    │  (per player)      │                     │
│  └──────────────┘    └────────┬───────────┘                     │
│                               │                                  │
│                               ▼                                  │
│  ┌────────────────────────────────────────────┐                  │
│  │           2. Entity + Components           │                  │
│  │  ┌──────────┐ ┌──────────┐ ┌───────────┐  │                  │
│  │  │ Health   │ │ Hurtbox  │ │ Knockback │  │                  │
│  │  │Component │ │Component │ │ Receiver  │  │                  │
│  │  └──────────┘ └──────────┘ └───────────┘  │                  │
│  └───────────┬───────────────────┬────────────┘                  │
│              │                   │                                │
│              ▼                   ▼                                │
│  ┌──────────────────┐  ┌─────────────────┐                      │
│  │ 3. Kinematic     │  │  4. Combat      │                      │
│  │ Movement         │  │  System         │                      │
│  │ Controller       │  │  (singleton)    │                      │
│  └────────┬─────────┘  └───┬──────┬──────┘                      │
│           │                │      │                              │
│           ▼                ▼      ▼                              │
│  ┌─────────────────┐  ┌──────────────────┐                      │
│  │ 5. Corgi        │  │  6. Enemy AI     │                      │
│  │ Controller      │  │  (FSM per type)  │                      │
│  │ (state machine) │  │  + AggroSlots    │                      │
│  └────────┬────────┘  └──────┬───────────┘                      │
│           │                  │                                   │
│           ▼                  ▼                                   │
│  ┌──────────────────────────────────────┐                        │
│  │         7. Camera System             │                        │
│  │  GroupTargetCamera + ArenaCameraLock  │                        │
│  └──────────────────┬───────────────────┘                        │
│                     │                                            │
│           ┌─────────┼─────────┐                                  │
│           ▼         ▼         ▼                                  │
│  ┌──────────┐ ┌──────────┐ ┌──────────────────┐                 │
│  │ 8. Spawn │ │ 9. Env   │ │ 10. Boss         │                 │
│  │ & Waves  │ │ Weapons  │ │ (Whiskerbot)     │                 │
│  └────┬─────┘ └──────────┘ └──────────────────┘                 │
│       │                                                          │
│       ▼                                                          │
│  ┌──────────────────┐    ┌─────────────────┐                    │
│  │ 11. RunState     │───▶│ 12. UI / HUD    │                    │
│  │ (co-op shared)   │    │ (safe area)     │                    │
│  └──────────────────┘    └─────────────────┘                    │
│                                                                  │
│  ┌──────────────────────────────────────────┐                    │
│  │  13. Platform Build Config (macOS/iOS)   │                    │
│  └──────────────────────────────────────────┘                    │
└─────────────────────────────────────────────────────────────────┘
```

## Subsystem Descriptions

### 1. Input Abstraction
Wraps Unity Input System behind a timestamped buffer. All gameplay systems read from `IInputBuffer`, never raw input. This enables input replay, AI-driven players, and platform-agnostic input handling. One `PlayerInputHandler` per player bridges Unity's events to the buffer.

### 2. Entity Base & Components
Composition-based entity system. Every combat actor (player, enemy, weapon, boss) is an `Entity` with attached `IEntityComponent` instances — `HealthComponent`, `HurtboxComponent`, `KnockbackReceiver`. Faction enum determines targeting rules. Static level geometry is not an entity.

### 3. Movement Controller
Custom kinematic 2.5D movement. X = horizontal, Z = depth into screen, Y = jump (gravity-driven). No Rigidbody2D dynamics — pure `transform.position += velocity * dt` with manual grounded checks. SpeedMultiplier supports environmental weapon slowdown.

### 4. Combat System
Singleton that centralizes all hit resolution. Checks Z-band tolerance (±0.5 units), hurtbox state, faction, then applies damage, knockback, hitstop, and fires events. Manages combo counters (2s timeout) and special meter (fills on hit). All combat data is driven by `AttackData` ScriptableObjects.

### 5. Player Controller
State machine for the corgi: Idle, Walk, Attack1/2/3, Hit, Knockdown, GetUp, Special, PickupHold. Reads from `InputBuffer` each tick, validates state transitions, chains the Punch-Punch-Kick combo from `CorgiData`. Special move requires full meter.

### 6. Enemy AI
Lightweight FSM per enemy type: Idle → Chase → Attack → Stunned → Recover. `FeralCatAI` (simple melee), `RaccoonBanditAI` (steals Treats, flees), `SprinklerTurretAI` (fixed, telegraphed shots). `AggroSlotManager` limits 2 attackers per player — overflow enemies circle at range.

### 7. Camera
Cinemachine Target Group frames both players. `GroupTargetCamera` enforces a soft 10-unit horizontal distance cap. `ArenaCameraLock` clamps camera bounds during wave encounters and boss fights, unlocking on wave clear.

### 8. Spawn & Wave Management
`SpawnManager` drives wave-based encounters from `WaveData` ScriptableObjects. Tracks alive enemy count, fires wave clear events, coordinates with arena camera locks. Supports multi-wave encounters with configurable delays.

### 9. Environmental Weapons
Classic brawler pickup objects (trash lid, gnome, rake, mailbox, tennis ball). Press Special to grab, light attack to swing (multi-use), heavy to throw (single-use, consumes). Breaks after N uses. Holding slows movement via SpeedMultiplier.

### 10. Boss Controller
WHISKERBOT-9000: three-phase mech fight. Phase transitions at 75% and 35% HP. Phase 1: stomp + claws. Phase 2: debris, missiles, laser pointer (chase-tax mechanic). Phase 3: pilot ejects as separate Entity for a 1v1 finish.

### 11. Co-op Run State
`RunState` ScriptableObject holds shared lives pool and Treats counter. `ReviveSystem` allows partner revive by standing nearby for 3 seconds. Drop-in/drop-out: P2 joins without resetting Treats. Game over when both dead and no lives remain.

### 12. UI
`HUDController` manages health bars, special meter, wave indicator. `ComboCounterUI` shows x2/x3/etc and fades on combo break. `BossBannerUI` shows boss name + health bar with phase-change flash. Pause halts time, switches input to UI map. All UI anchored to safe areas for iOS.

### 13. Platform Build Config
`PlatformSettings` ScriptableObject stores build-target settings. `PlatformBuildConfig` provides runtime queries (IsIOS, GetSafeArea). iOS: IL2CPP, ARM64, landscape-locked, iOS 13+. macOS: Mono/IL2CPP, Universal (Apple Silicon + Intel).

## Data Flow

```
AttackData SO ──▶ CombatSystem.ResolveAttack() ──▶ HitResult
                         │
                         ├──▶ HealthComponent.TakeDamage()
                         ├──▶ KnockbackReceiver.ApplyKnockback()
                         ├──▶ Hitstop (frame pause)
                         ├──▶ Combo counter increment
                         └──▶ Special meter fill

WaveData SO ──▶ SpawnManager ──▶ Enemy instantiation ──▶ EnemyAI.Initialize()
                    │
                    └──▶ ArenaCameraLock.Activate() / Deactivate()

CorgiData SO ──▶ CorgiController.Initialize() ──▶ combo chain, stats, special
```

## Key Design Decisions

1. **Composition over inheritance** — Entities compose behaviors via IEntityComponent, not deep class hierarchies.
2. **Centralized combat** — All hit resolution goes through CombatSystem to prevent per-entity inconsistencies.
3. **Data-driven tuning** — All combat numbers live in ScriptableObjects, tunable without code changes.
4. **Input buffer** — Gameplay reads buffered input, not raw. Enables input leniency and future replay/AI.
5. **Custom kinematic movement** — No Rigidbody2D dynamics. Full control over 2.5D positioning.
6. **Z-band hit detection** — ±0.5 unit tolerance creates the classic brawler "same lane" feel.
