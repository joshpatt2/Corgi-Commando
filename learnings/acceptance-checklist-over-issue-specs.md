# Acceptance Checklist as System of Record, Not Per-Issue Spec Files

**Date:** 2026-05-26
**Context:** This project bootstrapped with an `issues/` folder containing 20 markdown spec files (issues 01-18, 29, 30). All 20 mapped to closed GitHub issues. The folder stopped being updated after spec #18 — every subsequent issue (#31 onward) was filed directly via `gh issue create` with no local spec file. The folder became dead weight: it looked authoritative but was 5+ days out of date. This document captures why we deleted it and replaced it with a single milestone-level acceptance file.

## The pattern that worked early

For project bootstrap, the spec-file-per-issue pattern was good:
- 13 numbered system specs (Input, Combat, Movement, etc.) — each got a markdown file with detailed acceptance criteria
- Files lived in version control, were greppable, and could be referenced across PRs
- The file numbers and the issue title prefix (`14. Vertical Slice...`) kept them aligned
- Copilot read the file as part of the issue context and the structure was reliably parseable

## Why it broke

Once the project moved out of bootstrap into iteration:
- New issues were small (test fixes, bug reproductions, follow-on work) — writing a full spec file for each was overkill
- The marginal value of a local file vs the GitHub issue body was zero (GitHub issues have the full spec content)
- The spec files kept their original "system overview" framing while the actual work shifted to "fix this specific thing"
- Numbers diverged: file 30 → GitHub issue #44 (because PRs share the GitHub number sequence)
- Nobody updated the folder; everyone authored directly on GitHub

After 5 days of drift, the folder was misleading: anyone reading the repo could reasonably assume it was current. It wasn't.

## What replaced it

A single file at the repo root: `MILESTONE_1_ACCEPTANCE.md`. Structure:
- The Three Pillars (high-level binary go/no-go)
- Success Criteria (decomposed checklist, ~60 verifiable items)
- Kill Criteria (any-one = FAIL conditions)
- Verification (how each criterion is proved)
- Out of Scope (deferred to later milestones)
- Decision Gate (go / pivot / no-go after the verification phase)

The principle: **one file per milestone, not one file per issue**. The milestone file defines what "done" means and never moves until scope is renegotiated. GitHub issues are the unit of work; the milestone file is the contract those issues collectively satisfy.

## How to apply

For a new project or milestone:

1. **Write the acceptance file first.** Decompose the milestone deliverable into 30-60 verifiable checkboxes. Each checkbox should be objectively pass/fail, not "looks good."
2. **Group checkboxes by domain** (Controls, Combat, Waves, etc.) to match how the work decomposes.
3. **Include the kill criteria** — the conditions that fail the milestone regardless of other progress.
4. **Specify the verification methods** — for each checkbox, how is it proved? Unit test? Playtest? External review?
5. **Reference the file from every issue.** Each issue body has a "Maps to MILESTONE_X_ACCEPTANCE.md" section pointing to the specific checkbox(es) it closes.
6. **Make the Ralph loop / agent completion-promise reference it.** Don't write a separate completion criterion in the prompt; point to the file. *"Completion: all checkboxes in MILESTONE_X_ACCEPTANCE.md are checked."*
7. **Update the file when scope is renegotiated.** Don't let drift happen silently.

## How to phase out a stale `issues/` folder

If a project has accumulated a per-issue spec folder that's no longer current:

1. **Don't backfill.** Writing specs for the ~25 issues you missed is significant work for unclear value (the GitHub issues already have the content).
2. **Archive or delete.** Move to `issues/archive-bootstrap/` to preserve as history, or `git rm -r issues/` to remove entirely. Git history preserves the files either way.
3. **Document the convention change.** Add a one-liner to README or ARCHITECTURE.md: *"Issues live on GitHub; M1 acceptance criteria live in MILESTONE_1_ACCEPTANCE.md."*
4. **Don't recreate the folder.** When tempted to write a new spec file locally, file it as a GitHub issue with the full spec in the body instead.

## When the per-issue file pattern IS right

A few scenarios where local spec files beat GitHub-only:
- **Cross-repo specs** that multiple projects reference — file lives in a shared docs repo
- **Offline editing** when network access to GitHub isn't reliable
- **Specs that are themselves source-of-truth artifacts** (e.g., API contracts checked into version control alongside the code that implements them)
- **Living specs** that evolve continuously with the code — these belong in version control next to what they describe

For ad-hoc per-bug or per-feature work, GitHub issues are sufficient.

## Carmack-style summary

The acceptance checklist file is the contract. Issues are the unit of work that satisfies the contract. One central file with clear pass/fail criteria beats N drifting per-issue specs. Don't pretend a stale folder is the system of record; either keep it current or delete it.

## Related learnings

- [[copilot-coding-agent-effectiveness]] — concrete > vague feedback to agents
- [[agent-time-vs-human-time-economics]] — why we don't backfill 25 spec files
