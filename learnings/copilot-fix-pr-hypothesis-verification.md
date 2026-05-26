# Copilot Fix PRs — Diff Inspection Is Not Verification

**Date:** 2026-05-26
**Context:** During Milestone 1 ralph-loop iterations, two consecutive Copilot-authored "fix" PRs (#67, #72) were approved-and-merged based on diff inspection alone — the CI Unity-Tests workflow on each branch sat in `action_required` (workflow approval required from a maintainer), so no test signal was available pre-merge. Both PRs were technically clean: tight scope, matched documented patterns from existing `learnings/*.md` files, no production scope creep. Both also turned out to be wrong fixes that did not actually make the target tests pass once they landed on main.

## What happened

### PR #67 — HUDController `EnsureVisualHierarchy`

- **Hypothesis (in PR body):** Canvas.renderMode throws NRE in headless EditMode because `canvas.GetComponent<Canvas>()` returned null after Unity destroyed the Canvas component during its own Awake.
- **Fix:** Reorder so `_safeAreaRectTransform = canvasGO.GetComponent<RectTransform>()` happens *before* canvas-component access, and null-guard `canvas.renderMode`.
- **Why the diff inspection approved:** The reordering is defensive, matches the structurally-cleaner ordering, and follows the "RectTransform is in the constructor type-list so it's always present" guarantee.
- **Why it didn't fix the test:** The real failure mode is that `new GameObject("HUDCanvas", typeof(Canvas), ...)` itself causes Unity to destroy the GameObject when Canvas init fails, leaving `canvasGO` as a destroyed reference. The reordering doesn't help if the GameObject is destroyed — `canvasGO.GetComponent<RectTransform>()` returns null too. Issue #73 documents the next-attempt fix (construct GameObject without Canvas in the type-list, AddComponent each piece individually).

### PR #72 — LevelBackyard `ClearAllWaves` test helper

- **Hypothesis (in PR body):** The helper consumed its `while` loop in one frame; wave advancement needs frame progression to settle, so `IsBossDoorUnlocked` is never observed true before the guard exhausts.
- **Fix:** Convert `ClearAllWaves` to `IEnumerator`, add `yield return null` inside each loop iteration.
- **Why the diff inspection approved:** Matches the documented `learnings/unity-test-isolation.md:101-110` "deterministic wait" pattern. Test-only fix. No production code touched.
- **Why it didn't fix the test:** The death → wave-advance chain in this codebase is *synchronous*. `health.TakeDamage` → `OnDied` → `Entity.RaiseDeath` → `Entity.OnDeath` → `SpawnManager.HandleSpawnedEnemyDeath` → `TryRegisterEnemyDeath` → `HandleEnemyDiedCount` → `ClearCurrentWave` → `OnWaveCleared` → `LevelBackyardDirector.HandleWaveCleared` → `AdvanceToNextWave` + `SpawnCurrentWave` all execute on a single call stack with no `yield` between them. The yield doesn't help if the chain is already synchronous. The actual bug is somewhere upstream — likely in subscription bookkeeping or fixture setup order — and the issue (#71) had to be reopened with a more rigorous "instrument before fixing" plan.

## The pattern

Two well-formed "fix" PRs, two failed fixes. Common shape:

1. The PR description contains a plausible "root cause" paragraph.
2. The diff is minimal and matches documented patterns.
3. The PR author has not actually verified the fix works against the failing test, because Unity tests don't run in their sandbox / their CI is gated.
4. The reviewer (Claude) reads the diff, recognizes the documented pattern, approves.
5. Merge → main CI runs → fix doesn't work.

**The diff cannot tell you whether the hypothesis is correct.** A defensive reorder, a yield insertion, a null-guard — these are all *patches against guesses about what's wrong*. Without test execution, the reviewer is essentially co-signing the agent's hypothesis.

## What to do differently

### Treat fix PRs from Copilot as guesses until proven

When a fix PR claims to fix a specific failing test:

- **Require CI green on the PR branch.** Not the diff. The test must actually pass under the new code. If CI on the Copilot branch is gated (e.g., `action_required`), surface that as a blocker — don't merge until the workflow runs and is green.
- **If CI cannot run on the branch**, do not approve the merge. Ask the user to manually approve the workflow run, or push the same fix on a maintainer branch where CI auto-runs.
- **Approving based on "diff looks reasonable" is not a verification.** It's a recommendation that the diff isn't *introducing new bugs*. It says nothing about whether the diff fixes the *target* bug.

### Demand evidence before hypothesis-driven fixes

Issue specs should require the implementer to:

1. **First** add diagnostic output (Debug.Log) that pinpoints which step of the failing chain is breaking.
2. **Run the failing test** with the diagnostic to capture the actual failure mode.
3. **Then** propose a fix targeting the observed failure, not the hypothesized one.

The instrument-first pattern catches "the hypothesis is wrong" before the fix lands. The bare hypothesis-then-fix pattern has a high false-positive rate when the author can't reproduce the failure.

### Reviewer red flags

A fix PR is high-risk when:

- The PR body's "root cause" section uses tentative language ("likely…", "the most plausible cause is…").
- The fix is defensive (null-guards, reorderings, yields) without a test that proves the unguarded path was being hit.
- The diff is plausible from documented patterns alone — patterns are necessary but not sufficient.
- The PR has no CI signal on its branch.

When two or three of those land in the same PR, the right review is "needs CI signal before approval," not "approve based on diff."

## Anti-pattern, captured

**"Diff matches the documented pattern, therefore the fix is correct"** is a category error. The documented pattern tells you *what kind* of fix is appropriate for *that kind* of bug. It does not tell you that *this PR's bug* is *that kind* of bug. Verification of the hypothesis is what closes that gap. Without verification, the diff inspection is just sign-off on plausibility.
