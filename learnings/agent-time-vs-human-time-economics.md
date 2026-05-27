# Agent Time vs Human Time — The Cost Inversion

**Date:** 2026-05-26
**Context:** Mid-session, the user articulated an explicit premise that reshaped the entire approach to playtest and verification: *"Human time is many orders of magnitude more costly than agent time."* This document captures the workflow shifts that follow from accepting that premise, and what the wrong-default looks like.

## The premise

For a project where:
- The user pays per-token for agent compute and per-month for service tiers
- The user has finite focus hours per day (8 if you're lucky, fewer in practice)
- The deliverable is a vertical-slice game with ~60 acceptance checkboxes

...agent time costs roughly $0.01–$0.10 per minute. Human focus time costs hundreds of dollars per hour at any commercial rate, and is irreplaceable (you can't buy more focus hours from an exhausted founder).

That's at least 100x, plausibly 1000x, cost ratio. Once you accept the ratio, several "obviously good" workflow choices invert.

## What inverts

### "Test it manually first" → "Automate first, even when manual seems faster"

The intuition "I'll just play it once to check" is right at scale 1:1 (human time ≈ agent time). It's wrong at 1000:1. The right move is to spend 5x the agent-hours building a `PlaytestBot` + `PlaytestMetrics` + headless full-arc test so that the playthrough becomes a CI artifact. After the buildout, every PR gets a free playthrough. Without it, every PR gates on the human's willingness to test it.

Concretely in this project: issues #77, #78, #79 (PlaytestBot, PlaytestMetrics, FullArcPlaytest) are the explicit instantiation of this premise. Total agent time to build them: hours. Total human time saved: every PR review forever.

### "Daily play ritual" → "Weekly aesthetic-only playthrough"

Earlier in the same session, the wrong advice was given: *"play the game daily, log three things you observed."* That advice was calibrated for 1:1 time costs. Under 1000:1, the correct cadence is:
- Daily: agent runs full-arc playthrough, captures screenshots, generates summary
- Weekly: human spends 30 minutes — reviews the latest agent recording at 4x speed, then 15 minutes of hands-on play targeting one specific feel question

The human time goes into things only humans can answer: *does the punch feel weighty*, *is the boss intimidating*. Not *did the boss spawn*, which is automatable.

### "Tests verify, humans validate" → "Tests verify metrics, humans validate only aesthetic residue"

Most of what feels like "human-only" validation is actually instrumentable. Hitstop duration in frames is a number; the call that 3-6 frames is the right range is the only aesthetic part. Boss attack telegraph visibility is a screenshot the AI can describe; the call that the telegraph is *good* is the only aesthetic part. Decompose every "human-only" task and find the measurable substrate. Reserve human attention for the irreducible aesthetic residue.

### "Don't over-instrument, premature optimization" → "Pre-instrument every category"

Wrong default at 1:1 costs. Right default at 1000:1: every diagnostic gap you can imagine is worth instrumenting in advance, because the marginal agent cost is near-zero and the marginal human time saved on debugging is enormous. Don't wait for a bug to motivate adding exception capture. Add it now. Concrete example: issues #85–#90 (exception capture, damage events, input lifecycle, position snapshots, init tracking, asset resolution) — all filed before any of them was acutely needed.

### "Serial PRs to avoid conflicts" → "Maximum-parallel PRs, accept additive conflicts"

The merge-sequencing advice in `pr-merge-sequencing.md` correctly identifies conflict hotspots as a risk. Under 1:1 cost, serializing is the right move. Under 1000:1, the right move is to dispatch all parallel work even when conflicts are likely, because:
- Conflict re-resolution by an agent is 30–90 seconds of agent time
- Waiting for one PR to land before assigning the next is hours of wall-clock time
- The conflict cost (agent re-resolves) is cheaper than the queuing cost (wall clock blocks the human)

The exception is when the conflicts are *competing intent* (different PRs trying to do the same thing differently) — those still serialize. Additive conflicts (different PRs each appending to the same file) parallelize fine.

## How to apply

When facing any workflow choice, ask: *which path consumes less human time?* Not "which is technically cleaner" or "which feels right" — which one preserves the irreplaceable resource.

- Setting up automation: usually worth it even when the manual path is 5x faster *for one execution*.
- Reviewing agent output: spend the time, it's the highest leverage human work — agents need feedback to improve.
- Writing specs for agents: spend the time, every minute of clearer scope saves hours of re-iteration.
- Hand-playing the game: spend the *minimum* needed for aesthetic judgment, no more.
- Triaging open issues: automatable if criteria are explicit.

## When the premise breaks

The 1000:1 ratio assumes agent output is reliable when given clear scope. When it isn't (e.g., a bug that requires deep domain context the agent doesn't have), the ratio collapses toward 1:1 because every minute of agent work needs minutes of human verification. Watch for:

- Repeated CHANGES_REQUESTED on the same PR — agent isn't converging
- Agent rebuilding the same thing differently each time — scope isn't clear
- Tests passing but the feature still wrong — instrumentation is missing the actual contract

These signals mean the cost ratio has temporarily collapsed and human time should re-engage until the agent path is back on track.

## Related learnings

- [[copilot-coding-agent-effectiveness]] — when agent output is reliable and when it isn't
- [[copilot-fix-pr-hypothesis-verification]] — when to invest human time in verification despite the ratio
- [[ralph-loop-vs-iterative-merge]] — when to let agents run unattended
