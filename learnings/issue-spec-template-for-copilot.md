# Issue Spec Template That Works for Copilot

**Date:** 2026-05-26
**Context:** Across 12 issues filed in one session (#77-#82 and #85-#90), a consistent body structure produced reliably good Copilot first-pass implementations. This document captures the template, with notes on which sections are load-bearing and why.

## The template

```markdown
## Goal

<One paragraph — what the implementation should do and the user-visible outcome.>

## Why

<One paragraph — why this matters. Reference acceptance criteria, observed bugs, or strategic context. Helps the agent prioritize edge cases.>

## Scope (in)

- <Bulleted list of concrete deliverables>
- File paths, class names, method signatures when known
- New assets and their location
- Specific tests to add

## Scope (out)

- <Bulleted list of explicitly-excluded scope>
- Things that look adjacent but are filed as separate issues
- Future enhancements that don't belong now

## Acceptance criteria

- <Bulleted checklist of binary pass/fail conditions>
- Reviewer will check each item before approving

## Quality bar

- <Bulleted list of generic-but-strict expectations>
- "All public method parameters used or removed"
- "Null-guard on Initialize"
- "No hardcoded values"
- Etc.

## Tests to pass

- `ExpectedTestName_ExpectedBehavior_ExpectedOutcome` (test category)
- Specific named tests, not just "tests pass"

## Dependencies

- Blocked by issue #XX (one-line reason)
- Serializes with issues #YY, #ZZ (file-conflict reason)

## Maps to MILESTONE_X_ACCEPTANCE.md

<One or two sentences naming the specific acceptance checkbox(es) this issue closes.>
```

## What each section does

### Goal + Why
The agent reads these first. **Goal** describes what to build. **Why** describes the constraints implicit in the build. Skipping Why leads to literal-but-wrong implementations (the "minimum-viable to pass tests" failure mode documented in [[copilot-coding-agent-effectiveness]]).

### Scope (in) and Scope (out) — both are load-bearing
**Scope (in)** with concrete file paths and method signatures dramatically reduces the agent's freedom to invent its own structure. Without explicit paths, you get inconsistent file organization across PRs.

**Scope (out)** is the antidote to scope creep. Without it, the agent often "while I'm in here" refactors adjacent code (see PR #62 retrospective in [[pr-merge-sequencing]]). Stating *what NOT to touch* is as important as stating what to touch.

### Acceptance criteria vs Tests to pass — different lenses
**Acceptance criteria** describe behavior in domain terms ("PR diff includes all five hook sites"). **Tests to pass** are the executable form of those criteria ("`PlaytestMetrics_LogException_CapturesNullRef` passes").

Both are needed because:
- Tests-only specs let agents pass tests with minimum-viable code that satisfies no other criterion (see [[copilot-coding-agent-effectiveness]])
- Acceptance-only specs let agents over-engineer by adding things that aren't strictly required to pass tests
- Having both keeps the agent honest in both directions

### Quality bar — codifies recurring agent failure modes
This section came out of observed Copilot anti-patterns:
- Unused method parameters (PR #27 boss)
- Hardcoded values that should come from data (PR #27 pilot HP)
- Missing null-guards on `Initialize`
- Missing input validation on public methods (PR #28 OnPlayerDropIn)

By listing these as standing rules in every issue, the floor of agent output rises without per-PR feedback. Each rule was added after the project hit a bug caused by its absence — it's not theoretical hygiene.

### Dependencies — drives assignment scheduling
The reviewer uses this to decide *when* to assign Copilot. Issues blocked by other in-flight work shouldn't be assigned yet (the agent will produce code against stale assumptions). Issues that serialize with other parallel work need to be assigned one at a time.

Two flavors of dependency:
- **Blocked by:** the issue can't start until another lands (hard)
- **Serializes with:** the issue can start but its PR will conflict with parallel siblings (soft — see additive-vs-competing-conflicts note in [[pr-merge-sequencing]])

### Maps to MILESTONE_X_ACCEPTANCE.md — closes the contract loop
Every issue should point to a specific checkbox in the milestone acceptance file. This:
- Makes the work's value explicit (no orphan-feature work)
- Helps the reviewer verify that the PR closes what it claims
- Makes the completion-promise traceable from acceptance → issue → PR

If you can't write this section because no acceptance checkbox is closed by the work, that's a signal: the issue might be premature, out-of-scope for the current milestone, or covering yak-shaving rather than user-visible deliverable.

## Anti-patterns to avoid in issue bodies

- **Vague Acceptance criteria:** "tests pass" is not an acceptance criterion. Name the tests.
- **Missing Scope (out):** the agent will guess; you may not like the guess.
- **Quality bar phrased as suggestions:** *"prefer to validate inputs"* leaves the agent room to skip it. State as a rule: *"public methods that accept indices MUST validate range."*
- **Hidden production scope inside test-fix PRs:** see PR #62. The issue body should explicitly forbid bundling.
- **Title without spec-number prefix:** `N. Title` keeps the issue queue chronologically scannable. Without it, you lose the at-a-glance sense of project sequence.

## What this template doesn't do

It doesn't generate good acceptance criteria for free. Writing the criteria — *what does "done" actually look like for this scope?* — is the irreducible human work. The template is the scaffolding; the user still has to fill in domain-specific content for each issue.

Under the agent-time-vs-human-time economics (see [[agent-time-vs-human-time-economics]]), the human time spent on a well-scoped issue body is the highest-leverage time in the workflow. A 5-minute issue body saves 30 minutes of re-iteration.

## Related learnings

- [[copilot-coding-agent-effectiveness]] — feedback patterns once a PR exists
- [[pr-merge-sequencing]] — how dependencies inform merge scheduling
- [[acceptance-checklist-over-issue-specs]] — why the "Maps to" section matters
- [[agent-time-vs-human-time-economics]] — why writing tight specs is the human-time sweet spot
