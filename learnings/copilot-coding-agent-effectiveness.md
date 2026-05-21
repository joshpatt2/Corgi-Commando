# Working with GitHub Copilot Coding Agent — Effectiveness Notes

**Date:** 2026-05-21
**Context:** Corgi Commando vertical-slice sprint. In one day, Copilot Coding Agent (the `copilot-swe-agent` premium agent) shipped 8 PRs covering player controller, enemy AI, environmental weapons, boss controller, co-op run state, and content authoring. Claude reviewed each PR under increasingly strict criteria. This doc captures what went well, what didn't, and how to communicate with the agent more effectively next time.

## What Went Well

### Speed of iteration is genuinely impressive
- Issue assigned → draft PR opened in 2–5 minutes.
- Code review posted → first response commit in 60–90 seconds.
- A full review-respond-rereview cycle typically completes in under 5 minutes when feedback is concrete.
- Eight feature PRs and one infra PR shipped to main in a single working session.

### Commit messages map directly to feedback
Copilot's response commits name the issues they address, sometimes verbatim:
- *"Address review feedback: null guard, Throw guard, instance buffer..."* — directly hits items 1–3 of the review.
- *"Fix revive index and input validation in run state systems"* — maps to two specific review items.
- *"test: use robust bounds for sarge authored numeric assertions"* — picked up the exact predicate-shape feedback.

This makes it trivial to verify intent before re-reading the diff.

### Stays in scope
- Touches only the files the issue calls for. No drive-by refactors or unrelated cleanup.
- Doesn't expand the issue while implementing — if the spec says "FSM + slot manager," that's what lands.

### Implements YAML asset authoring correctly
PR #29 created 11 `.asset` ScriptableObject YAML files plus folder `.meta` files, with all script GUIDs correctly matched against the actual `.cs.meta` files (`AttackData`, `CorgiData`, `EnemyData`, `WaveData`). Cross-asset references (Sarge's `comboChain` pointing to `Sarge_Punch1.asset` etc.) wired via fileID+guid+type tuples in the correct YAML format. No Unity Editor session required.

### Architecturally compliant
- Uses `Initialize(...)` patterns for dependency injection rather than `Awake()` magic.
- Fires events with `(old, new)` tuples consistently.
- Adds new `EnemyData.pilotMaxHP` field rather than introducing a separate `BossData` SO before justified — minimal data-model expansion.

## What Didn't Go Well

### Literal interpretation of vague feedback
On PR #29, my review said:
> "Switch to robust predicates (e.g., `Is.GreaterThan(0)`, `Is.GreaterThanOrEqualTo(MinExpectedHP)`)."

Copilot read this as "remove `Is.Not.EqualTo(default)` and add exact-match assertions" — went from `Is.Not.EqualTo(100)` to `Is.EqualTo(120)`. Same brittleness, opposite direction.

**Fix that worked:** the re-review showed exact code blocks of what the assertions should look like. Round 2 was perfect. Lesson: if the requested change is non-obvious, **show the literal code** in the review body.

### Minimum-viable-to-pass behavior
Implementations satisfy the issue's stated "Tests to Pass" list and stop. Examples:
- **PR #27 Boss:** `Initialize(EnemyData data)` ignored the `data` parameter entirely on round 1. The issue's tests only check `CurrentPhase == 1` after Initialize — so unused `data` "passes."
- **PR #27 Boss:** Pilot HP hardcoded to `100` because no test specified otherwise.
- **PR #27 Boss:** Pilot spawned at world origin because no test checked position.
- **PR #28 Co-op:** `OnReviveComplete(0)` hardcoded because the existing test asserted `0`.
- **PR #28 Co-op:** `OnPlayerDropIn(5)` silently incremented `ActivePlayerCount` because no test exercised invalid indices.

**Fix that worked:** explicit "non-blocking but real" feedback in CHANGES_REQUESTED with concrete examples ("calling `OnPlayerDropIn(5)` while ActivePlayerCount=1 silently increments to 2"). Once told, Copilot fixes cleanly.

### Doesn't surface architectural gaps
Eight PRs landed without anyone — author, Copilot, or reviewer — noticing that **no scene-level orchestrator calls any `Tick(float deltaTime)` method**. Every controller exposes `Tick(float)` publicly, but nothing drives them per frame. The macOS build accepts gamepad input via `PlayerInputHandler` → `InputBuffer` faithfully, but `CorgiController._inputBuffer` is never assigned because `Initialize(...)` is never called. We didn't catch this until we built the app and pressed keys.

