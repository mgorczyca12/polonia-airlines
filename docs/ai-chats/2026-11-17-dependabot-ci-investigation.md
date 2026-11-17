# Dependabot CI investigation

Date: 2026-11-17. Prior context: [gh-aw upgrade](./2026-11-16-gh-aw-upgrade.md).

## Findings

- PR #44 updates OpenTelemetry ASP.NET Core and HTTP instrumentation from 1.20.1 to 1.20.2. Its backend build passed, but the shared observability integration test failed because it did not capture the expected correlated HTTP server activity. The logs do not establish whether the dependency update or test behavior caused it.
- PR #46's current CI passed, including frontend and backend. Its separate Dependabot auto-merge job failed because the `dependency-tier2` label was not found, preventing tier 2 dispatch.
- An earlier CI run on #46 failed a different backend broker integration test: the saga reached `Cancelled`, but the reservation status was still `Created` when asserted. Its current CI later passed; the reason for the earlier mismatch is unverified.
- Tier 1's scheduled runs at 03:12 and 13:52 UTC on Nov 17 skipped activation: the `skip-if-no-match` search returned zero results. Removing `status:failure` from the same search returns both PRs, while the combined commit-status API reports no statuses for #44 despite its failed Actions check. The search gate does not detect this failure; the distinction between commit statuses and Actions check runs is a likely explanation, not independently proven to be GitHub search's general behavior.
- Tier 2 has no recorded runs. The #46 major-upgrade router stopped at the missing `dependency-tier2` label before reaching its dispatch command.

## Checks and follow-up

- Initially inspected PR diffs, check statuses, workflow jobs, failure logs, and the relevant test and workflow source without making changes.
- Investigate or rerun #44's observability test, and investigate the earlier broker status mismatch separately.
- Create the missing repository label and rerun the major-upgrade routing workflow, then verify tier 2 review and its checks before merging #46.
- Replace or redesign tier 1's failure detection to evaluate current check runs and commit statuses; preserve its scope and exclusions, and verify pending, failed, and successful cases before relying on it.

## Automation repair

- Created the missing `dependency-tier2` and `needs-human-decision` repository labels.
- Replaced the tier 1 search's `status:failure` filter with a deterministic pre-agent gate that inspects current check runs and the latest commit status per context for eligible PRs. The agent also checks both forms of status before selecting a PR.
- Added routing tests and CI coverage, synchronized the workflow's setup-action pins with the generated file, and regenerated the tier 1 lock while retaining the current gh-aw setup action.
- `node --test` passed all 46 routing and merge-policy tests. `gh aw compile` and `gh aw validate` passed; a read-only live API check selected #44. `git diff --check` passed.
- In an isolated checkout of #44, the specific observability test passed, then all 48 shared integration tests passed. This does not explain the earlier CI failure; a fresh GitHub CI run is still needed.
- Five further runs of the 48 shared integration tests on #44 passed locally.
- PR #47's required backend and frontend checks passed, but the advisory reviewer failed before starting: its setup action had no reviewed checksum for firewall v0.28.37. All five agent workflows used this incompatible combination. Updated them to firewall v0.28.62, whose checksum is in the pinned setup action, and regenerated locks with immutable container digests. Remote execution remains to be verified.
- Compilation and validation of all five workflows succeeded with two existing warnings, concerning the docs updater's `pull_request_target` trigger and the test writer's unrestricted push target. The 46 dependency policy and routing tests passed again.
- PR #47 merged to main after backend, frontend and advisory reviewer checks passed; the reviewer successfully installed the firewall and executed. The PR's advisory findings were inspected and its description updated.
- Reran #44's failed CI and #46's router. The router added the label but failed at dispatch because it runs without a checkout and `gh workflow run` could not infer the repository. Dispatched #46 to tier 2 explicitly and added `GH_REPO` to the router in a follow-up fix. CI and tier 2 results are pending.
- PR #44's backend rerun passed, and the PR was merged. A live Tier 1 run completed its gate and correctly skipped the agent when no eligible check was failing.
- PR #49's backend CI failed at the same observability assertion on its pre-#44 base. The response may arrive before the HTTP server activity is exported; the test now waits, with a timeout, for that correlated activity before asserting rather than racing the exporter. The targeted test and three 48-test shared integration runs passed locally; both backend format checks passed. CI must verify the change.
- Tier 2 reviewed #46 and created decision issue #50 for added upstream licensing notices; it did not run its own frontend checks because of its environment restrictions. Its label and comment outputs could not target a PR from a dispatch event, so the human-decision label and issue link were applied manually. Auto-merge was confirmed disabled. Both tier workflows now configure explicit, label-restricted comment and label targets; their regenerated locks compiled and validated without warnings. Runtime verification is pending.
