# gh-aw workflow upgrade

Date: 2026-11-16. Follow-up to the [frontend refactor plan](./2026-11-14-frontend-refactor-plan.md).

## Goal and decisions

- Upgrade the five agentic workflow lock files to gh-aw v0.91.10, required by the runtime compatibility issue noted in the previous plan.
- The model policy already matched every workflow and named sub-agent, so no model or workflow frontmatter changes were needed.
- Installed the official Windows amd64 v0.91.10 release after verifying its SHA-256. The GitHub CLI upgrade command could not run because this environment is not authenticated.

## Changes and checks

- Recompiled all five workflows with gh-aw v0.91.10. Updated `.github/aw/actions-lock.json` and the lock files for dependency-fix-tier1, dependency-fix-tier2, docs-updater, pr-review, and test-writer. Each generated metadata header records v0.91.10 and the configured model.
- `gh aw validate dependency-fix-tier1 dependency-fix-tier2 docs-updater pr-review test-writer --no-check-update` passed. It reported existing warnings for the `pull_request_target` trigger and an unrestricted pull-request branch target.
- `git diff --check` passed; Git reported line-ending conversion warnings for generated files.
- No GitHub Actions run was started, so remote activation is not verified. The pre-existing edit to the Nov 14 plan was left untouched.
