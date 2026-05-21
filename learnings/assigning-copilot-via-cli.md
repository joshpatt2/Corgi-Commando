# Assigning GitHub Copilot Coding Agent to Issues via CLI

**Date:** 2025-05-21
**Context:** Setting up Corgi Commando repo, needed to assign Copilot to issues programmatically.

## What Doesn't Work

```bash
# gh issue edit --add-assignee does NOT recognize Copilot as a valid user
gh issue edit 1 --repo OWNER/REPO --add-assignee copilot    # fails: 'copilot' not found
gh issue edit 1 --repo OWNER/REPO --add-assignee Copilot    # fails: 'Copilot' not found
```

The `gh issue edit` command validates assignees against the repo's collaborator list, and Copilot isn't a standard collaborator — it's a special GitHub agent account.

## What Works

```bash
# Direct API call with POST to the assignees endpoint
gh api repos/OWNER/REPO/issues/N/assignees --method POST --input - <<< '{"assignees":["Copilot"]}'
```

The REST API accepts `Copilot` (capital C) as a valid assignee even though the CLI doesn't. This is the reliable programmatic method.

## Gotcha: Self-Assignment Side Effect

The API call above may also add the authenticated user (you) as an assignee. Clean it up with:

```bash
gh api repos/OWNER/REPO/issues/N/assignees --method DELETE --input - <<< '{"assignees":["YOUR_USERNAME"]}'
```

## Verify Assignment

```bash
gh api repos/OWNER/REPO/issues/N -q '.assignees[].login'
# Should output: Copilot
```

## Prerequisites

- Copilot Coding Agent must be enabled on the repo (Settings > Copilot > Coding agent)
- The first assignment may need to happen via the GitHub UI to activate the agent on the repo
- Once activated, the API method works for subsequent assignments

## Batch Assignment Example

```bash
# Assign Copilot to issues 1 and 2
for i in 1 2; do
  gh api repos/OWNER/REPO/issues/$i/assignees --method POST --input - <<< '{"assignees":["Copilot"]}'
  gh api repos/OWNER/REPO/issues/$i/assignees --method DELETE --input - <<< '{"assignees":["YOUR_USERNAME"]}'
done
```

## Sources

- https://github.blog/ai-and-ml/github-copilot/assigning-and-completing-issues-with-coding-agent-in-github-copilot/
- https://github.blog/changelog/2025-10-28-github-copilot-cli-use-custom-agents-and-delegate-to-copilot-coding-agent/
- https://github.blog/changelog/2025-09-25-copilot-coding-agent-is-now-generally-available/