Copilot's per-issue scope discipline (a feature) becomes a liability when no single issue owns "make the systems actually run together." Issue #31 (Scene Bootstrap & Tick Driver) was opened to fill the gap, but only because we tested the build manually.

### Doesn't test the running game
Copilot validates by compiling and running unit tests. Nothing in the workflow actually opens the built app and tries to play it. Multiple PRs landed that compile and pass their tests but produce a non-interactive game.

### Repeat patterns across PRs
Several PRs initially shipped with the same kind of gap:
- Unused parameters on `Initialize(...)` methods (#27)
- Hardcoded values that should come from data (#27 pilot HP, #28 OnReviveComplete index)
- Missing input validation on public methods (#28 OnPlayerDropIn, #28 AddTreats)

This suggests a few prompt-able general rules would prevent multiple iteration cycles.

## Communication Patterns That Work

### Concrete > vague
- ✅ "Pilot HP hardcoded to 100. Pull from a new `EnemyData.pilotMaxHP` field."
- ❌ "Pilot HP should be configurable."

### Show the code in reviews
When the requested change is structurally specific, paste the literal target code:
```csharp
// Replace
Assert.That(sarge.maxHP, Is.EqualTo(120));
// With
Assert.That(sarge.maxHP, Is.GreaterThan(0));
```
Round-trip success rate goes from ~50% to ~100%.

### Tie feedback to the issue's acceptance criteria
*"Issue #6 says 'Slot released on stun/death' — that means released from the manager, not just the flag."* The agent re-reads the issue body and writes a fix that matches the contract intent.

### Strict CHANGES_REQUESTED beats approve-with-notes
The mid-session policy shift from "approve with follow-up notes" to "any unresolved issue blocks until clean" was the right call. Copilot iterates in ~90 seconds; the cost of a re-review round is small, and the cost of accumulated debt is large. Approve only when there's nothing meaningful left to fix.

### Re-reviews settle in one round when you're specific
Of the four CHANGES_REQUESTED I posted under strict mode, three (#26, #27, #28) were fully clean on round 2. The fourth (#29) needed a third pass because round 1's feedback was insufficiently literal ("use robust predicates" → showed code in round 2).

## What I'd Change Going Forward

### Issue specs need a "Quality bar" section
Beyond "Tests to Pass," add expectations like:
- All public method parameters must be used or removed
- All hardcoded numeric values must come from data unless documented as constants
- Public methods that accept indices must validate range
- Initialize methods must null-guard their reference-type parameters

If these become part of every issue body, Copilot's minimum-viable implementations rise to the new floor without per-PR feedback.

### Issue specs need a "Out of scope" section
The current `issues/*.md` files have "Dependencies" but rarely "Out of scope." Naming what each issue *doesn't* cover prevents both Copilot scope creep and reviewer confusion. (Issue #24 added this section and it worked well.)

### Pre-merge integration check
Add a step between "PR approved" and "merge": build the app and try the feature end-to-end if possible. This would have caught the missing Tick driver before merging eight PRs.

### Capture recurring rules in `learnings/`
The patterns above (unused params, hardcoded values, missing validation) are predictable. Worth a `learnings/copilot-quality-bar.md` doc that lists them. Future issues can `[See learnings/copilot-quality-bar.md]` rather than restate.

## Cost-Benefit Summary

| Aspect | Score | Comment |
|---|---|---|
| Speed | A+ | 8 PRs in a session is genuinely faster than human-only would manage |
| Code quality (round 1) | B | Functional, passes tests, but minimum-viable |
| Code quality (round 2 after CR) | A | Concrete feedback gets concrete fixes |
| Scope discipline | A | Stays inside the issue boundary, sometimes too rigidly |
| Architectural thinking | C | Doesn't catch cross-issue integration gaps |
| Test authoring | B+ | Adds tests when prompted; sometimes proactively |
| Asset / YAML authoring | A | Surprisingly capable at non-code artifacts |
| Communication responsiveness | A+ | Sub-minute response to reviews |

Net: **a strong force multiplier** for well-scoped issues with clear acceptance criteria, used in tight reviewer loops. Less suited for ambiguous architectural work or anything requiring cross-issue judgment. The human reviewer's job shifts from "writing code" to "writing precise feedback and catching what the agent can't see."
