# Dependency major-upgrade automation

Date: 2026-11-13

## Goal and decisions

- Route NuGet/npm major updates, including green-CI and framework upgrades, to
  Tier 2 for compatibility analysis, bounded repairs and guarded auto-merge.
- User approved enabling real outputs for both dependency fixers while retaining
  substantial-change handoffs. GitHub Actions updates and the MassTransit
  licensing-related major exclusions remain unchanged.
- User requested Tier 2 use the existing `medium-sol` profile (`gpt-6.1-sol`)
  instead of Astra. Updated its policy mapping and workflow; other profiles
  remain unchanged. The bundled catalog did not list this model; Actions runtime
  access remains provisional/unverified.
- Preserve manual review for Domain/shared changes, architecture/abstractions,
  migrations, public API/event changes, licensing changes, uncertainty, failed
  or incomplete validation, and more than eight total files in the final PR diff.
  Handoffs include a PR analysis and linked decision issue with options.

## Changes

- Updated both dependency agent profiles, Markdown workflows and generated locks;
  removed their preview-only staging and added Tier 2's merge dispatch.
- Updated the Dependabot router to label/dispatch major upgrades and disable
  existing auto-merge on human-review label events.
- Added [the merge workflow](../../.github/workflows/dependency-automerge.yml),
  [policy](../../.github/scripts/dependency-merge-policy.cjs) and
  [tests](../../.github/scripts/dependency-merge-policy.test.cjs). Merge gates
  require successful Tier 2 outputs, eligible PR/labels/scope and successful
  backend/frontend CI. Exact validated tree equality plus original-head ancestry
  supports recreated signed commits; the final merge binds to the remote SHA.
- Added a policy-test CI job, updated
  [model policy](../../.github/workflows/data/model-policy.json), and documented
  routing, safeguards and setup in [dependency updates](../dependency-updates.md).

## Checks

- `node --test .github/scripts/dependency-merge-policy.test.cjs`: 42 passed.
  Tests include the exact eight/nine-file boundary, no-repair upgrades, signed
  repairs, stale content, PR provenance/labels and missing/failed/ambiguous CI.
- `gh aw compile dependency-fix-tier1 dependency-fix-tier2`: passed, no warnings.
- `gh aw validate dependency-fix-tier1 dependency-fix-tier2 --strict`: passed.
- Native actionlint: all three changed handwritten workflows passed; generated
  dependency locks passed with the compiler's existing unsupported `queue`
  syntax exclusion. Docker-based lint was unavailable, so a temporary native
  actionlint binary was used instead.
- All mapped workflow/sub-agent models match policy; reviewer model remains
  distinct from author models. The initial ad hoc check mishandled CRLF; the
  corrected normalized check passed.
- `git diff --check`: passed. Application builds/tests were not run because no
  application code changed. One intermediate policy test caught an early-return
  bug; it was fixed before the final successful run.

## Pending

- User requested committing and pushing the completed changes to `main`.
  Commit/push execution follows this summary update; its actual result is
  reported in the final response. No remote workflow runs or GitHub settings
  changes were made. GitHub CLI is signed out, so live permissions, secrets,
  rulesets and model access were not verified.
- Deploy the sources and generated locks together. Confirm required backend/
  frontend CI, protected policy files, repository auto-merge, routing labels,
  `DEPENDABOT_AUTOMERGE=true`, Copilot credentials and output-token permissions.
- Use a scoped PAT/App output token to trigger CI after repairs and label-event
  handling for manual handoffs. `GITHUB_TOKEN`-only pushes/labels suppress those
  ordinary events; missing CI blocks merging.
- Already-open major PRs without a new event need a labelled manual Tier 2
  dispatch; existing human-review PRs remain manual until a human opts them in.
