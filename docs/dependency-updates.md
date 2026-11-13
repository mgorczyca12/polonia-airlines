# Dependency update automation

Dependabot opens weekly NuGet, npm and GitHub Actions updates with a seven-day
cooldown. NuGet/npm minor and patch updates remain grouped. The MassTransit
major-version exclusions remain unchanged because of the commercial licensing
change.

## Routing and decisions

- NuGet/npm minor and patch PRs request squash auto-merge through
  [the Dependabot router](../.github/workflows/dependabot-automerge.yml).
- Every new, reopened or synchronized NuGet/npm major PR goes directly to
  [Tier 2](../.github/agents/dependency-fixer-tier2.agent.md), including upgrades
  with green CI and framework upgrades. The router adds `dependencies` and
  `dependency-tier2` before dispatching the review.
    Tier 2 uses the central `medium-sol` model profile (`gpt-6.1-sol`).
- [Tier 1](../.github/agents/dependency-fixer.agent.md) repairs failing minor/patch
  PRs. It routes any major upgrade it discovers to Tier 2 without attempting a
  repair, and retains its small-repair and human-escalation limits.
- Tier 2 checks upstream breaking changes, affected usage, runtime behavior,
  transitive compatibility, licensing and security implications. It makes
  bounded compatibility fixes and runs applicable builds/tests, or approves the
  exact reviewed head without code changes if the upgrade is already compatible.
- Both fixer workflows now apply their configured safe outputs rather than
  staging previews. Unrelated agent workflows remain unchanged.

Tier 2 leaves substantial changes for manual review: Domain/shared edits, new
architecture/abstractions, migrations, public API/event contract changes,
licensing/commercial changes, material behavior changes or uncertainty, more
than **8 total files in the final PR diff**, or incomplete/failed validation.
It adds `needs-human-decision`, leaves a PR analysis, and creates a linked decision
issue with upstream sources, affected code, attempted work, actual test results,
options/trade-offs and a recommendation. It does not push partial repairs or
request a merge in this case. A major version number alone is not a blocker.
GitHub Actions updates remain outside this automation and require manual review.
The router disables existing auto-merge when `needs-human-decision` is added.
Label events made only with `GITHUB_TOKEN` do not trigger that handler; configure
`GH_AW_GITHUB_TOKEN` for real handoffs, and verify auto-merge is disabled before
considering a handoff complete.

## Guarded Tier 2 merging

Tier 2 dispatches [the merge workflow](../.github/workflows/dependency-automerge.yml)
with the PR number, original reviewed head SHA, exact validated tree SHA and its
review run ID. The merge
workflow executes trusted `main` code only, never dependency PR code.

It validates the originating Tier 2 workflow, waits for that run (including
pushes and other safe outputs) to succeed, and waits for PR checks. The
[merge policy](../.github/scripts/dependency-merge-policy.cjs) then requires:

- an open, non-draft, same-repository Dependabot NuGet/npm PR targeting `main`;
- both routing labels and no `needs-human-decision`;
- the exact validated source tree at the PR head, with any signed repair commits
  descending from the original reviewed head (signed outputs may change commit
  SHAs but must not change validated content);
- at most eight changed files, all within the configured repair paths, excluding
  Domain/shared, migration, contract and workflow/policy edits;
- unambiguous successful GitHub Actions `backend` and `frontend` check runs on
  the final remote commit SHA.

Only then does it request `gh pr merge --auto --squash --match-head-commit`,
bound to that final remote SHA.
GitHub branch protection and review requirements still apply; no administrator
bypass or automatic approval is used. A stale head or failed gate stops the run
with an explicit error. Tier 2 reports a merge request, not a completed merge.

## Repository setup

Before enabling automation:

1. Require backend and frontend CI on `main`, protect workflow/policy files, and
   enable repository auto-merge. Keep any required human reviews you still want;
   automation will not bypass them.
2. Set the **Actions repository variable** `DEPENDABOT_AUTOMERGE=true`, not a
   secret. This gates automatic major routing and both merge workflows.
3. Configure `COPILOT_GITHUB_TOKEN` and the `gh-aw` output credentials, and ensure
   the `dependencies`, `dependency-tier2` and `needs-human-decision` labels exist.
   Agent pushes need an appropriately scoped PAT/GitHub App token, such as the
   existing `GH_AW_GITHUB_TOKEN` override, to trigger new PR CI. Pushes made only
   with `GITHUB_TOKEN` do not trigger ordinary PR workflows; missing CI blocks
   Tier 2 merging rather than being treated as success.
4. Ensure the router token can label PRs and dispatch workflows. Tier 2 output
   permissions must support pushes, comments, labels, decision issues and
   dispatching the merge workflow.

Already-open major PRs without a new event can be reviewed by dispatching
`dependency-fix-tier2.lock.yml` after adding its two required labels. Existing
`needs-human-decision` PRs remain manual; remove that label only after a human
decides to re-enter automation.

After changing Markdown workflow configuration, run
`gh aw compile dependency-fix-tier1 dependency-fix-tier2` and commit the generated
lock files together with the sources. Runtime agent imports read the profiles
from the trusted repository branch.

Policy tests run in CI and locally with
`node --test .github/scripts/dependency-merge-policy.test.cjs`.
