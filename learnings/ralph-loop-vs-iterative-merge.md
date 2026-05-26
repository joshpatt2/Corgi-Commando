# Ralph Loop vs Iterative Merge — When Each Mode Fits

**Date:** 2026-05-25
**Context:** During Milestone 1 sprint, the `/ralph-loop` skill was invoked with the prompt: *"finish the project so it meets milestone one by repeating what we have been doing, opening issues, assigning them to copilot and monitoring and reviewing PRs then merging approved ones."* The loop ran for 3 iterations before being manually cancelled. This doc captures what Ralph mode actually got us, what it didn't, and when the iterative-merge cadence is more appropriate.

## What Ralph Mode Did Well

- **Sustained context across long sessions.** The same goal (M1 completion) carried through context compactions without re-explaining. Each iteration started knowing the project state.
- **Removed the "what's next?" decision overhead.** No back-and-forth between human and agent about whether to merge X first or Y first — the agent just kept executing the same playbook.
- **Surfaced systemic patterns.** Repeating the same review loop across 8 PRs made structural anti-patterns visible (e.g., the SceneBootstrap conflict hotspot, fix-PR scope creep, regression-via-parallel-PR).
- **Documented its own work in flight.** Because every iteration restated context for the next, the running diary of decisions was searchable later.

## What Ralph Mode Did Poorly

### No natural stopping condition

The prompt was *"finish the project so it meets milestone one"* — a goal that has no programmatic completion check. The loop has no way to know when it's done, and the operator (Josh) has to manually cancel. In practice the iteration kept going past the natural "done for now" point because there was always *some* PR to nudge, *some* issue to file.

**Better pattern:** invoke `/ralph-loop` with `--completion-promise "All M1 acceptance criteria pass on main"` so there's a concrete exit. Even better: a CI check or test-count-stable signal.

### Spends turns on bookkeeping when nothing has changed

Several iterations checked `gh pr list`, found no actionable state changes, and burned a context cycle re-reading the same diffs. The dynamic `/loop` mode (with `ScheduleWakeup`) is better suited for "watch for change, act when something happens" — Ralph's eager re-fire wastes work on stable states.

**Better pattern:** for "monitor PRs and act on changes," use `/loop` with `ScheduleWakeup(delaySeconds=1200, …)`. For "execute this concrete checklist," Ralph fits.

### Hides the user from decision points

Ralph's value prop is "don't ask me, just keep going." But this sprint had several genuine adjudication moments:
- Should we serialize the SceneBootstrap PRs or refactor the hotspot?
- Is "approve-with-notes" or "strict CHANGES_REQUESTED" the right posture?
- Is PR #62's bundled scope acceptable or do we split it?

In iterative mode, each of these would have been a quick check-in with Josh. In Ralph mode, the agent made the call (correctly, as it turned out) but Josh didn't see the decisions framed. That's a coordination debt that compounds across the project.

**Better pattern:** add `AskUserQuestion` checkpoints to the Ralph prompt for explicit branch points. Ralph for execution; user for adjudication.

### Easy to forget it's running

The cancellation took manual intervention (`/ralph-loop:cancel-ralph`). If the user steps away from the terminal, Ralph keeps consuming context turns. This is a budgetary risk on long projects.

**Better pattern:** always set `--max-iterations N` even if you think you want unlimited. You can re-invoke if N wasn't enough; you can't un-spend tokens.

## When to Use Ralph

- **Concrete checklist with N items.** "Process these 12 issues" — set max-iterations=12.
- **Repetitive grunt work with deterministic exit.** "Apply this codemod to every file in src/" — exit when grep returns nothing.
- **Sprint completion with a verifiable end state.** "All tests pass on main and the milestone label has no open issues" — completion-promise it.
- **Anti-procrastination tool for the user.** When the human knows they'll lose focus partway through, Ralph keeps the agent on task.

## When NOT to Use Ralph

- **Open-ended exploration.** "Make the game good" has no exit. Use a regular session.
- **Work requiring frequent human judgment.** Code reviews of agent-authored PRs benefit from the user seeing each one. Iterative mode preserves that visibility.
- **Monitoring/polling tasks.** Use `/loop` with `ScheduleWakeup` instead — Ralph's eager re-fire wastes work.
- **Tasks where stopping cleanly matters more than running fast.** Ralph cancels mid-iteration. If you need a graceful checkpoint (e.g., commit-and-stop), iterative is safer.

## The Cancellation Pattern That Worked

`/ralph-loop:cancel-ralph` reads `.claude/ralph-loop.local.md`, removes it, reports the last iteration number. Simple, atomic, idempotent. No partial-state risk.

The lesson from this sprint: cancel early when the work shifts from "execute the playbook" to "make a judgment call." We cancelled at iteration 3 once the work shifted from "merge approved PRs" to "decide whether the SceneBootstrap conflict warrants a refactor PR." That was the right time to drop back to iterative mode.

## Net Assessment

Ralph mode was the right call for the merge-review-merge cadence in the middle of the sprint. It was the wrong call when the work shifted to architectural judgment. The transition signal: when the agent's iterations start looking like "post review, wait, post another review" rather than "decision → action → outcome," cancel and switch modes.

Net: **a useful tool with sharp edges**. Treat the max-iterations and completion-promise flags as defaults, not options. Cancel before the loop runs past its useful life — Ralph won't notice when it's done.
