# PR Merge Sequencing — Conflict Hotspots & Scope Discipline

**Date:** 2026-05-25
**Context:** Eight Copilot-authored PRs landed in parallel during the Milestone 1 sprint. Three of them (`#53`, `#56`, `#57`, `#58`) all modified `SceneBootstrap.cs`. Merge order determined whether the queue cleared smoothly or stalled with rebase loops. Two other PRs (`#62`, `#57`) demonstrated scope-creep anti-patterns. This doc captures what worked and what didn't.

## Identify the Conflict Hotspot Early

Before opening parallel issues, grep the issue specs for shared file paths. In this sprint, `SceneBootstrap.cs` was the orchestrator and four PRs touched it:

| PR | Change to SceneBootstrap |
|---|---|
| #53 | Added `AutoStartEncounter` toggle, `LevelBackyardDirector` integration |
| #56 | Added P2 drop-in, multi-player Tick loop |
| #57 | Boss checkpoint logic, scene reset hooks |
| #58 | Added `_screenShakeHandler` reference + Cinemachine wiring |

Once you see four PRs queued against the same file, you have three options:
1. **Serialize them.** Merge one, wait for the others to rebase, repeat. Slow but safe.
2. **Bundle them into one PR.** Faster but loses individual review granularity.
3. **Refactor first to split the hotspot.** Land a no-op extract PR that splits `SceneBootstrap.cs` into focused partial classes, then merge in parallel. Highest upfront cost; pays off if many more PRs are coming.

We chose option 1. In retrospect, option 3 would have been correct earlier in the project — the orchestrator role makes SceneBootstrap a natural conflict point that will keep biting.

## Merge Order: Path of Least Resistance

The order that worked: `#61 → #54 → #55 → #62 → #58 → #53 → #68 → #69 → #70`.

Heuristics that produced this order:
- **Fixes before features.** PR #61 (Arial.ttf → LegacyRuntime.ttf) and #54 (death-tracking strictness) landed first; they were narrow and unblocked others' assertions.
- **Smallest diff first when contention is equal.** A 15-line PR merges in seconds; queue it before a 500-line one to free up reviewer attention and reduce stale-rebase cycles.
- **Test infrastructure ahead of features.** PR #70 (test fixture cleanup) intentionally landed last in this batch because it patched up the regressions exposed by #54 + #69 — but the principle is to land infra **between** waves of feature PRs, not after them. Doing test infra last left a window where main was red.
- **Wait for GitHub's mergeability recomputation.** After every merge, GitHub re-evaluates downstream PRs as `UNKNOWN` until it recomputes. Polling pattern:
  ```bash
  until gh pr list --json number,mergeable | grep -qv UNKNOWN; do sleep 2; done
  ```
  Without this, downstream rebases happen against stale state and produce avoidable conflicts.

## Anti-Pattern 1: Fix PR with Hidden Production Scope

**PR #62 was supposed to fix a flaky PlayMode timing assertion.** The diff also included a multi-player `Tick(deltaTime)` refactor that belonged in PR #56. The agent's reasoning was reasonable ("while I'm in here..."), but:

- The test-only change reviews in 30 seconds.
- The production refactor needs domain context and review against the multi-player issue spec.
- Bundling them blocked the test fix on the production review.

**Posted CHANGES_REQUESTED.** Copilot stripped the production change. The slimmed PR merged in the next cycle; the production refactor went where it belonged in #56.

**Rule of thumb for the reviewer:** if the PR title says "fix X" but the diff also does "and refactor Y," push back. Each PR should answer one question.

## Anti-Pattern 2: Regression Reintroduced by a Later PR

**PR #61 fixed every `Resources.GetBuiltinResource<Font>("Arial.ttf")` reference** — that font was removed in newer Unity versions and was crashing 9 tests. The fix was `LegacyRuntime.ttf`.

**PR #57 was opened in parallel.** It added new HUD-touching code. Copilot wrote `Resources.GetBuiltinResource<Font>("Arial.ttf")` — the same dead reference, in a new file, by another agent that didn't see #61's fix.

This is structurally invisible to the agent: each PR branches from main *as of when its issue was opened*, so #57 inherited the bug that #61 was simultaneously fixing.

**Mitigations:**
- When a fix lands for a project-wide hazard (deprecated API, removed font, etc.), grep open PRs for the same antipattern and post a heads-up comment. Don't wait for the parallel PRs to land and then re-fix.
- For systemic hazards, add a CI lint that fails the build if the deprecated reference appears. The font check is a one-line regex; cheaper than catching it in review N times.

## Anti-Pattern 3: Stale Mergeability State

After merging PR #54 (a high-conflict PR that touched `SpawnManager`), GitHub showed downstream PRs as `MERGEABLE` for ~10 seconds before recomputing them to `CONFLICTING`. Acting on the stale state attempted a merge that immediately failed.

**Fix:** always poll mergeability after a non-trivial merge:
```bash
until gh pr view N --json mergeable -q '.mergeable' | grep -qv UNKNOWN; do sleep 2; done
```
Or check the queue:
```bash
until [ -z "$(gh pr list --json number,mergeable -q '.[] | select(.mergeable == "UNKNOWN") | .number')" ]; do sleep 2; done
```

## The Strict Review Posture (Validated)

Mid-sprint we switched from "approve-with-notes" to "any unresolved issue blocks until clean." Empirically this was right:
- Copilot re-iterates in ~90 seconds when feedback is concrete.
- The cost of a re-review cycle is small; the cost of accumulated debt is large.
- Approve-with-notes promises follow-up that nobody schedules; strict blocking forces the issue in front of the right agent now.

The exception: stylistic preferences ("I'd name this differently") — those legitimately can be follow-up. The strict bar is for behavioral correctness, security, and adherence to acceptance criteria.

## Quick Checklist Before Merging an Agent PR

1. Diff touches only what the issue scope describes? (If not → CHANGES_REQUESTED, split the PR.)
2. Tests pass on the rebased branch? (Not on the open PR — GitHub's status can be stale post-rebase.)
3. Mergeable state is `MERGEABLE`, not `UNKNOWN`? (If UNKNOWN, wait.)
4. No regression of an earlier-merged fix in the same area? (Grep for the fixed pattern.)
5. Branch deletion enabled on squash merge? (`gh pr merge --squash --delete-branch`)
