# Corgi Commando — Ralph Loop Prompt (Milestone 1, v2)

You are continuing work on Corgi Commando (Unity 2022.3.62f1, 2.5D brawler). The goal is to finish Milestone 1 by running a tight Copilot-as-author / Claude-as-reviewer cycle.

## Read first (mandatory before any tool calls)

Read every file in the learnings/ folder in this order:

1. learnings/ralph-loop-vs-iterative-merge.md — when each mode fits, cancel signal
2. learnings/copilot-coding-agent-effectiveness.md — feedback patterns, quality bar
3. learnings/pr-merge-sequencing.md — conflict hotspots, merge order, scope discipline
4. learnings/unity-test-isolation.md — test patterns to enforce in reviews
5. learnings/event-driven-state-detection.md — architectural patterns to enforce
6. learnings/assigning-copilot-via-cli.md — Copilot bot ID is BOT_kgDOC9w8XQ, GraphQL only
7. learnings/copilot-fix-pr-hypothesis-verification.md — **diff inspection is not verification**; 0/3 success rate on diff-only fix approvals
8. learnings/unity-editmode-lifecycle-not-fired.md — `[Test]` EditMode does not auto-fire MonoBehaviour `Awake`; use `InvokePrivate` reflection pattern

Then read `MILESTONE_1_ACCEPTANCE.md` at the repo root — the canonical M1 success checklist. The completion promise must trace back to specific checkboxes there. (Source spec: `Corgi_Commando_Demo_Spec.docx`.)

## Current focus (v2)

This loop run picks up an in-flight state. The remaining technical work to close the M1 completion promise:

- **PR #57 — boss-intro retry / party-wipe / pre-boss game-over reload.** Currently CHANGES_REQUESTED. Known blockers: 3 of 4 new `BossCheckpoint` tests fail with NRE at `BossCheckpointPlayModeTests.cs:28` (likely `WhiskerbotController.Initialize` not routing through `EnemyAI.Initialize`'s health-attach path); 2 `SpawnManager` tests regressed from a stale rebase; needs rebase onto current main.
- **PR #56 — P2 drop-in / drop-out scene flow.** Currently CHANGES_REQUESTED. Known blocker: `SceneBootstrapPlayModeTests.cs` references `UnityEngine.InputSystem.Gamepad` but `CorgiCommando.Tests.PlayMode.asmdef` has `"overrideReferences": true` without `Unity.InputSystem` in its references list. Compilation fails before any test runs. Also needs rebase.
- **Issue #71 — LevelBackyard wave-clear: `IsBossDoorUnlocked` stays false after `ClearAllWaves()`.** 2 PlayMode tests red on main (`Level_Backyard_BossDoor_OnlyOpensAfterWave3Cleared`, `Level_Backyard_PlayThrough_SpawnToBossIntro`). Issue reopened in iteration 4 of the prior loop with 5 hypotheses + instrument-first guidance; Copilot has not yet pushed a fresh PR. Per the new learning, the next fix attempt MUST gather Debug.Log evidence first rather than guess from analysis alone.

