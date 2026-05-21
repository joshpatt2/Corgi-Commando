# Claude Code Prompt — Corgi Commando Prototype (Skeleton + TDD)

This prompt asks Claude Code to produce the **architecture skeleton and test suite only**, not full implementations. Copilot (or Gemini) implements against the contracts and tests that Claude Code defines. This keeps Claude Code's expensive cycles focused on design, not grinding through boilerplate.

Attach the three design docs (`Corgi_Commando_Concept.docx`, `Corgi_Commando_Technical.docx`, `Corgi_Commando_Gameplay.docx`) to the Claude Code project before running this prompt.

---

## PROMPT START

You are setting up a Unity prototype for **Corgi Commando**, a 2.5D side-scrolling beat-em-up. Three design documents are attached — read all of them before doing anything. They are the source of truth for design intent.

**Your job is NOT to implement the game.** Your job is to produce the skeleton: class APIs, interfaces, ScriptableObject data shapes, and a complete test suite that defines what "done" looks like for each system. Other tools (GitHub Copilot, Gemini) will fill in the implementations to make the tests pass.

This division of labor exists because architectural reasoning is your highest-value contribution and is expensive. Implementation-grinding is cheap elsewhere. Stay in your lane.

### What You Will Produce

1. **C# class & interface stubs** with full signatures, XML doc comments, and `throw new NotImplementedException()` bodies (or empty bodies for void returns).
2. **ScriptableObject definitions** (data shapes only) for `CorgiData`, `AttackData`, `EnemyData`, `WaveData`.
3. **A complete NUnit test suite** under `Assets/_Project/Tests/` that defines behavior for every public method on every interface. Tests must compile and **fail** initially because implementations are missing.
4. **One GitHub issue per system** (markdown files in `/issues/` at repo root) with: title, description, acceptance criteria, list of tests that must pass, dependencies on other issues.
5. **A `README.md`** at the repo root with: project overview, build order, how to run tests, and which issues to assign to Copilot first.
6. **An `ARCHITECTURE.md`** with a system-relationship diagram (ASCII or Mermaid) and a short explanation of each subsystem.

**Do not write implementations.** If you find yourself writing logic inside a method body, stop. Write a test for the behavior instead.

### Goals for the Prototype

1. **Prove combat feel.** Hitstop, knockback, screen shake, combo chains, special meter should feel satisfying.
2. **Prove the co-op architecture.** Local 2-player shared-screen co-op with drop-in/drop-out must work end-to-end.
3. **Run on both macOS and iOS.** macOS is the dev target; iOS is the "show people on my phone" demo target. Both must build and run from day one of CI.

This is a vertical slice. Polish, content variety, persistence are out of scope.

### Visual Style — Placeholder Art Only

No sprite art. No animations. Use solid-color Unity primitives for everything:

- Sarge (player corgi): orange rectangle, ~32×48 units
- Feral Cat: grey rectangle
- Raccoon Bandit: dark grey rectangle with a white stripe
- Sprinkler Turret: blue circle
- Roomba mini-boss: large black circle
- Whiskerbot-9000: large red rectangle (smaller red square = cockpit weak spot)
- Environmental weapons: distinct shape per type (lid = yellow circle, gnome = green triangle, rake = brown thin rect)
- Level geometry: muted brown / beige rectangles
- Hit VFX: white flash + expanding circle, color-coded (white = light, yellow = heavy, magenta = special)
- Background: flat green plane

Resist the urge to "just add a quick sprite." Solid colors only until the prototype validates feel.

### Tech Stack (Locked)

- Unity 2022 LTS or Unity 6 LTS, 2D Built-in Render Pipeline
- C#, Unity Input System (new)
- Cinemachine for camera with Target Group
- NUnit via Unity Test Framework (already included)
- Git + Git LFS initialized from start
- No third-party assets beyond Unity packages
- No persistence/save system

### Build Targets

- **macOS** — primary dev target. Native dev loop, gamepad + keyboard input. Co-op via two paired controllers.
- **iOS (iPhone)** — secondary demo target. Requires Unity iOS Build Support, Xcode on Mac, Apple Developer account. IL2CPP backend, ARM64, landscape orientation locked, iOS 13+ minimum.
- **No Windows / Linux / Android** for prototype.

### iOS-Specific Constraints

- Input on iOS is **MFi / Bluetooth gamepad first**. Touch controls are deferred and explicitly out of scope for the prototype.
- **iOS co-op** requires two Bluetooth controllers paired to the device. Gate the P2 join flow on detecting a second controller; otherwise run solo.
- Lock orientation to **landscape only** on both platforms.
- Iteration cycle on iOS is slower (Unity export → Xcode build → device deploy). Plan to develop on macOS, batch-validate on iOS.
- First iOS build to device is a Week 3 milestone — early enough to surface signing / controller issues, not so early it blocks combat-feel work.

