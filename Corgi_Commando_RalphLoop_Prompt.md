# Corgi Commando — Ralph Loop Prompt (Milestone 1)

You are continuing work on Corgi Commando (Unity 2022.3.62f1, 2.5D brawler). The goal is to finish Milestone 1 by running a tight Copilot-as-author / Claude-as-reviewer cycle.

## Read first (mandatory before any tool calls)

Read every file in the learnings/ folder in this order:

1. learnings/ralph-loop-vs-iterative-merge.md — what to do differently this time
2. learnings/copilot-coding-agent-effectiveness.md — feedback patterns, quality bar
3. learnings/pr-merge-sequencing.md — conflict hotspots, merge order, scope discipline
4. learnings/unity-test-isolation.md — test patterns to enforce in reviews
5. learnings/event-driven-state-detection.md — architectural patterns to enforce
6. learnings/assigning-copilot-via-cli.md — Copilot bot ID is BOT_kgDOC9w8XQ, GraphQL only

Then read Corgi_Commando_Demo_Spec.docx (or the equivalent M1 spec at repo root) to ground your understanding of what M1 actually requires.

## Iteration playbook

Each iteration, execute in this exact order:

1. Sync. Pull main, then list open PRs with their number, title, draft state, mergeable state, review decision, and head ref.
2. Score the queue. Categorize each PR: APPROVED+MERGEABLE / CHANGES_REQUESTED / NEEDS_REVIEW / DRAFT / CONFLICTING.
3. Merge what is ready. Squash-merge APPROVED+MERGEABLE PRs one at a time with branch deletion. After each merge, poll the PR list until no PR shows mergeable=UNKNOWN before merging another. (See pr-merge-sequencing.md for the polling pattern.)
4. Review the next unreviewed PR. Apply the strict posture from copilot-coding-agent-effectiveness.md: any unresolved acceptance-criteria gap is CHANGES_REQUESTED with concrete code snippets, not approve-with-notes.
5. Check for regressions in parallel PRs. When a fix lands for a project-wide hazard (deprecated API, removed asset, dead reference), grep open PRs for the same pattern and post heads-up comments before they merge.
6. File next-up work. If the queue is empty and M1 still has gaps, identify the single highest-leverage gap from the spec, open one issue with a Quality bar section and an Out of scope section, assign Copilot via the GraphQL pattern documented in learnings/assigning-copilot-via-cli.md.
7. Status line. End the iteration with one sentence: what changed, what is next.

## Hard rules (violation = stop loop, escalate)

- Never approve with unresolved acceptance criteria. CHANGES_REQUESTED until clean.
- Never merge while a downstream PR shows mergeable=UNKNOWN. Poll first.
- Never bundle production refactors into fix PRs. Split them; CHANGES_REQUESTED if Copilot bundles.
- Never let a fix PR ship without grepping open PRs for the same antipattern.
- Never decide architectural trade-offs unilaterally. If an iteration hits a judgment call (refactor a conflict hotspot vs serialize? split a feature into two issues? rewrite vs patch?), STOP the loop with AskUserQuestion. The user adjudicates.

## Stop conditions (any one triggers exit)

- Completion promise becomes verifiably TRUE — output it verbatim and exit.
- Max iterations reached.
- A hard rule fires its escalation.
- Two consecutive iterations produce no state change (no merges, no reviews, no new issues) — surface the stall, ask the user what is blocking.

## Anti-patterns from attempt 1 to avoid

- Do not keep iterating when the work shifts from execute-checklist to make-a-judgment-call. That is the cancel signal.
- Do not spend an iteration on bookkeeping if the PR list shows zero actionable state changes. Surface the stall instead.
- Do not fabricate test enemies via back-door methods like NotifyEnemyDied with external objects. The back doors are getting tightened. Use real spawn paths and health.TakeDamage(int.MaxValue).
- Do not approve fix PRs that also include production scope creep (see PR #62 retrospective in pr-merge-sequencing.md).
- Do not trust GitHub mergeable state for ~10s after a merge. Poll until UNKNOWN clears.

## Memory

When you learn something genuinely new and reusable across sessions (a new anti-pattern, a workflow that saves time, a Unity quirk), add it to learnings/ as a focused .md file. Do not restate things already in the existing learnings docs.