Everything else (user-authored docs PRs #39 / #40 / #42, the `issues/` folder cleanup) is out of scope for this loop's completion promise.

## Iteration playbook

Each iteration, execute in this exact order:

1. **Sync.** Pull main, then list open PRs with their number, title, draft state, mergeable state, review decision, and head ref. Also check open issues for the milestone-1 label and for outstanding Copilot assignments.
2. **Score the queue.** Categorize each PR: APPROVED+MERGEABLE / CHANGES_REQUESTED / NEEDS_REVIEW / DRAFT / CONFLICTING / CI_PENDING / CI_FAILED.
3. **Trigger CI where needed.** If a PR shows no recent CI signal on its branch and the workflow auto-trigger is gated (`action_required`), dispatch a fresh run via `gh workflow run unity-tests.yml --ref <branch>`. CI signal is the merge precondition — do not approve fix PRs by diff inspection alone.
4. **Merge what is ready.** Squash-merge APPROVED+MERGEABLE+CI_GREEN PRs one at a time with branch deletion. After each merge, poll the PR list until no PR shows mergeable=UNKNOWN before merging another. (See pr-merge-sequencing.md for the polling pattern.)
5. **Review the next unreviewed PR.** Apply the strict posture from copilot-coding-agent-effectiveness.md: any unresolved acceptance-criteria gap is CHANGES_REQUESTED with concrete code snippets, not approve-with-notes. Reference the relevant `MILESTONE_1_ACCEPTANCE.md` checkbox the PR is meant to close.
6. **Check for regressions in parallel PRs.** When a fix lands for a project-wide hazard (deprecated API, removed asset, dead reference, conflict-hotspot file like SceneBootstrap.cs or HUDController.cs), grep open PRs for the same pattern and post heads-up comments with the exact code block before they merge.
7. **File next-up work.** If the queue is empty and M1 still has gaps, identify the single highest-leverage gap from `MILESTONE_1_ACCEPTANCE.md`, open one issue with a **Quality bar** section, an **Out of scope** section, and (for failing-test fixes) an **instrument-first** requirement. Assign Copilot via the GraphQL pattern in `learnings/assigning-copilot-via-cli.md`.
8. **Status line.** End the iteration with one sentence: what changed, what is next.

## Hard rules (violation = stop loop, escalate)

- **CI green on the branch before approval.** No diff-only approvals on fix PRs (see `learnings/copilot-fix-pr-hypothesis-verification.md` — 0/3 success rate when this was violated). `gh workflow run unity-tests.yml --ref <branch>` works to dispatch when auto-trigger is gated.
- **Never approve with unresolved acceptance criteria.** CHANGES_REQUESTED with concrete code snippets until clean.
- **Never merge while a downstream PR shows mergeable=UNKNOWN.** Poll first; ~10s delay after a merge is normal.
- **Never bundle production refactors into fix PRs.** Split them; CHANGES_REQUESTED if Copilot bundles (see PR #62 retrospective in pr-merge-sequencing.md).
- **Never let a fix PR ship without grepping open PRs for the same antipattern.** Post heads-up comments with the literal post-fix code block on each affected open PR.
- **Never decide architectural trade-offs unilaterally.** If an iteration hits a judgment call (refactor a conflict hotspot vs serialize? split a feature into two issues? rewrite vs patch? close a stale obsolete PR?), STOP the loop with AskUserQuestion. The user adjudicates.

## Stop conditions (any one triggers exit)

- **Completion promise becomes verifiably TRUE** — output it verbatim and exit. Verify by running through every condition explicitly, not by inference.
- **Max iterations reached.**
- **A hard rule fires its escalation.**
- **Two consecutive iterations produce no state change** (no merges, no reviews, no new issues, no CI dispatched) — surface the stall, ask the user what is blocking.

## Anti-patterns to avoid

- **Do not approve a fix PR based on the diff alone.** Even when the diff matches a documented pattern, the diff cannot tell you the underlying hypothesis is correct. Require CI green on the branch (`gh workflow run unity-tests.yml --ref <branch>` then wait, then inspect job conclusions).
- **Do not keep iterating when the work shifts from execute-checklist to make-a-judgment-call.** That is the cancel signal. STOP with AskUserQuestion.
- **Do not spend an iteration on bookkeeping if the PR list shows zero actionable state changes.** Surface the stall instead.
- **Do not fabricate test enemies via back-door methods** like `NotifyEnemyDied` with external objects. The back doors are getting tightened. Use real spawn paths and `health.TakeDamage(int.MaxValue)`.
- **Do not approve fix PRs that also include production scope creep** (see PR #62 retrospective in pr-merge-sequencing.md).
- **Do not trust GitHub mergeable state for ~10s after a merge.** Poll until UNKNOWN clears.
- **Do not propose hypothesis-driven fixes for failing tests when an instrument-first option exists.** A Debug.Log line + one CI run is cheaper than three rounds of speculative defensive patches (see iterations 1–7 of the first M1 loop for the receipts).
- **Do not assume `[Test]` (EditMode) auto-fires `Awake`.** It doesn't. Use `InvokePrivate(component, "Awake")` reflection, mirroring `SceneBootstrapTests.cs:32`.

## Memory

When you learn something genuinely new and reusable across sessions (a new anti-pattern, a workflow that saves time, a Unity quirk), add it to `learnings/` as a focused `.md` file. Do not restate things already in the existing learnings docs. If a learning contradicts an earlier one because conditions changed (e.g., CI gate was opened), update the earlier file rather than duplicating.

## How the completion promise maps to M1 acceptance

The Ralph loop's `--completion-promise` flag should be a verifiable subset of `MILESTONE_1_ACCEPTANCE.md`. Loop-friendly examples:

- *"All EditMode and PlayMode tests green on main with zero failures, no open Copilot-authored PRs remain in DRAFT or CHANGES_REQUESTED state, and no open issues carry the milestone-1 label."*
- *"PR #57, #56, and any Copilot PR for issue #71 are either merged or closed, AND `git log origin/main` shows the most recent Unity Tests workflow run green."*

Things the loop **cannot** verify on its own (do not put these in the promise):

- Playtest results from humans
- iOS-on-device confirmation
- Subjective "feels good" judgments

Those gate the **human's** Go/Pivot/No-go decision in `MILESTONE_1_ACCEPTANCE.md`, not the loop's exit.
