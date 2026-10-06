---
name: Cross-model PR reviewer
description: Advisory cross-model review of pull requests (preview-only).
on:
  pull_request:
    types: [opened, ready_for_review, synchronize]
if: github.event.pull_request.draft == false && github.event.pull_request.user.login != 'dependabot[bot]'
permissions:
  contents: read
  pull-requests: read
  checks: read
  copilot-requests: none
engine:
  id: copilot
  # Medium Sonnet keeps review independent from this repo's Luna, Sol, Astra, and Opus author profiles.
  model: claude-sonnet-5.5
sandbox:
  agent:
    version: v0.28.37
timeout-minutes: 15
max-turns: 40
max-ai-credits: 150
max-daily-ai-credits: 500
tools:
  github:
    toolsets: [pull_requests, repos]
    min-integrity: approved
safe-outputs:
  staged: true
  report-failure-as-issue: false
  create-pull-request-review-comment:
    max: 10
  submit-pull-request-review:
    max: 1
    allowed-events: [COMMENT]
---

{{#runtime-import .github/agents/code-reviewer.agent.md}}
