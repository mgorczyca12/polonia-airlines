# Agent Model Policy

[model-policy.json](./model-policy.json) is the central, version-controlled source of intended model choices for the AI workflows and their sub-agents.

- `profiles` defines an engine and one model ID per profile. The current profiles are:
  - `fast`: GPT-6 Luna with low effort.
  - `small`: GPT-6 Luna with default settings.
  - `medium-sol`: GPT-6.1 Sol.
  - `medium-sonnet`: Claude Sonnet 5.5, used by PR review to cross-review changes written by the configured Luna, Sol, Astra, and Opus agents.
  - `large`: Claude Opus 5.5, the preferred high-capability model for most complex tasks.
  - `long-task`: GPT-6 Astra, assigned to the longer-running tier-2 dependency fixer.
- `workflows` maps workflow IDs (Markdown filenames without `.md`) to profiles.
- `subAgents` maps named sub-agents to their definition file, parent workflow, and profile. Their engine is inherited from the parent workflow.

Profile names are repository policy labels, **not custom gh-aw aliases**. This file is not imported or read by GitHub Actions. The executable settings remain in workflow frontmatter and named sub-agent definitions; changing this policy alone does not change execution.

Use the [update-agent-models skill](../../skills/update-agent-models/SKILL.md) to synchronize the policy with those settings, compile affected workflows, and validate the result. For example: "Use update-agent-models to apply the model policy" or "Use update-agent-models to change the large profile to a verified model ID."

The review workflow uses `medium-sonnet`, not a separate review profile. This is distinct from the models assigned to this repository's agent-authored changes (Luna, Sol, Astra, and Opus). Its review prompt asks it to verify the author's model from workflow metadata where possible; a fixed reviewer cannot guarantee diversity for an agent whose model is unknown or configured outside this policy.

Model IDs are inferred from Copilot display names at the user's request. The installed `gh-aw` catalog may lag behind the user's Copilot subscription. Local compilation checks configuration syntax only; it does not prove model availability, subscription access, or successful remote execution. Check the first staged Actions runs before relying on these choices.
