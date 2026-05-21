# Corgi Commando — Prototype Vertical Slice

A 2.5D side-scrolling beat-em-up where elite corgis defend suburbia. 1980s Saturday morning cartoon energy. Inspired by TMNT, Streets of Rage, River City Ransom.

**Prototype scope:** One playable corgi (Sarge), one level (Backyard Breakout), 3 enemy types + mini-boss + stage boss, solo + 2-player local co-op. macOS + iOS.

## Project Status

This repo contains the **architecture skeleton and test suite only**. All method bodies are stubs (`throw new NotImplementedException()`). The test suite defines what "done" looks like for each system. Implementation happens via GitHub Copilot / Gemini against these contracts.

## Build Targets

| Target | Role | Backend | Architecture |
|--------|------|---------|--------------|
| macOS | Primary dev | Mono or IL2CPP | Universal (Apple Silicon + Intel) |
| iOS | Demo target | IL2CPP (required) | ARM64 |

Both targets lock orientation to landscape. iOS uses Bluetooth gamepads (no touch controls in prototype).

## Tech Stack

- Unity 2022 LTS or Unity 6 LTS
- 2D Built-in Render Pipeline
- Unity Input System (new)
- Cinemachine (camera)
- NUnit via Unity Test Framework
- Git + Git LFS

## How to Run Tests

1. Open the project in Unity
2. Window → General → Test Runner
3. **Edit Mode tab** — click "Run All" (pure logic tests, no engine loop)
4. **Play Mode tab** — click "Run All" (tests requiring engine loop: camera, spawning)

All tests should **fail** initially — they define the contracts that implementations must satisfy.

## Build Order

Systems have dependencies. Implement in this order:

### Tier 1 — No dependencies, start immediately
- **Issue #1** — Input Abstraction (`InputBuffer`, `PlayerInputHandler`)
- **Issue #2** — Entity Base & Components (`Entity`, `HealthComponent`, etc.)

### Tier 2 — After Tier 1
- **Issue #3** — Movement Controller (`KinematicMovementController`)
- **Issue #4** — Combat System (`CombatSystem`, `AttackData`, `HitResult`)

### Tier 3 — After Tier 2
- **Issue #5** — Player Controller (`CorgiController`)
- **Issue #6** — Enemy AI (`EnemyAI`, `FeralCatAI`, `RaccoonBanditAI`, `SprinklerTurretAI`, `AggroSlotManager`)

### Tier 4 — After Tier 3
- **Issue #7** — Camera (`GroupTargetCamera`, `ArenaCameraLock`)
- **Issue #8** — Spawn & Wave Management (`SpawnManager`, `WaveData`)
- **Issue #9** — Environmental Weapons (`EnvironmentalWeaponEntity`, `PickupHandler`)

### Tier 5 — After Tier 4
- **Issue #10** — Boss Controller (`WhiskerbotController`)
- **Issue #11** — Co-op Run State (`RunState`, `ReviveSystem`)

### Tier 6 — After Tier 5
- **Issue #12** — UI (`HUDController`, `ComboCounterUI`, `BossBannerUI`)
- **Issue #13** — Platform Build Configuration

## Which Issues to Assign to Copilot First

**Start with Issues #1 and #2** — they're pure foundational code with no dependencies and straightforward contracts. Copilot should handle these without escalation.

**Good Copilot candidates:** Issues #1, #2, #3, #8, #9, #11, #13 — clear patterns, well-scoped, data-driven.

**May need Claude escalation:** Issues #4, #5, #6, #10 — combat feel, state machines, boss phases require more design reasoning.

**Likely needs Claude:** Issue #7 — camera framing in co-op is the #1 risk per the technical doc. Cinemachine integration is fiddly.

## Workflow Per Issue

1. Copilot takes first crack. Push to a branch, run tests.
2. If tests pass → review for style, merge.
3. If tests fail and Copilot can't fix in 2 iterations → escalate to Claude Sonnet.
4. If Sonnet struggles → Claude Opus.

Most issues should never need Claude. The test suite bounds implementation mistakes.

## Folder Structure

```
/
  README.md              ← you are here
  ARCHITECTURE.md        ← system diagram + subsystem descriptions
  issues/                ← GitHub issue markdown files (1 per system)
  Assets/
    _Project/
      Code/
        Core/            ← Entity, Input, Movement, RunState, SpawnManager
        Player/          ← CorgiController, PickupHandler
        Enemies/         ← EnemyAI, FeralCat, Raccoon, Turret, Boss
        Combat/          ← CombatSystem, HitResult, EnvironmentalWeapon
        Camera/          ← GroupTargetCamera, ArenaCameraLock
        UI/              ← HUDController, ComboCounterUI, BossBannerUI
        Data/            ← ScriptableObjects (CorgiData, AttackData, EnemyData, WaveData, PlatformSettings)
      Tests/
        EditMode/        ← Pure logic tests (NUnit)
        PlayMode/        ← Engine-loop tests (UnityTest)
      Scenes/
      Settings/
```

## Open Design Questions

These are documented as `// TODO:` comments in the relevant stubs and tests:

- **Environmental weapon damage:** Bigger than standard combos, or just different feel (wider arc, longer reach)?
- **Combo multiplier persistence:** Persist across enemies (true combo) or reset between groups?
- **Special meter model:** Cost-based consumption or cooldown timer?
- **S-rank threshold:** Easy to get on first clear, or true mastery only?

Decide during prototype playtests. Do not make unilateral decisions in code.

## Visual Style (Prototype)

No art. Solid-color Unity primitives only:
- Sarge: orange rectangle
- Feral Cat: grey rectangle
- Raccoon: dark grey + white stripe
- Sprinkler: blue circle
- Roomba: large black circle
- Whiskerbot: large red rectangle (cockpit = red square)
- Weapons: yellow circle (lid), green triangle (gnome), brown thin rect (rake)
- Hit VFX: white flash + expanding circle (white/yellow/magenta by hit type)