### Systems to Skeleton (Build Order)

Produce stubs, tests, and GitHub issues for each in this order. Each system has dependencies on prior ones — note them in the issue.

#### 1. Input Abstraction (`InputBuffer`, `PlayerInputHandler`)
- Wraps Unity Input System
- Buffers inputs with timestamps (combat reads from buffer, not raw)
- Supports gamepad + keyboard, multi-player join-on-press
- **Platform parity:** identical input contract on macOS and iOS. Gamepad is the canonical input device on both. Keyboard is macOS-only.
- Tests: input registered with correct timestamp; buffer purges stale inputs; simultaneous inputs resolved; controller disconnect handled; second controller pair detected on iOS gates P2 join flow

#### 2. Entity Base & Components (`Entity`, `IComponent`, `HealthComponent`, `HurtboxComponent`, `KnockbackReceiver`)
- Component-based composition (not inheritance-heavy)
- Faction enum (Player, Enemy, Neutral)
- Tests: entity creation; component add/remove/get; health damage and death event; hurtbox enable/disable; knockback applied as velocity impulse

#### 3. Movement Controller (`KinematicMovementController`)
- 2.5D: X horizontal, Z depth, Y jump only
- Custom kinematic (no Rigidbody2D dynamics)
- Tests: input vector → velocity; gravity applied when airborne; grounded check; Z-depth movement; cannot move through static colliders

#### 4. Combat System (`CombatSystem` singleton, `AttackData`, `HitResult`)
- Centralized hit resolution
- Hitstop, knockback, VFX spawning
- Same-Z-band hit rule (±0.5 units)
- Tests: attack hits target in band; attack whiffs target outside band; damage applied correctly; hitstop duration honored; combo counter increments on chained hits and resets after timeout; special meter fills on hit landed

#### 5. Player Controller (`CorgiController : Entity`)
- State machine: Idle, Walk, Attack1/2/3, Hit, Knockdown, GetUp, Special, PickupHold
- Reads from `InputBuffer`
- Combo chain: Punch, Punch, Kick → launcher
- Tests: state transitions follow rules; attack reads frame data from `AttackData`; combo window honored; cannot attack during recovery; special consumes full meter

#### 6. Enemy AI (`EnemyAI` base FSM, concrete `FeralCatAI`, `RaccoonBanditAI`, `SprinklerTurretAI`)
- Lightweight FSM: Idle → Chase → Attack → Stunned → Recover
- `AggroSlotManager` (max 2 attackers per player, others circle)
- Tests: state transitions; aggro slot reserved on chase; slot released on stun/death; raccoon flees with stolen Treats; turret fires on telegraphed interval

#### 7. Camera (`GroupTargetCamera`, `ArenaCameraLock`)
- Cinemachine Target Group with both players
- Soft horizontal distance cap (~10 units)
- Arena locks during waves and boss
- Tests: camera follows single player; framing covers both players; distance cap pulls trailing player; arena lock prevents pan past trigger; unlock fires on wave clear

#### 8. Spawn & Wave Management (`SpawnManager`, `WaveData`)
- Drives wave-based encounters from `WaveData` SOs
- Knows arena cleared state
- Tests: wave initializes enemies at spawn points; wave clear event fires when all dead; next wave triggers after delay; arena gate state correct

#### 9. Environmental Weapons (`EnvironmentalWeaponEntity`, `PickupHandler`)
- Pickup on Special near flagged object
- Light = swing, Heavy = throw (consumes)
- Breaks after N uses
- Tests: pickup near flagged object succeeds; swing applies attack data; throw consumes weapon; weapon breaks after use limit; movement slowed while held

#### 10. Boss Controller (`WhiskerbotController : BossEntity`)
- Three-phase fight with HP threshold transitions
- Phase 1: stomp + claws; Phase 2: debris + missiles + laser; Phase 3: pilot fight
- Tests: phase transition at 75% and 35% HP; laser pointer chase-tax mechanic correct; pilot ejects at 0% mech HP; pilot fight is separate entity with own HP

#### 11. Co-op Run State (`RunState` ScriptableObject + service)
- Shared lives pool, shared Treats counter
- Revive on partner proximity
- Tests: Treats added correctly; revive timer counts down on proximity; both players dead → game over event; drop-in restores player without resetting Treats

#### 12. UI (`HUDController`, `ComboCounterUI`, `BossBannerUI`)
- Health bars, special meter, combo counter, boss banner, pause menu
- Either player can pause
- **Anchored to safe areas** so iOS notch/home-indicator don't clip UI
- Tests: health bar reflects health component; combo counter shows current chain and fades on break; boss banner shows on boss spawn; pause halts time and shows menu; input switches to UI map on pause; UI respects iOS safe area insets

