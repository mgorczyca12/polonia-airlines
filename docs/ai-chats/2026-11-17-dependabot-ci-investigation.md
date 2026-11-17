# Dependabot CI investigation

Date: 2026-11-17. Prior context: [gh-aw upgrade](./2026-11-16-gh-aw-upgrade.md).

## Findings

- PR #44 updates OpenTelemetry ASP.NET Core and HTTP instrumentation from 1.20.1 to 1.20.2. Its backend build passed, but the shared observability integration test failed because it did not capture the expected correlated HTTP server activity. The logs do not establish whether the dependency update or test behavior caused it.
- PR #46's current CI passed, including frontend and backend. Its separate Dependabot auto-merge job failed because the `dependency-tier2` label was not found, preventing tier 2 dispatch.
- An earlier CI run on #46 failed a different backend broker integration test: the saga reached `Cancelled`, but the reservation status was still `Created` when asserted. Its current CI later passed; the reason for the earlier mismatch is unverified.
- Tier 1's scheduled runs at 03:12 and 13:52 UTC on Nov 17 skipped activation: the `skip-if-no-match` search returned zero results. Removing `status:failure` from the same search returns both PRs, while the combined commit-status API reports no statuses for #44 despite its failed Actions check. The search gate does not detect this failure; the distinction between commit statuses and Actions check runs is a likely explanation, not independently proven to be GitHub search's general behavior.
- Tier 2 has no recorded runs. The #46 major-upgrade router stopped at the missing `dependency-tier2` label before reaching its dispatch command.

## Checks and follow-up

- Inspected PR diffs, check statuses, workflow jobs, failure logs, and the relevant test and workflow source. No tests were run locally and no GitHub actions or settings were changed.
- Investigate or rerun #44's observability test, and investigate the earlier broker status mismatch separately.
- Create the missing repository label and rerun the major-upgrade routing workflow, then verify tier 2 review and its checks before merging #46.
- Replace or redesign tier 1's failure detection to evaluate current check runs and commit statuses; preserve its scope and exclusions, and verify pending, failed, and successful cases before relying on it.

## Automation repair

- Created the missing `dependency-tier2` and `needs-human-decision` repository labels.
- Replaced the tier 1 search's `status:failure` filter with a deterministic pre-agent gate that inspects current check runs and the latest commit status per context for eligible PRs. The agent also checks both forms of status before selecting a PR.
- Added routing tests and CI coverage, synchronized the workflow's setup-action pins with the generated file, and regenerated the tier 1 lock while retaining the current gh-aw setup action.
- `node --test` passed all 46 routing and merge-policy tests. `gh aw compile` and `gh aw validate` passed; a read-only live API check selected #44. `git diff --check` passed.
- In an isolated checkout of #44, the specific observability test passed, then all 48 shared integration tests passed. This does not explain the earlier CI failure; a fresh GitHub CI run is still needed.
