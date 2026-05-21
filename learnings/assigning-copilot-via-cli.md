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

## What Works (Sometimes): REST API

```bash
# Direct API call with POST to the assignees endpoint
gh api repos/OWNER/REPO/issues/N/assignees --method POST --input - <<< '{"assignees":["Copilot"]}'
```

The REST API accepts `Copilot` (capital C) as a valid assignee even though the CLI doesn't. **However**, this method is unreliable — it can return HTTP 200 but silently fail to assign Copilot.

## What Works (Reliably): GraphQL Mutation

```bash
# Step 1: Get Copilot's bot node ID (do this once, it's stable per repo)
gh api graphql -f query='{ repository(owner:"OWNER", name:"REPO") { issue(number:KNOWN_ISSUE) { assignees(first:5) { nodes { login id } } } } }' --jq '.data.repository.issue.assignees.nodes[] | select(.login == "Copilot") | .id'
# Returns something like: BOT_kgDOC9w8XQ

# Step 2: Get the issue's node ID
gh api graphql -f query='{ repository(owner:"OWNER", name:"REPO") { issue(number:N) { id } } }' --jq '.data.repository.issue.id'

# Step 3: Assign via mutation
gh api graphql -f query='mutation { addAssigneesToAssignable(input: { assignableId: "ISSUE_NODE_ID", assigneeIds: ["BOT_kgDOC9w8XQ"] }) { assignable { ... on Issue { assignees(first:5) { nodes { login } } } } } }' --jq '.data.addAssigneesToAssignable.assignable.assignees.nodes[].login'
```

For this repo, Copilot's bot ID is `BOT_kgDOC9w8XQ`.

## Gotcha: Self-Assignment Side Effect

Both methods may also add the authenticated user (you) as an assignee. Clean it up with:

```bash
echo '{"assignees":["YOUR_USERNAME"]}' | gh api repos/OWNER/REPO/issues/N/assignees --method DELETE --input - --jq '.assignees[].login'
```

## Verify Assignment

```bash
gh api repos/OWNER/REPO/issues/N -q '.assignees[].login'
# Should output: Copilot
```

## Prerequisites

- Copilot Coding Agent must be enabled on the repo (Settings > Copilot > Coding agent)
- The first assignment may need to happen via the GitHub UI to activate the agent on the repo
- Once activated, the GraphQL method works reliably for subsequent assignments
- The REST API method may stop working after initial activation — prefer GraphQL

## Batch Assignment Example

```bash
# Copilot bot ID (stable for this repo)
COPILOT_ID="BOT_kgDOC9w8XQ"

# Assign Copilot to issues 3 and 4
for i in 3 4; do
  ISSUE_ID=$(gh api graphql -f query="{ repository(owner:\"OWNER\", name:\"REPO\") { issue(number:$i) { id } } }" --jq '.data.repository.issue.id')
  gh api graphql -f query="mutation { addAssigneesToAssignable(input: { assignableId: \"$ISSUE_ID\", assigneeIds: [\"$COPILOT_ID\"] }) { assignable { ... on Issue { assignees(first:5) { nodes { login } } } } } }" --jq '.data.addAssigneesToAssignable.assignable.assignees.nodes[].login'
  # Clean up self-assignment
  echo "{\"assignees\":[\"YOUR_USERNAME\"]}" | gh api repos/OWNER/REPO/issues/$i/assignees --method DELETE --input -
done
```

## Sources

- https://github.blog/ai-and-ml/github-copilot/assigning-and-completing-issues-with-coding-agent-in-github-copilot/
- https://github.blog/changelog/2025-10-28-github-copilot-cli-use-custom-agents-and-delegate-to-copilot-coding-agent/
- https://github.blog/changelog/2025-09-25-copilot-coding-agent-is-now-generally-available/
