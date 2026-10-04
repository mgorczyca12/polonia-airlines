# AI Agent Workflow Decisions

Date: 2026-10-03

## Decisions

- Use GitHub Actions with GitHub Agentic Workflows (`gh-aw`) for repo agents; defer a custom LangGraph/Probot orchestrator.
- Keep the incident-fix integration for Jira or ServiceNow as future work. No incident-management integration is planned now.
- Start all AI workflows in staged/preview mode. Humans remain responsible for merges and architecture decisions.
- Use the existing GitHub Copilot subscription where possible, with a fine-grained PAT stored as `COPILOT_GITHUB_TOKEN`. The GitHub CLI was signed out after local setup.
- Allow Dependabot patch and minor updates to auto-merge only after required checks pass. Major updates and Actions updates remain manual. Dependabot handles rebasing its own branches; automatic rebasing of other PR branches is deferred.

## Workflows Added Locally

- `ci.yml`: backend and frontend build/test jobs for PRs and pushes to `main`.
- `.github/dependabot.yml`: weekly NuGet, pnpm/npm and GitHub Actions updates, grouped minor/patch changes, seven-day cooldown.
- `dependabot-automerge.yml`: opt-in via the `DEPENDABOT_AUTOMERGE` repository variable; restricted to Dependabot NuGet/npm minor and patch PRs.
- `pr-review.md`: cross-model reviewer using GPT-5.4; checks for correctness, invented APIs and weak tests; comment-only, never approves.
- `dependency-fix-tier1.md` and `dependency-fix-tier2.md`: small-model then larger-model repairs for failing Dependabot PRs. Domain/shared-layer changes, migrations, contract changes and framework major upgrades go to human review.
- `docs-updater.md`: proposes draft documentation/diagram PRs after merges; can also be run to backfill docs.
- `test-writer.md`: label-triggered test author for xUnit and Vitest, with a larger sub-agent for integration and Playwright tests.
- `polonia-airlines.slnx`: includes the backend services, shared library and test projects.
- `pnpm-workspace.yaml`: unresolved build-script permissions were replaced with explicit settings so frozen installs work.

The `gh-aw` Markdown workflow sources and generated `.lock.yml` files are both required. The AI workflows are staged, so proposed comments, pushes, labels and PRs are previews rather than live changes.

## GitHub Setup Deferred

Nothing was committed or pushed, and no repository settings were changed. Before enabling automation, configure the Copilot PAT secret and required labels, require CI checks on `main`, then opt in to Dependabot auto-merge. Removing `staged: true` is required before agent outputs become live. An additional CI-trigger token may be needed for agent-created branches to start CI.

## Known Caveats

- Existing frontend issues remain: ESLint is not installed, Prettier reports files needing formatting, TypeScript has two errors, and there are no frontend tests yet. CI currently runs build and allows no frontend tests.
- Backend integration test projects currently have no discovered tests.
- Dependabot's documented pnpm support may lag the repository's pnpm 12 version; check its first update PRs for lockfile compatibility.
- One `gh-aw` compiler warning remained after the final compile and should be reviewed before activation.