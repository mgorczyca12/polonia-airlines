# Agent Profile Imports

Date: 2026-10-05

## Goal and decisions

- Standardize all five AI workflows: keep execution configuration in `.github/workflows/` and all agent instructions in kebab-case `*.agent.md` profiles under `.github/agents/`.
- Each workflow body contains only a `{{#runtime-import .github/agents/<profile>.agent.md}}` directive. Frontmatter-only workflows with `imports:` failed on `gh-aw` v0.89.21 with `no markdown content found`; runtime imports compile and validate.
- Keep the `test-writer` workflow's named `integration-tests` sub-agent behavior through a nested runtime import in its agent profile.
- Preserve workflow triggers, permissions, safe-output policies, and display names.

## Files changed

- Added [dependency-fixer.agent.md](../../.github/agents/dependency-fixer.agent.md), [dependency-fixer-tier2.agent.md](../../.github/agents/dependency-fixer-tier2.agent.md), [test-writer.agent.md](../../.github/agents/test-writer.agent.md), [code-reviewer.agent.md](../../.github/agents/code-reviewer.agent.md), and [documentation-writer.agent.md](../../.github/agents/documentation-writer.agent.md); moved each workflow's instructions unchanged.
- Added [integration-tests.agent.md](../../.github/agents/integration-tests.agent.md), runtime-imported by [test-writer.agent.md](../../.github/agents/test-writer.agent.md).
- Updated the five corresponding workflow sources and regenerated their `.lock.yml` files. Workflow `name:` fields preserve the previous display names without retaining prompt headings in workflow bodies.

## Checks

- `gh aw compile dependency-fix-tier1 dependency-fix-tier2 test-writer pr-review docs-updater` — passed; all five workflows compiled.
- `gh aw validate` run separately for each of the five workflows — passed. The existing broad push-target warning remains for `test-writer`.
- Structural and preservation assertions — passed: every workflow body is a single runtime import, configuration matches the committed version apart from explicit display names, agent instructions are unchanged, generated locks reference the correct profiles, and the integration sub-agent instructions/model are preserved.
- `git diff --check` — passed.
- No application test suite was run; changes are limited to workflow prompts and generated workflow files.

## Deferred

- GitHub Actions was not enabled or exercised remotely. Runtime behavior remains to be confirmed in a GitHub Actions run.

## Custom model alias investigation

- Confirmed installed `gh-aw` version: v0.89.21.
- Tested isolated workflows with a direct custom `models:` alias map and a map imported from `workflows/data/models.md`. Both compilation attempts failed schema validation: alias keys are unknown; valid `models` fields are `allowed`, `blocked`, `default-ai-credits-pricing`, and `providers`.
- The version-pinned upstream import test explicitly states that frontmatter model aliases were removed and aliases now come only from the built-in map. Draft specification and sample content still describe custom aliases, but the installed compiler rejects them.
- No production workflow was changed during these probes. Temporary probe files were deleted. Central custom alias configuration is not supported via the tested frontmatter/import mechanism in this version.

## Central model variable evaluation

- An isolated workflow using `engine.id: copilot` and `engine.model: ${{ vars.GHAW_DEFAULT_MODEL }}` compiled successfully on v0.89.21. The generated lock preserves the expression in `COPILOT_MODEL` and model metadata; it is not resolved to a concrete model at local compilation.
- The pasted `runner.engine`/`runner.model` configuration failed schema validation. A separate probe using top-level `strategy.matrix` also failed schema validation.
- Repository or organization variables can therefore centralize model values after a one-time change to workflow references and recompilation. Actual authenticated runtime routing, access, and behavior when a variable is unset were not tested.
- No production workflows or GitHub variables were changed during this evaluation.

## Repository model policy

- Chose repository-maintained model settings instead of GitHub variables or unsupported custom gh-aw alias maps.
- Added [model-policy.json](../../.github/workflows/data/model-policy.json) with lightweight (`small`), powerful (`large`), and review (`gpt-5.4`) profiles; mapped all five workflows and the integration-tests sub-agent without changing existing model choices.
- Added the [policy guide](../../.github/workflows/data/README.md) and [update-agent-models skill](../../.github/skills/update-agent-models/SKILL.md). The skill applies profile values to workflow/sub-agent configuration and recompiles affected locks; the policy itself is not consumed at runtime.
- Validation: parsed policy JSON and verified coverage/alignment for all five workflow engines/models and the integration-tests sub-agent; passed. The first verification script incorrectly assumed no comment between engine fields; corrected the check to allow the existing review-engine comment, with no configuration change. `git diff --check` passed. No new compilation was needed because this addition changed no executable model settings.

## Profile refresh investigation

- User proposed fast/small Luna, two medium choices (Sol and Sonnet), large Opus, and Astra for long-horizon tasks, plus a reviewer distinct from agent-authored PRs.
- The v0.89.21 bundled model catalog lists `gpt-5.6-luna`, `gpt-5.6-sol`, `claude-sonnet-5`, `claude-opus-5`, and `gpt-6-astra`; it does not list the user's exact display labels/versions. The catalog is not the user's live Copilot subscription inventory.
- Isolated compile accepted `gpt-6.1-sol` as an explicit `engine.model` despite it not being in the bundled catalog. It only proves compile-time acceptance, not Copilot account authorization or runtime model routing. Also tested `gpt-5.6-luna?effort=low`; compile accepted it, but runtime effort behavior was not tested.
- User authorized proceeding with inferred model IDs, with access to be checked later. Temporary probe files and generated lock files were removed.
- Added fast (`gpt-6-luna?effort=low`), small (`gpt-6-luna`), medium-Sol (`gpt-6.1-sol`), medium-Sonnet (`claude-sonnet-5.5`), large (`claude-opus-5.5`), long-task (`gpt-6-astra`) and review (`gpt-5.4`) policy profiles. The two medium profiles are explicit alternatives; no automatic context/cost switch exists.
- Assigned tier 1 to fast, docs updater to small, test writer to medium-Sol, integration-test sub-agent to large, tier 2 to Astra long-task, and PR review to dedicated GPT-5.4. Updated reviewer instructions to check reliable PR workflow metadata for author-model diversity; where author model is not reported, reviewer must disclose it cannot verify diversity.
- `medium-sonnet` is defined but currently unassigned; it is the explicit alternative for medium tasks where context/effort changes the cost trade-off. No automatic context-based routing is configured.
- Compiled and validated all five workflows successfully with gh-aw v0.89.21. Existing test-writer broad push-target warning remains. Policy-to-workflow and sub-agent assertions plus `git diff --check` passed.
- Model names remain provisional. Compilation can validate workflow syntax but cannot establish Copilot subscription access or runtime availability; staged Actions validation remains pending.
- Follow-up: removed the dedicated `review` profile and assigned `pr-review` to the existing `medium-sonnet` profile. `test-writer` remains assigned to `medium-sol`. The reviewer remains a different model from the repository's configured agent-author profiles. Recompiled and validated `pr-review`; policy JSON/profile mappings and `git diff --check` pass. No model runtime test was run.
