# Dependabot and post-merge documentation checkout

## Goal and changes

- Restored the existing Dependabot NuGet, npm, and Actions weekly schedules, seven-day cooldowns, and minor/patch groups in [dependabot.yml](../../.github/dependabot.yml). Takes effect once pushed to the default branch.
- Changed [docs-updater.md](../../.github/workflows/docs-updater.md) to react to merged `pull_request_target: closed` events targeting `main`, while retaining manual dispatch and the merged-only guard.
- Explicitly check out the trusted workflow repository with full history. gh-aw no longer generates a PR-head checkout step for this trigger, so deleting the merged branch does not prevent documentation work.
- Updated [documentation-writer.agent.md](../../.github/agents/documentation-writer.agent.md) to work against `main`, read the merged PR diff through GitHub tools/merge commit metadata, and never fetch the deleted head branch or execute PR-supplied scripts.
- Regenerated [docs-updater.lock.yml](../../.github/workflows/docs-updater.lock.yml). Read-only source permissions and staged outputs remain unchanged.

## Evidence and checks

- Public run [37483999332](https://github.com/mgorczyca12/polonia-airlines/actions/runs/37483999332) reported PR #13 as closed and its head branch as deleted, with checkout exit 128. The agent job subsequently failed at Execute GitHub Copilot CLI; that later failure's exact cause was not established from annotations.
- `gh aw compile docs-updater --approve` and `gh aw validate docs-updater` passed after reviewing the trigger change. The generic `pull_request_target` warning remains; validation still reports the trigger change against the committed baseline even after approved compilation.
- Reviewed generated manifests: no secrets, pinned actions, or containers were added, removed, or changed. The target trigger uses trusted base-repository code, not PR-head checkout; merged-only guard and preview mode are retained. Runtime imports now come from trusted base context. No remote run was performed.
- Assertions passed: generated lock has no Checkout PR branch step, trusted repository/full-history settings exist, manual dispatch and merge guard remain, and Dependabot restoration exactly matches the uncommented original.
- `git diff --check` passed. An optional PyYAML availability probe found it absent; no dependency was installed, and content-preservation assertions were used instead.

## Pending

- Changes are local, not committed or pushed. A new staged Actions run is needed after deployment.
- Investigate any remaining Copilot execution failure separately; model IDs/access are still provisional from the prior session.
