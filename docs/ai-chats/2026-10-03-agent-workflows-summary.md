# AI Agent Workflow Decisions

Date: 2026-10-03

## Decisions

- Use GitHub Actions with GitHub Agentic Workflows (`gh-aw`) for repo agents; defer a custom LangGraph/Probot orchestrator.
- Keep the incident-fix integration for Jira or ServiceNow as future work. No incident-management integration is planned now.
- Start all AI workflows in staged/preview mode. Humans remain responsible for merges and architecture decisions.
- Use the existing GitHub Copilot subscription where possible, with a fine-grained PAT stored as `COPILOT_GITHUB_TOKEN`. The GitHub CLI was signed out after local setup.
- Allow Dependabot patch and minor updates to auto-merge only after required checks pass. Major updates and Actions updates remain manual. Dependabot handles rebasing its own branches; automatic rebasing of other PR branches is deferred.

## Workflows Added

- `ci.yml`: backend and frontend build/test jobs for PRs and pushes to `main`.
- `.github/dependabot.yml`: weekly NuGet, pnpm/npm and GitHub Actions updates, grouped minor/patch changes, seven-day cooldown.
- `dependabot-automerge.yml`: opt-in via the `DEPENDABOT_AUTOMERGE` repository variable; restricted to Dependabot NuGet/npm minor and patch PRs.
- `pr-review.md`: cross-model reviewer using GPT-5.4; checks for correctness, invented APIs and weak tests; comment-only, never approves.
- `dependency-fix-tier1.md` and `dependency-fix-tier2.md`: small-model then larger-model repairs for failing Dependabot PRs. Domain/shared-layer changes, migrations, contract changes and framework major upgrades go to human review.
- `docs-updater.md`: proposes draft documentation/diagram PRs after merges; can also be run to backfill docs.
- `test-writer.md`: label-triggered test author for xUnit and Vitest, with a larger sub-agent for integration and Playwright tests.
- `polonia-airlines.slnx`: includes the backend services, shared library and test projects.
- `pnpm-workspace.yaml`: unresolved build-script permissions were replaced with explicit settings so frozen installs work.

The `gh-aw` Markdown workflow sources and generated `.lock.yml` files are both required. The AI workflows are staged, so proposed comments, pushes, labels and PRs are previews rather than live changes. These repository files are committed on `main`.

## GitHub Setup Deferred

GitHub Actions is currently disabled, and the Dependabot configuration is commented out. No GitHub-side settings were configured. Before enabling automation, configure the Copilot PAT secret and required labels, require CI checks on `main`, then opt in to Dependabot auto-merge. Removing `staged: true` is required before agent outputs become live. An additional CI-trigger token may be needed for agent-created branches to start CI.

## Future Work: Red Diagnostic Environment

Explore an on-demand, access-restricted "Red" environment alongside the conceptual Blue/Green production slots. Its purpose would be production-like debugging and testing, not serving customer traffic. Defer implementation until there is a working MVP and a live system to replicate.

Design constraints to retain for that future investigation:

- Restore coordinated, production-derived snapshots or isolated clones for the stores in scope (relational, document, and object storage); decide point-in-time consistency and retention requirements.
- Complete data masking before developers or agents can access the environment. Do not connect Red to live production stores, even for fallback reads.
- Isolate writes and external side effects such as messaging, payments, and email. Use restricted developer access, verbose diagnostics, and automatic teardown/TTL.
- Treat Red as an ephemeral diagnostic environment, not a replacement for a tested backup and disaster-recovery plan. Snapshot cadence, provider-specific restore mechanisms, and costs remain undecided.

## Known Caveats

- Existing frontend issues remain: ESLint is not installed, Prettier reports files needing formatting, TypeScript has two errors, and there are no frontend tests yet. CI currently runs build and allows no frontend tests.
- Backend integration test projects currently have no discovered tests.
- Dependabot's documented pnpm support may lag the repository's pnpm 12 version; check its first update PRs for lockfile compatibility.
- One `gh-aw` compiler warning remained after the final compile and should be reviewed before activation.