#### 13. Platform Build Configuration
- Build presets / scripts for macOS and iOS
- iOS: IL2CPP, ARM64, landscape-locked, iOS 13+ minimum, dev signing config
- macOS: Mono or IL2CPP, Apple Silicon + Intel universal
- **Single source of truth** — `PlatformSettings` ScriptableObject or build script that both targets read from
- Tests: build script for each target produces a valid output without manual Unity tweaks; safe area handling validated; orientation locked correctly

### GitHub Issue Format

Each issue lives at `/issues/NN-system-name.md` and follows this shape:

```
# [System Name]

## Goal
One-sentence purpose.

## Acceptance Criteria
- [ ] All tests in Tests/[SystemName]Tests.cs pass
- [ ] Public API matches the stub in Code/[Path]/[Class].cs
- [ ] No new dependencies introduced outside listed prerequisites

## Tests to Pass
- Test_Behavior1
- Test_Behavior2

## Dependencies
- Issue #N (system this builds on)

## Notes for Implementer
- Cost-sensitive: prefer Copilot first, escalate to Claude only if stuck
- Implementation hints, edge cases, gotchas
```

### Test Quality Bar

- Tests must be runnable in Unity Test Runner (Edit Mode for pure logic, Play Mode for behaviors needing the engine loop)
- Each test names a single behavior (`Punch_ConnectsWithEnemyInZBand_AppliesDamage`)
- Use Arrange / Act / Assert structure with comments
- No flaky tests — time-based behaviors use fake clocks or coroutine yields, not real wall time
- A test that requires the implementation to be smart in non-obvious ways must have a comment explaining the design intent

### Folder Layout

```
/  (repo root)
  README.md
  ARCHITECTURE.md
  issues/
    01-input-abstraction.md
    02-entity-components.md
    ...
  Assets/
    _Project/
      Code/
        Core/
        Player/
        Enemies/
        Combat/
        Camera/
        UI/
        Data/
      Tests/
        EditMode/
        PlayMode/
      Scenes/
      Settings/
```

### What to Do First

1. Confirm you've read all three design docs.
2. Propose your skeleton plan as a short outline (system list, dependency order, test count estimate) and STOP. Wait for approval before generating code.
3. After approval, produce the skeleton system by system in the order above. After each system, post the GitHub issue and stop for a quick sanity check before continuing.

### Hard Rules

- No implementations. Method bodies are empty or `throw new NotImplementedException()`. Trivial getters/setters and constructors are the only exception.
- No real art. Solid-color primitives only.
- No UI polish work. Visual polish is not your job.
- Document open questions (env weapon damage scaling, combo persistence, special cost model) as `// TODO:` comments at the relevant test or stub, not unilateral decisions.
- Scope-locked to Level 1. No multi-level architecture, no HQ scene, no save system.
- **Build targets locked to macOS + iOS.** Do not add Windows, Linux, Android, WebGL, or console targets. Future-proofing for them is explicitly out of scope.
- **No touch input implementation.** iOS demo uses Bluetooth controllers. Touch is a deferred design problem.

## PROMPT END

---

## How to Use This Prompt

1. Set up Claude Code with the Unity project root and attach the three `.docx` design docs.
2. Paste the prompt above as the initial message.
3. Approve the plan Claude Code proposes before it starts generating files.
4. Review each system as Claude Code completes it — issue file, stubs, and tests should all align.
5. Create the GitHub repo and post issues from `/issues/` as real GitHub issues.
6. Assign Copilot (or Gemini) to the issues, in dependency order.
7. Keep Claude on call for: integration debugging, architecture changes, anything where Copilot is spinning.

## Model Cost Strategy

- **Claude Opus (via Claude Code):** architecture, test specs, integration debugging, anything requiring deep reasoning. Most expensive — use sparingly.
- **Claude Sonnet:** the default workhorse for Claude Code if Opus is overkill. Strong at code, much cheaper. Good fit for skeleton generation.
- **Claude Haiku:** quick refactors, doc updates, simple bug fixes — cheapest Claude tier, fine for narrow tasks.
- **GitHub Copilot:** bulk implementation against the test contracts. Bundled in your subscription, so effectively free per-completion. Will struggle on complex system design but excels at filling in patterns.
- **Gemini:** good alternative or second opinion for implementation. Useful when Copilot gets stuck or you want to A/B a tricky function.

The skeleton + TDD setup bounds Copilot's mistakes by the test suite. If tests pass, the contract is met — even if the implementation is uglier than what Claude would write. That's the whole point of this workflow: you only pay for premium reasoning where it matters (design + spec), and let cheap tools fill the rest.

### Recommended Workflow Per Issue

1. Copilot takes first crack. Push to a branch, run tests.
2. If tests pass → review for style, merge.
3. If tests fail and Copilot can't fix in a couple iterations → escalate to Claude Sonnet via Claude Code with the failing test output.
4. If even Sonnet struggles (rare for well-scoped issues) → Opus.

Most issues should never need Claude. That's the design.
