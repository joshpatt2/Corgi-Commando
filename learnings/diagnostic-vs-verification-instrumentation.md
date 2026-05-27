# Diagnostic vs Verification Instrumentation — Two Purposes, Different Coverage

**Date:** 2026-05-26
**Context:** When scoping the `PlaytestMetrics` issue (#78), the initial design covered 5 metric categories (hitstop, knockback, screen shake, state transitions, frame time) that mapped directly to M1 acceptance checkboxes. The user then asked: *"Is this enough logging to debug and root-cause any issue we find?"* The honest answer was no — it covered verification but not diagnostics. This distinction is worth its own learning because the two purposes have systematically different coverage needs.

## The distinction

**Verification instrumentation** answers *did the system do what the spec says it should do?* It measures the things the acceptance criteria explicitly call out. Examples:
- Hitstop duration (M1 says 3-6 frames → log durations → assert in range)
- Boss phase transitions at 75% and 35% HP (M1 says they fire → log transitions → assert presence)
- Wave-to-wave timing (M1 says next doesn't start until previous clears → log start/end → assert ordering)

The scope of verification instrumentation is **closed**: it covers exactly what the spec mentions. Add a new acceptance checkbox, add a new metric.

**Diagnostic instrumentation** answers *when something went wrong, what was the upstream cause?* It captures the data needed to root-cause failures, including failures the spec didn't anticipate. Examples:
- Unhandled exceptions — most non-trivial bugs throw something
- Damage events — the upstream cause when an enemy doesn't die when expected
- Input record/consume lifecycle — the diagnostic when an input doesn't produce visible effect
- Component initialization — when a system silently fails to start
- Asset resolution — when a prefab fails to load, a font reference is null, etc.

The scope of diagnostic instrumentation is **open-ended**: it tries to anticipate the categories of *future* failure, not just the ones the spec mentions.

## Why they're different

A bug report says "the boss didn't die when its HP hit zero." Verification instrumentation tells you: yes, phase 3 fired, yes, the player landed N hits totalling K damage, yes, the boss's HP did reach zero. All the verification metrics pass. The bug is still there.

Diagnostic instrumentation tells you: oh, an `NullReferenceException` fired in `BossController.OnDeath` because `_pilotPrefab` was null on a specific code path. Or: the damage event log shows the last hit was `damageType: special` and the special damage path bypasses the death-event subscription. Or: the asset resolution log shows `_pilotPrefab` failed to load with reason "asset moved."

Verification metrics tell you *the contract held*. Diagnostic metrics tell you *what actually happened during the run*. Both are needed.

## The mistake to avoid

Scoping instrumentation purely from acceptance criteria leads to verification-only coverage. The acceptance file says nothing about exceptions, so exceptions aren't logged. Then a real bug throws an NRE inside `Update`, the test asserts wrong end-state and fails, and the report has nothing useful — verification metrics all pass, no exception was recorded, no clue what went wrong.

In this project: the original #78 (PlaytestMetrics) scope was verification-only. The user caught it by asking the right question. Issues #85-#90 were filed to fill the diagnostic gaps:
- #85: Exception capture (highest leverage — most bugs throw)
- #86: Damage events (HP-related bugs)
- #87: Input lifecycle (input-not-firing bugs)
- #88: Position snapshots (stuck-in-geometry / teleport bugs)
- #89: Component initialization (silent-init-failure bugs)
- #90: Asset resolution (null-ref / missing-prefab bugs)

Each one was motivated not by an acceptance checkbox but by a *category of failure* the team has hit (or could realistically hit) on similar projects.

## How to scope diagnostic instrumentation

Three heuristics:

1. **Cover the layers below acceptance.** Acceptance asks "does damage apply" — diagnostic asks "if damage doesn't apply, was it a missed call, a null target, a guard reject, or an exception?" Trace one level deeper than the acceptance contract.

2. **Cover the silent failure modes.** Any path that can fail without throwing or asserting needs an explicit log. Null-prefab spawns. Components that don't `Initialize`. Inputs that record but aren't consumed. Asset lookups that return null and get gracefully skipped.

3. **Cover the cross-boundary signals.** Inputs cross the user→engine boundary. Damage crosses the attack→target boundary. Asset loads cross the data→runtime boundary. Each boundary is a likely failure surface and deserves a log at both sides (record and consume).

## When to file diagnostic vs verification issues

A practical workflow rule:
- **Verification issues** are filed when an acceptance checkbox needs measurement
- **Diagnostic issues** are filed in *batches*, ahead of need, when setting up a new project's instrumentation layer
- Both classes can be assigned to Copilot in parallel if they touch separate files

Don't wait until you hit a bug to add diagnostic instrumentation. The marginal cost of pre-instrumenting under the agent-time-cheap premise (see [[agent-time-vs-human-time-economics]]) is near-zero. The marginal cost of *not* pre-instrumenting is one wasted debugging session per missing category.

## Concrete pattern: the `PlaytestMetrics.Log<Category>` family

What worked structurally:
- All categories are static methods on a single `PlaytestMetrics` class
- Each gates on `IsRecording` and is allocation-free when off
- Each appends to a category-specific list
- `WriteReport(path)` serializes all categories to one JSON
- Categories are added by appending new methods (additive — safe for parallel PRs, see [[pr-merge-sequencing]])

The benefit: a single file to subscribe to, a single report to read, but six (or N) independent diagnostic dimensions. Each can be developed in parallel because the contract is "add a new method + a new JSON field." No category needs to know about the others.

## Carmack-style summary

If your instrumentation only measures what the spec says, you have measurement, not debugging. Pre-instrument the failure-mode categories every project hits: exceptions, damage/state changes, input lifecycle, component init, asset resolution. The marginal cost is small; the marginal benefit is the ability to root-cause unanticipated bugs without a re-run.

## Related learnings

- [[copilot-coding-agent-effectiveness]] — concrete feedback patterns
- [[agent-time-vs-human-time-economics]] — why we pre-instrument
- [[pr-merge-sequencing]] — additive parallelism for multi-PR instrumentation work
