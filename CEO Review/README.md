# CEO Review — JoshCorp / Corgi Commando

Strategic-level reviews delivered to Josh Patterson, CEO of JoshCorp. Not technical code reviews — these are about *the operation*: where the project stands, what tradeoffs are being made, what decisions need CEO attention, what's working in the workflow and what isn't.

## When a CEO Review fires

- **Milestone gates** — every milestone start, milestone end, go/no-go decision
- **Material state changes** — a feature lands that changes the trajectory, a hard blocker appears, a strategic option opens or closes
- **Operational shifts** — when how-we-work changes meaningfully (workflow pattern, agent allocation, division of labor)
- **CEO requests** — when explicitly asked

## Format

Each review is a single markdown file dated `YYYY-MM-DD-<topic-slug>.md`. Standard sections:

1. **Executive summary** — three sentences max. The headline.
2. **Where we are** — the strategic state, not a status report.
3. **What's blocking us** — explicit, prioritized, with cost estimates.
4. **Decisions you need to make** — the CEO-only calls. Never decided unilaterally.
5. **What I observed** — operational learnings, workflow risks, leverage opportunities.
6. **Recommended next moves** — concrete, prioritized, with effort estimates.

## Operating principles for these reviews

Written in the combined Carmack/Boz/Kroc voice articulated in `/Users/joshuapatterson/ai/.claude/CLAUDE.md`:

- **Carmack:** be technically honest. No softening hard truths. If the build doesn't run, say it.
- **Boz:** own the tradeoffs. Every recommendation has a cost; name it. No "comfortable lies."
- **Kroc:** think at scale. What's the throughput? Where's the chokepoint? What's irreversible?
- **Beauvier:** before recommending action, check what's reversible. Flag the high-blast-radius moves.

## What CEO Reviews are NOT

- Status updates (those go in git log + GitHub issues)
- Implementation plans (those go in issue specs)
- Cheerleading (the CEO doesn't need flattery; they need signal)
- Decisions made for the CEO (always frame as options + recommendation, never as faits accomplis)

## Index

Most recent first.

- 2026-05-26 — [M1 Readiness & Workflow Audit](2026-05-26-M1-readiness-and-workflow-audit.md)
