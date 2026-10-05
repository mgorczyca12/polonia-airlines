---
name: update-agent-models
description: 'Update or synchronize the central agent model policy with gh-aw workflow and sub-agent model settings. Use when changing model choices, checking model-policy drift, or applying the repository model policy.'
argument-hint: 'Optional profile and requested model ID, or check-only'
user-invocable: true
disable-model-invocation: false
---

# Update Agent Models

Use [.github/workflows/data/model-policy.json](../../workflows/data/model-policy.json) as the source of intended model settings. It is maintenance data, not a gh-aw import or runtime alias map.

## Procedure

1. Read the policy, current `git status`, and the mapped workflow and sub-agent definitions. Preserve unrelated edits. For a check-only request, report drift without changing files or generating lock files.
2. Validate the policy before editing: each referenced profile must exist and have a non-empty engine and model; each mapped workflow and sub-agent file must exist; each sub-agent parent must be a mapped workflow. Report invalid entries explicitly rather than skipping them.
3. If the user requests a new model choice, update the relevant profile in the policy first. Do not change other profiles without authorization. Ask for the desired choice if it is unspecified. Model IDs explicitly supplied by the user or inferred with their authorization may be absent from the bundled gh-aw catalog; keep them marked provisional until tested in an Actions run. Keep the PR reviewer assigned to a profile different from repository-configured agent-author models; a dedicated review profile is optional.
4. Check the installed version with `gh aw version`. Use `gh aw models --json --refresh-observed=false` for built-in aliases and bundled catalog evidence. This catalog may lag the Copilot subscription and is not an account entitlement check. Do not put local profile names or unsupported custom alias lists into `engine.model`.
5. For each affected workflow, set `engine.id` and `engine.model` from its assigned profile. Keep all other frontmatter and its import-only body unchanged. Do not put execution configuration into imported prompt text.
6. For each affected sub-agent, update the `model` inside its named `## agent:` definition, not merely the file's outer metadata. Check that its profile engine matches its parent workflow's engine, since the sub-agent inherits that engine. Stop and ask if they differ rather than silently changing runtime platforms.
7. Compile affected workflows in one command: `gh aw compile <workflow-id> ...`. Include parent workflows when a sub-agent changes. Never edit generated `.lock.yml` files manually. Validate each affected workflow with `gh aw validate <workflow-id>` and report failures and warnings. Do not change GitHub settings, run workflows remotely, or commit unless requested.
8. Verify every mapped workflow's engine/model and every named sub-agent's model matches the policy. Confirm the review model differs from all repository-configured agent-author profiles. Run `git diff --check`. If a check fails, fix task-related problems and repeat the affected checks.
9. Update the session summary with the actual changes, validation results, and unresolved access/runtime questions. Clearly distinguish synchronized local configuration from remotely tested behavior.
