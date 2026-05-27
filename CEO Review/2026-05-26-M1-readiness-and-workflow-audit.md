# CEO Review — M1 Readiness & Workflow Audit

**Date:** 2026-05-26
**Reviewer:** Claude (Opus 4.7, 1M context)
**For:** Josh Patterson, CEO, JoshCorp

---

## Executive summary

**Milestone 1 is not ready for review and won't be today.** The product systems (combat, spawn, HUD, instrumentation) are 80% built, but the demo arc doesn't yet play end-to-end — two PlayMode tests are red on main and the two remaining feature PRs (#56 co-op, #57 boss retry) have bugs blocking merge. The workflow is producing real throughput (9 agent streams currently active) but has two structural risks worth addressing today: no human has ever played this game end-to-end, and iOS has never been built.

## Where we are

We've completed the build-out phase of M1. ~45 of ~60 acceptance checkboxes have implementation code on main. The instrumentation layer (`PlaytestMetrics`, `PlaytestBot`) landed in the last 12 hours and unblocks the entire automated verification pipeline. We are in the **verification phase** of M1, not the build phase — but we have not yet started verifying.

Strategic position is good. We have:
- A milestone-level acceptance file (`MILESTONE_1_ACCEPTANCE.md`) defining "done"
- An agent-driven playtest pipeline scoped across 12 issues (8 actively assigned to Copilot, 2 still gated by dependencies)
- A learnings folder capturing what we've paid for in pain so it doesn't repeat
- A Ralph loop v2 prompt that incorporates the lessons from attempt 1

Strategic risk is concentrated in two places:
- **Verification is theoretical.** Tests pass locally but the game has not been played to completion by anyone — human or agent. We don't actually know if the demo arc works.
- **iOS is a black box.** Half the M1 platform target has never been attempted. The longer this sits, the more compounding risk it carries.

## What's blocking us

In priority order, with rough cost-to-clear:

| Blocker | Severity | Owner | Cost to clear |
|---|---|---|---|
| Issue #71 — wave-clear → boss door broken on main | HARD (main is red) | Copilot, in flight | <2 agent hours if instrument-first works; up to a day if not |
| PR #57 — boss intro retry has 3 NRE + 2 regressions | HARD | Copilot, in flight | 1-3 agent hours |
| PR #56 — P2 drop-in/drop-out compile error | HARD | Copilot, in flight | 1-2 agent hours |
| Issue #79 — FullArcPlaytest not yet built | MAJOR | Copilot, just assigned | 1-2 agent hours |
| Issue #85 — exception capture not yet built | MAJOR | Copilot, just assigned | 30-60 agent minutes |
| First macOS playthrough | MAJOR | **CEO** | 30 human minutes |
| First iOS build attempt | MAJOR | **CEO** | 1-2 human hours (Xcode setup may eat most of it) |
| External playtester session | MAJOR | **CEO + 1 friend** | 60 human minutes |

The agent-side blockers are all "in flight" — under the Ralph loop, they progress without your attention. The human-side blockers do not progress without your attention. **Total human time to close all CEO-owned blockers: ~3-4 focused hours.**

## Decisions you need to make

These are the CEO-only calls. None should be made by me unilaterally.

### Decision 1: Sequence of human time

You have ~3-4 hours of CEO-owned verification work to clear. Three sequencing options:

- **(a) Wait for agent work to land first.** Let Copilot close #71, #56, #57, #79, #85 before you touch the game. Risk: discovers issues only after building on a faulty foundation.
- **(b) Play it now in current state.** Play the broken build today, log what's broken, fix in parallel with the agent work. Risk: tests are red — you'll hit known bugs and might miss new ones in the noise.
- **(c) Mixed.** Spend 30 min today playing whatever exists; 60 min tomorrow with #71 fixed; 90 min later this week with iOS smoke and external playtester.

**My recommendation: (c) Mixed.** The agent work clears the technical blockers; your time covers the irreducibly aesthetic blockers. Don't batch your time into one massive verification session — it makes the feedback loop too slow and creates over-investment in any one judgment.

### Decision 2: iOS criticality timing

M1 acceptance says iOS must work. We have zero iOS evidence. Three postures:

- **(a) iOS is a hard gate.** Don't declare M1 done until iOS is verified on a physical device with a BT controller. Cost: at least one workday of your time, likely more for first-time XCode + provisioning.
- **(b) iOS is a soft gate.** Declare M1 "demoable on macOS" with iOS as a follow-on milestone. Cost: a stated scope renegotiation; the acceptance file currently says iOS is required.
- **(c) Defer the decision.** Get the macOS demo green first; revisit iOS after a successful macOS demo. Cost: kicks risk down the road.

**My recommendation: (a) hard gate, but start now.** Issue an "iOS build smoke" task today even before the macOS demo is green. The first iOS build will surface 1-2 days of provisioning/SDK work that you cannot parallelize away. Start that wall clock now, in the background, while agents work on macOS.

### Decision 3: External playtester

M1 acceptance says 3 unfamiliar people must play. Currently zero. Three timings:

- **(a) Now, with current broken build.** Get directional feedback even on broken state. Risk: testers waste time on known bugs; feedback quality is low.
- **(b) After #71 fixes main and you've played once yourself.** Higher signal. Risk: delays first external feedback by 1-2 days.
- **(c) Only when you'd happily ship it.** Highest signal. Risk: by then it's late to iterate on findings.

**My recommendation: (b).** Specifically: book one external playtester for ~48 hours from now. That's enough lead time for the technical blockers to clear and for you to do your first playthrough, but tight enough that you're not over-polishing in their absence.

## What I observed

Operational notes that don't fit elsewhere. None of these are decisions — they're observations the CEO should be aware of.

### What's working

- **Issue spec template + acceptance file workflow.** The issue bodies filed today were tighter and more agent-actionable than earlier in the project. Copilot's first-pass quality is visibly improving when scope is explicit.
- **Ralph loop v2.** The prompt now incorporates the hard-won lessons (CI green before approval, instrument-first for bugs, no diff-only fix approvals). Throughput is up materially.
- **Learnings folder.** ~12 files now, capturing institutional memory most projects lose between sessions. This compounds.

### What's not working

- **Two-Claude-Code sessions running concurrently on the same repo.** Diagnosed early in this session as a race-condition risk. Observed in practice today: a `m_TimeScale: 0` regression appeared in `TimeManager.asset` from the other session's local state, and the two sessions' `.claude/` state files compete. The pattern produces silent state corruption. Recommend: one session at a time, or explicit coordination protocol.
- **You don't play your game.** This is the #1 strategic risk and the one most likely to bite at M1 review. The agent pipeline is being built specifically to compensate, but it can't fully substitute. Every day that passes without a human playthrough is a day where the feel of the product is unmeasured.
- **iOS is dark.** Zero attempts. The longer this sits, the more compounding risk.

### Leverage opportunities you might not see

- **Copilot is saturated, your time is the bottleneck.** Nine active agent streams; one human. Anything that converts human work into agent-spec-able work is leveraged. The recently filed diagnostic issues (#85-#90) are an example — they exist because you asked the right question, but Copilot is doing the work.
- **The MILESTONE_1_ACCEPTANCE file is the new contract.** It replaces the old `issues/` folder as system-of-record. Make sure your team (when you have one) knows that's where "done" is defined. Updates to the file = scope renegotiation, not casual edits.
- **The `learnings/` folder is institutional capital.** It's currently invisible because no one but you reads it. Worth referencing in your next CEO/founder communication — it demonstrates that the project compounds learning, not just code.

## Recommended next moves

In order. Each line is one item with rough effort.

1. **Right now:** Verify #71 / #56 / #57 / #79 / #85 are all assigned and in flight. ✓ (verified at time of writing)
2. **Today, your time, ~30 min:** Open the macOS build (whatever's on main right now) and try to play it. Even in broken state. Log three observations in a `CEO Review/playtest-log.md` — one thing that felt right, one thing that felt wrong, one surprise.
3. **Today, your time, ~90 min:** Attempt a first iOS build. Don't aim for runtime — just compile + provisioning. Surface the actual problems (XCode version, signing certs, GameCI setup) so they're not last-minute.
4. **Tomorrow morning:** Re-run this CEO Review after the overnight agent work lands. Check whether #71 instrument-first worked. Check whether #79 produces a passing full-arc run.
5. **48 hours from now:** Book one external playtester for a 30-minute recorded session.
6. **End of week:** Go/no-go on M1 against MILESTONE_1_ACCEPTANCE.md. Use the verification checklist; don't declare done from vibes.

## Net call

M1 is achievable this week if the agent pipeline holds and you spend 3-4 hours of CEO time on the verification work only you can do. The risk is not technical anymore — the systems exist. The risk is that the project's tightest constraint (your attention) doesn't get spent on the irreducibly human work in time. Schedule the playtest and the iOS build before today is over.

---

*Carmack: ship it. Boz: name the tradeoffs. Kroc: scale the verified version. Beauvier: back up before you go.*
