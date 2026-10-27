# Dependabot automation diagnosis - 2026-10-23

## Goal

Explain why the open dependency PRs were not auto-merged and why the dependency fixer did not repair them.

## Verified findings

- Open PRs: #24 (GitHub Actions), #25 (NuGet), and #26 (npm). All three head commits have successful backend and frontend CI checks; their auto-merge jobs were skipped.
- The auto-merge workflow deliberately excludes GitHub Actions updates. NuGet and npm updates require the repository variable `DEPENDABOT_AUTOMERGE` to equal `true`. Their skipped jobs indicate this gate was not enabled when the PR events ran. The current variable value was not directly inspected.
- #25 and #26 report `mergeable_state: clean` and have no auto-merge request.
- Follow-up confirmed the root cause: the user stored `DEPENDABOT_AUTOMERGE=true` under Actions **Repository secrets**, but the workflow reads `vars.DEPENDABOT_AUTOMERGE`. A secret does not populate the `vars` context. Create the flag under Actions > Variables > Repository variables instead; do not change the workflow to read a secret.
- The user confirmed repository auto-merge is enabled and main has no-delete and CI-build policies. Exact required-check names were not independently inspected.
- Tier 1 is scheduled and running. Run [39964533786](https://github.com/mgorczyca12/polonia-airlines/actions/runs/39964533786), on October 23 at 13:50 UTC, completed pre-activation successfully. Its logs explicitly report zero matching failing Dependabot PRs, so activation and the agent were skipped.
- Tier 2 has zero workflow runs. It requires manual dispatch or escalation from tier 1; no current PR has the escalation label.
- Both fixer workflows have `safe-outputs.staged: true`: even a future repair run is configured for preview rather than applying changes.

## Files and checks

- Inspected auto-merge, tier 1, tier 2, and CI workflow configuration, live PR/check data, workflow runs, and tier 1 pre-activation logs.
- Initial worktree was clean. Only this summary was added; no workflow, repository setting, PR, or remote branch was changed.
- GitHub CLI was unauthenticated; GitHub MCP read tools supplied the remote evidence.
- No builds or tests were run: this was an automation diagnosis, not a code change.

## Pending

- Create the repository Actions variable `DEPENDABOT_AUTOMERGE=true` in the Variables tab, not the Secrets tab. Confirm both CI jobs are required.
- Re-trigger eligible PR events after enabling the gate; changing a variable alone does not rerun the original event.
- Keep Actions updates manual unless deliberately changing policy.
- Decide separately whether to move fixer safe outputs out of staged/preview mode.
